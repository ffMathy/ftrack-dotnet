namespace FtrackDotNet;

public interface IFtrackClient
{
    Task<T> QueryAsync<T>(string query);
}