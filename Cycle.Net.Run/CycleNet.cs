using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Collections.Generic;
using Cycle.Net.Run.Abstract;
using System.Reactive.Subjects;

namespace Cycle.Net.Run
{
    using Driver = IObservable<IResponse>;
    using DriverMaker = Func<IObservable<IRequest>, IObservable<IResponse>>;
    using Drivers = Dictionary<string, Func<IObservable<IRequest>, IObservable<IResponse>>>;

    public class CycleNet : AbstractObservable<IRequest>, IObserver<IRequest>
    {
        public void Run(Func<ISource, IObservable<IRequest>> main, Drivers drivers)
        {
            var source = new SimpleSource();
            var ignore = Observable.Empty<IRequest>();
            foreach (var driver in drivers)
            {
                var stream = driver.Value(this);
                ignore = ignore.Merge(stream.OfType<EmptyResponse>().SelectMany(response => Observable.Empty<IRequest>()));
                source.AddDriver(driver.Key, stream);
            }
            main(source).Merge(ignore).Subscribe(this);
        }

        public void OnCompleted() =>
            NotifyCompleted();

        public void OnError(Exception error) =>
            NotifyError(error);

        public void OnNext(IRequest value) =>
            NotifyNext(value);
    }
}