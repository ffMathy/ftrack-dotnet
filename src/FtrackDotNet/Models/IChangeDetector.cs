namespace FtrackDotNet.Models;

public interface IChangeDetector
{
    void TrackEntity(object entity);
    void OnChangesSaved();
}