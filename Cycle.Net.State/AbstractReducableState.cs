using Cycle.Net.Core.Abstract;

namespace Cycle.Net.State
{
    public abstract class AbstractReducableState<TState> : IState
        where TState : AbstractReducableState<TState>
    {
        public abstract TState Reduce(IResponse response);
    }
}