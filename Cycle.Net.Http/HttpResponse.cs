using System;
using System.Net.Http;
using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Http
{
    public class HttpResponse : IHttpResponse, IDisposable
    {
        public IHttpRequest Origin { get; }
        public HttpResponseMessage Message { get; }
        public HttpResponse(IHttpRequest origin, HttpResponseMessage message)
        {
            Origin = origin;
            Message = message;
        }
        public void Dispose() => Message.Dispose();

        public override string ToString() =>
            $"HttpResponse(origin={Origin}, message={Message}";
    }
}