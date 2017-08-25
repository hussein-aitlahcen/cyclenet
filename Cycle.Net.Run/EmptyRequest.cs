using Cycle.Net.Run.Abstract;

namespace Cycle.Net.Run
{
    public sealed class EmptyRequest : IRequest
    {
        public static EmptyRequest Instance => new EmptyRequest();
    }
}