using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Http
{
    public interface IHttpRequest : IRequest
    {
        string Id { get; }
        string Url { get; }
    }
}