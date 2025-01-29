namespace FtrackDotNet.UnitOfWork;

public interface IChangeDetector
{
    void TrackEntity(object entity);
    void RefreshSnapshots();
}