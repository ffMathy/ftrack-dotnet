namespace FtrackDotNet.UnitOfWork;

internal class FtrackTransactionFactory(
    IFtrackTransactionState ftrackTransactionState,
    IChangeDetector changeDetector) : IFtrackTransactionFactory
{
    public FtrackTransaction Create()
    {
        var transaction = new FtrackTransaction(ftrackTransactionState, changeDetector);
        return transaction;
    }
}