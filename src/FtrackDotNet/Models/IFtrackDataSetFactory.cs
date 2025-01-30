namespace FtrackDotNet.Models;

public interface IFtrackDataSetFactory
{
    FtrackDataSet<T> Create<T>() where T : IFtrackEntity;
}