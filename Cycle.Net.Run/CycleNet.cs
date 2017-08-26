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
            foreach (var driver in drivers)
            {
                source.AddDriver(driver.Key, driver.Value(this));
            }
            main(source).Subscribe(this);
        }

        public void OnCompleted() =>
            NotifyCompleted();

        public void OnError(Exception error) =>
            NotifyError(error);

        public void OnNext(IRequest value) =>
            NotifyNext(value);
    }
}