namespace FtrackDotNet.UnitOfWork;

public interface IFtrackTransactionState
{
    AsyncLocal<FtrackTransaction?> CurrentTransaction { get; }    
}