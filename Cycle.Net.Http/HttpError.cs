using System;
using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Http
{
    public sealed class HttpError : IHttpResponse
    {
        public IHttpRequest Origin { get; }
        public Exception Error { get; }
        public HttpError(IHttpRequest origin, Exception error)
        {
            Origin = origin;
            Error = error;
        }

        public override string ToString() =>
            $"HttpError(origin={Origin}, error={Error}";
    }
}