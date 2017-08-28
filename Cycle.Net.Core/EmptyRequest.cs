using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Core
{
    public sealed class EmptyRequest : IRequest
    {
        public static EmptyRequest Instance => new EmptyRequest();
    }
}