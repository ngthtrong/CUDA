using System.Text.Json;
using BFSSyncNetworkApp.Models;

namespace BFSSyncNetworkApp.Services;

public static class GraphParser
{
    public static Graph Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON graph input cannot be empty.");
        }

        GraphInputModel? input;
        try
        {
            input = JsonSerializer.Deserialize<GraphInputModel>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON format: {ex.Message}", ex);
        }

        if (input is null)
        {
            throw new ArgumentException("Cannot parse graph from JSON.");
        }

        if (input.Adjacency is { Count: > 0 })
        {
            return BuildFromAdjacency(input.Adjacency);
        }

        if (input.Edges is { Length: > 0 })
        {
            return BuildFromEdgeList(input.Nodes, input.Edges);
        }

        throw new ArgumentException("Graph JSON must contain either 'adjacency' or 'edges'.");
    }

    private static Graph BuildFromAdjacency(Dictionary<string, int[]> adjacency)
    {
        var graph = new Dictionary<int, HashSet<int>>();

        foreach ((string rawNode, int[] neighbors) in adjacency)
        {
            if (!int.TryParse(rawNode, out int nodeId))
            {
                throw new ArgumentException($"Adjacency key '{rawNode}' is not a valid integer node id.");
            }

            EnsureNode(graph, nodeId);

            foreach (int neighbor in neighbors)
            {
                EnsureNode(graph, neighbor);

                if (neighbor == nodeId)
                {
                    continue;
                }

                graph[nodeId].Add(neighbor);
                graph[neighbor].Add(nodeId);
            }
        }

        return ToGraph(graph);
    }

    private static Graph BuildFromEdgeList(int[]? nodes, int[][] edges)
    {
        var graph = new Dictionary<int, HashSet<int>>();

        if (nodes is not null)
        {
            foreach (int node in nodes)
            {
                EnsureNode(graph, node);
            }
        }

        foreach (int[] edge in edges)
        {
            if (edge.Length != 2)
            {
                throw new ArgumentException("Each edge must have exactly 2 node ids, for example [0, 1].");
            }

            int source = edge[0];
            int target = edge[1];

            EnsureNode(graph, source);
            EnsureNode(graph, target);

            if (source == target)
            {
                continue;
            }

            graph[source].Add(target);
            graph[target].Add(source);
        }

        return ToGraph(graph);
    }

    private static Graph ToGraph(Dictionary<int, HashSet<int>> graph)
    {
        if (graph.Count == 0)
        {
            throw new ArgumentException("Graph is empty.");
        }

        Dictionary<int, int[]> adjacency = graph.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.OrderBy(node => node).ToArray());

        return new Graph(adjacency);
    }

    private static void EnsureNode(Dictionary<int, HashSet<int>> graph, int node)
    {
        if (!graph.ContainsKey(node))
        {
            graph[node] = [];
        }
    }
}
