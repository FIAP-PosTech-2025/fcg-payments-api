namespace Payments.Tests.TestDoubles;

public sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly Dictionary<string, HttpClient> _clients;

    public FakeHttpClientFactory(Dictionary<string, HttpClient> clients)
    {
        _clients = clients;
    }

    public HttpClient CreateClient(string name)
    {
        if (_clients.TryGetValue(name, out var client))
            return client;

        throw new InvalidOperationException($"HttpClient com nome '{name}' nao foi configurado no teste.");
    }
}
