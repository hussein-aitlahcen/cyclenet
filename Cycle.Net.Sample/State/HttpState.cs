using System.Collections.Immutable;
using Cycle.Net.Http;

namespace Cycle.Net.Sample.State
{
    public class HttpState
    {
        public static HttpState Initial = new HttpState(ImmutableList.Create<IHttpResponse>());
        public ImmutableList<IHttpResponse> Responses { get; }
        public HttpState(ImmutableList<IHttpResponse> responses) => Responses = responses;

        public static HttpState Reducer(HttpState previous, IHttpResponse response) =>
            new HttpState(previous.Responses.Add(response));
    }
}