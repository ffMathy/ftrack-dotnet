namespace FtrackDotNet.UnitOfWork;

internal class FtrackTransaction : IFtrackTransaction
{
    internal IChangeDetector ChangeDetector { get; }
    
    private readonly IFtrackTransactionState _ftrackTransactionState;

    public FtrackTransaction(
        IFtrackTransactionState ftrackTransactionState, 
        IChangeDetector changeDetector)
    {
        ChangeDetector = changeDetector;
        _ftrackTransactionState = ftrackTransactionState;
        
        ftrackTransactionState.CurrentTransaction.Value ??= this;
    }

    public async ValueTask CommitAsync()
    {
        //TODO: save changes
        ChangeDetector.RefreshSnapshots();
        Dispose();
    }

    public void Rollback()
    {
        //TODO: rollback changes
        Dispose();
    }
    
    public void Dispose()
    {
        if(_ftrackTransactionState.CurrentTransaction.Value == this)
        {
            _ftrackTransactionState.CurrentTransaction.Value = null;
        }
    }
}