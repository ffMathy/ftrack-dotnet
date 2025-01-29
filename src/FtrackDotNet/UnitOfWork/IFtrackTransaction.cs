namespace FtrackDotNet.UnitOfWork;

public interface IFtrackTransaction : IDisposable
{
    ValueTask CommitAsync();
    void Rollback();
}