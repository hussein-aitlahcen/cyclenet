using Xunit;
using System;
using System.Reactive;
using System.Reactive.Linq;
using Cycle.Net.Sample.State;
using System.Collections.Immutable;
using Cycle.Net.Http;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Sample.Test.State
{
    public sealed class HttpStateTest
    {
        public static IHttpRequest FakeRequest = new HttpRequest("fake-id", "http://fake-url.com");
        public static IHttpResponse FakeResponse = new HttpResponse(FakeRequest, "fake-content");

        [Fact]
        public void ReducerConcatResponses()
        {
            var state = HttpState.Reducer(HttpState.Initial, FakeResponse);
            Assert.True(state.Responses.Contains(FakeResponse));
        }
    }
}