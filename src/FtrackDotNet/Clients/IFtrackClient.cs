namespace FtrackDotNet.Clients;

public interface IFtrackClient
{
    Task<T> QueryAsync<T>(string query);
    Task<QuerySchemasSchemaResponse> QuerySchemasAsync();
}