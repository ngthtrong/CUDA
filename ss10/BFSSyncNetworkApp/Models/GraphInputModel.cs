namespace BFSSyncNetworkApp.Models;

public sealed class GraphInputModel
{
    public int[]? Nodes { get; set; }

    public int[][]? Edges { get; set; }

    public Dictionary<string, int[]>? Adjacency { get; set; }
}
