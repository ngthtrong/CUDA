namespace BFSSyncNetworkApp.Models;

public sealed class Graph
{
    public Graph(Dictionary<int, int[]> adjacency)
    {
        Adjacency = adjacency;
        Nodes = adjacency.Keys.OrderBy(node => node).ToArray();
    }

    public IReadOnlyDictionary<int, int[]> Adjacency { get; }

    public IReadOnlyList<int> Nodes { get; }

    public bool ContainsNode(int nodeId)
    {
        return Adjacency.ContainsKey(nodeId);
    }
}
