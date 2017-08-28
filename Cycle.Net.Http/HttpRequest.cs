using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Http
{
    public class HttpRequest : IHttpRequest
    {
        public string Id { get; }
        public string Url { get; }
        public HttpRequest(string id, string url)
        {
            Id = id;
            Url = url;
        }

        public override string ToString() =>
            $"HttpRequest(id={Id}, url={Url})";
    }
}