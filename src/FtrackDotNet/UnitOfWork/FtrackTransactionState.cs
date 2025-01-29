namespace FtrackDotNet.UnitOfWork;

internal class FtrackTransactionState : IFtrackTransactionState
{
    public AsyncLocal<int> Depth { get; } = new();
    public AsyncLocal<FtrackTransaction> CurrentTransaction { get; } = new();
}