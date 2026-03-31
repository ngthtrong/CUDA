namespace BFSSyncNetworkApp.Models;

public enum BfsExecutionMode
{
    ThreadTask,
    ParallelFor,
    Channel
}

public sealed record NodeActivation(int NodeId, int? ParentId);

public sealed record BfsRoundResult(
    int RoundNumber,
    IReadOnlyList<int> Senders,
    IReadOnlyList<NodeActivation> NewlyMarked);

public sealed record BfsResult(
    BfsExecutionMode Mode,
    int StartNode,
    int DiscoveryDepth,
    int CommunicationRounds,
    long MessageCount,
    IReadOnlyDictionary<int, int> LevelByNode,
    IReadOnlyDictionary<int, int?> ParentByNode,
    IReadOnlyList<int> VisitOrder,
    IReadOnlyList<BfsRoundResult> Rounds);
