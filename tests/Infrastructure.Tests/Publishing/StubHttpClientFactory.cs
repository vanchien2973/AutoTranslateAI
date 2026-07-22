using System.Net;
using System.Text;

namespace Infrastructure.Tests.Publishing;

internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly StubHandler _handler;

    public StubHttpClientFactory(string responseJson, HttpStatusCode status = HttpStatusCode.OK) =>
        _handler = new StubHandler(responseJson, status);

    public string? LastRequestBody => _handler.LastRequestBody;
    public Uri? LastRequestUri => _handler.LastRequestUri;

    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _responseJson;
        private readonly HttpStatusCode _status;

        public StubHandler(string responseJson, HttpStatusCode status)
        {
            _responseJson = responseJson;
            _status = status;
        }

        public string? LastRequestBody { get; private set; }
        public Uri? LastRequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json"),
            };
        }
    }
}
