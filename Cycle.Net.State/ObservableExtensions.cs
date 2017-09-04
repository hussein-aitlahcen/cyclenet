using System;
using System.Reactive.Linq;
using Cycle.Net.Core.Abstract;

namespace Cycle.Net.State
{
    public static class ObservableExtensions
    {
        public static IObservable<TState> ToState<TState>(this IObservable<IResponse> responseStream, TState initial)
            where TState : AbstractReducableState<TState>
        {
            return responseStream
                        .Scan
                        (
                            initial,
                            (state, action) => state.Reduce(action)
                        );
        }

        public static IObservable<TCombined> CombineState<TStateA, TStateB, TCombined>(
            this IObservable<TStateA> stateAStream,
            IObservable<TStateB> stateBStream,
            Func<TStateA, TStateB, TCombined> selector)
        {
            return stateAStream
                        .CombineLatest(stateBStream, selector);
        }
    }
}