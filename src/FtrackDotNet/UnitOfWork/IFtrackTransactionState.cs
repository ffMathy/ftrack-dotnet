namespace FtrackDotNet.UnitOfWork;

public interface IFtrackTransactionState
{
    AsyncLocal<IFtrackTransaction?> CurrentTransaction { get; }    
}