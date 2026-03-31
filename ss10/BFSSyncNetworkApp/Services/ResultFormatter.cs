using System.Text;
using BFSSyncNetworkApp.Models;

namespace BFSSyncNetworkApp.Services;

public static class ResultFormatter
{
    public static string FormatSingle(Graph graph, BfsResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Mode: {ToDisplayName(result.Mode)}");
        builder.AppendLine($"Start node: {result.StartNode}");
        builder.AppendLine($"Nodes: {string.Join(", ", graph.Nodes)}");
        builder.AppendLine($"Discovery depth (max level): {result.DiscoveryDepth}");
        builder.AppendLine($"Communication rounds: {result.CommunicationRounds}");
        builder.AppendLine($"Messages sent: {result.MessageCount}");
        builder.AppendLine($"Visit order: [{string.Join(", ", result.VisitOrder)}]");
        builder.AppendLine();

        builder.AppendLine("Node states:");
        foreach (int node in graph.Nodes)
        {
            int level = result.LevelByNode[node];
            int? parent = result.ParentByNode[node];

            if (level < 0)
            {
                builder.AppendLine($"  Node {node}: unreached");
            }
            else
            {
                builder.AppendLine($"  Node {node}: level={level}, parent={(parent is null ? "-" : parent)}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Round trace:");
        foreach (BfsRoundResult round in result.Rounds)
        {
            if (round.RoundNumber == 0)
            {
                builder.AppendLine($"  Round 0 -> Mark start: [{string.Join(", ", round.NewlyMarked.Select(item => item.NodeId))}]");
                continue;
            }

            string senders = round.Senders.Count == 0
                ? "[]"
                : $"[{string.Join(", ", round.Senders)}]";

            string marks = round.NewlyMarked.Count == 0
                ? "[]"
                : $"[{string.Join(", ", round.NewlyMarked.Select(item => item.ParentId is null ? item.NodeId.ToString() : $"{item.NodeId}(p={item.ParentId})"))}]";

            builder.AppendLine($"  Round {round.RoundNumber} -> Senders: {senders}, Newly marked: {marks}");
        }

        return builder.ToString();
    }

    public static string FormatComparison(Graph graph, IReadOnlyList<BfsResult> results)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Comparison summary");
        builder.AppendLine("-----------------------------------------------------");
        builder.AppendLine("Mode                 | Depth | Rounds | Messages");
        builder.AppendLine("-----------------------------------------------------");

        foreach (BfsResult result in results)
        {
            builder.AppendLine(
                $"{ToDisplayName(result.Mode),-20} | {result.DiscoveryDepth,5} | {result.CommunicationRounds,6} | {result.MessageCount,8}");
        }

        builder.AppendLine("-----------------------------------------------------");
        builder.AppendLine();

        bool sameVisitOrder = results
            .Select(result => string.Join(",", result.VisitOrder))
            .Distinct()
            .Count() == 1;

        bool sameLevels = results
            .Select(result => SerializeLevels(graph, result.LevelByNode))
            .Distinct()
            .Count() == 1;

        builder.AppendLine($"Visit order equal across all modes: {(sameVisitOrder ? "Yes" : "No")}");
        builder.AppendLine($"Level assignment equal across all modes: {(sameLevels ? "Yes" : "No")}");
        builder.AppendLine();

        foreach (BfsResult result in results)
        {
            builder.AppendLine(FormatSingle(graph, result));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string ToDisplayName(BfsExecutionMode mode)
    {
        return mode switch
        {
            BfsExecutionMode.ThreadTask => "Thread/Task",
            BfsExecutionMode.ParallelFor => "Parallel.For",
            BfsExecutionMode.Channel => "Channel",
            _ => mode.ToString()
        };
    }

    private static string SerializeLevels(Graph graph, IReadOnlyDictionary<int, int> levels)
    {
        return string.Join("|", graph.Nodes.Select(node => $"{node}:{levels[node]}"));
    }
}
