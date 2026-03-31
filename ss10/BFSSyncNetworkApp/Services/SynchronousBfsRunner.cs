using System.Collections.Concurrent;
using System.Threading.Channels;
using BFSSyncNetworkApp.Models;

namespace BFSSyncNetworkApp.Services;

public static class SynchronousBfsRunner
{
    private sealed record DiscoveryBatch(long MessagesSent, Dictionary<int, int> ParentByNode);

    private readonly record struct ProbeMessage(int Sender, int Target);

    public static BfsResult Run(Graph graph, int startNode, BfsExecutionMode mode)
    {
        if (!graph.ContainsNode(startNode))
        {
            throw new ArgumentException($"Start node {startNode} does not exist in the graph.");
        }

        var levels = graph.Nodes.ToDictionary(node => node, _ => -1);
        var parents = graph.Nodes.ToDictionary<int, int, int?>(node => node, _ => null);
        var rounds = new List<BfsRoundResult>();
        var visitOrder = new List<int>();

        levels[startNode] = 0;
        visitOrder.Add(startNode);

        rounds.Add(new BfsRoundResult(
            RoundNumber: 0,
            Senders: [],
            NewlyMarked: [new NodeActivation(startNode, null)]));

        List<int> frontier = [startNode];
        int round = 0;
        long totalMessages = 0;

        while (frontier.Count > 0)
        {
            round++;
            DiscoveryBatch batch = mode switch
            {
                BfsExecutionMode.ThreadTask => DiscoverWithTasks(graph, frontier),
                BfsExecutionMode.ParallelFor => DiscoverWithParallelFor(graph, frontier),
                BfsExecutionMode.Channel => DiscoverWithChannels(graph, frontier),
                _ => throw new NotSupportedException($"Unsupported mode: {mode}")
            };

            totalMessages += batch.MessagesSent;

            List<NodeActivation> newlyMarked = batch.ParentByNode
                .Where(kvp => levels[kvp.Key] == -1)
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new NodeActivation(kvp.Key, kvp.Value))
                .ToList();

            foreach (NodeActivation activation in newlyMarked)
            {
                levels[activation.NodeId] = round;
                parents[activation.NodeId] = activation.ParentId;
                visitOrder.Add(activation.NodeId);
            }

            rounds.Add(new BfsRoundResult(
                RoundNumber: round,
                Senders: frontier.OrderBy(node => node).ToArray(),
                NewlyMarked: newlyMarked));

            frontier = newlyMarked.Select(item => item.NodeId).ToList();
        }

        int discoveryDepth = levels.Values.Where(level => level >= 0).DefaultIfEmpty(0).Max();

        return new BfsResult(
            Mode: mode,
            StartNode: startNode,
            DiscoveryDepth: discoveryDepth,
            CommunicationRounds: round,
            MessageCount: totalMessages,
            LevelByNode: levels,
            ParentByNode: parents,
            VisitOrder: visitOrder,
            Rounds: rounds);
    }

    private static DiscoveryBatch DiscoverWithTasks(Graph graph, IReadOnlyList<int> frontier)
    {
        var discoveredParents = new ConcurrentDictionary<int, int>();
        long messages = 0;

        Task[] workers = frontier
            .Select(sender => Task.Run(() =>
            {
                int[] neighbors = graph.Adjacency[sender];
                Interlocked.Add(ref messages, neighbors.Length);

                foreach (int neighbor in neighbors)
                {
                    discoveredParents.AddOrUpdate(
                        neighbor,
                        sender,
                        (_, existingParent) => Math.Min(existingParent, sender));
                }
            }))
            .ToArray();

        Task.WaitAll(workers);

        return new DiscoveryBatch(messages, discoveredParents.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }

    private static DiscoveryBatch DiscoverWithParallelFor(Graph graph, IReadOnlyList<int> frontier)
    {
        var discoveredParents = new ConcurrentDictionary<int, int>();
        long messages = 0;

        Parallel.For(0, frontier.Count, i =>
        {
            int sender = frontier[i];
            int[] neighbors = graph.Adjacency[sender];
            Interlocked.Add(ref messages, neighbors.Length);

            foreach (int neighbor in neighbors)
            {
                discoveredParents.AddOrUpdate(
                    neighbor,
                    sender,
                    (_, existingParent) => Math.Min(existingParent, sender));
            }
        });

        return new DiscoveryBatch(messages, discoveredParents.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }

    private static DiscoveryBatch DiscoverWithChannels(Graph graph, IReadOnlyList<int> frontier)
    {
        var discoveredParents = new ConcurrentDictionary<int, int>();
        long messages = 0;

        var channel = Channel.CreateUnbounded<ProbeMessage>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = false
        });

        Task producer = Task.Run(async () =>
        {
            foreach (int sender in frontier)
            {
                foreach (int neighbor in graph.Adjacency[sender])
                {
                    Interlocked.Increment(ref messages);
                    await channel.Writer.WriteAsync(new ProbeMessage(sender, neighbor)).ConfigureAwait(false);
                }
            }

            channel.Writer.Complete();
        });

        int consumerCount = Math.Max(1, Math.Min(Environment.ProcessorCount, frontier.Count));
        Task[] consumers = Enumerable.Range(0, consumerCount)
            .Select(_ => Task.Run(async () =>
            {
                await foreach (ProbeMessage message in channel.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    discoveredParents.AddOrUpdate(
                        message.Target,
                        message.Sender,
                        (_, existingParent) => Math.Min(existingParent, message.Sender));
                }
            }))
            .ToArray();

        Task.WaitAll([producer, .. consumers]);

        return new DiscoveryBatch(messages, discoveredParents.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }
}
