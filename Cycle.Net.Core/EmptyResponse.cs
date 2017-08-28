using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Core
{
    public sealed class EmptyResponse : IResponse
    {
        public static EmptyResponse Instance => new EmptyResponse();
    }
}