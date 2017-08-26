using Cycle.Net.Run.Abstract;

namespace Cycle.Net.Run
{
    public sealed class EmptyResponse : IResponse
    {
        public static EmptyResponse Instance => new EmptyResponse();
    }
}