namespace FtrackDotNet.UnitOfWork;

public interface IFtrackTransactionFactory
{
    IFtrackTransaction Create();
}