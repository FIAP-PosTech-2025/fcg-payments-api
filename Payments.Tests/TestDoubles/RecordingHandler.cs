using System.Net;
using System.Text;

namespace Payments.Tests.TestDoubles;

public class RecordingHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly List<RecordedRequest> _requests = new();
    private readonly object _lock = new();

    public RecordingHandler(HttpStatusCode statusCode = HttpStatusCode.NoContent)
    {
        _statusCode = statusCode;
    }

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_lock)
            {
                return _requests.ToList();
            }
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

        lock (_lock)
        {
            _requests.Add(new RecordedRequest(request.Method, request.RequestUri, body));
        }

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
        };
    }
}

public sealed record RecordedRequest(HttpMethod Method, Uri? Uri, string Body);
