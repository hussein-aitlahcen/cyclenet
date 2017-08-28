using System;
using System.Net.Http;
using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Http
{
    public interface IHttpResponse : IResponse
    {
        IHttpRequest Origin { get; }
    }
}