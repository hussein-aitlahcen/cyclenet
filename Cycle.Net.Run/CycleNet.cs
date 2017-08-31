using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Collections.Generic;
using Cycle.Net.Core.Abstract;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using Cycle.Net.Core;

namespace Cycle.Net.Run
{
    using Driver = IObservable<IResponse>;
    using DriverMaker = Func<IObservable<IRequest>, IObservable<IResponse>>;
    using Drivers = IEnumerable<Func<IObservable<IRequest>, IObservable<IResponse>>>;

    public class CycleNet
    {
        public static IScheduler Scheduler = new EventLoopScheduler();

        public static void Run(Func<IObservable<IResponse>, IObservable<IRequest>> main, Drivers drivers)
        {
            var proxy = new Subject<IRequest>();
            var observableProxy = proxy.AsObservable();
            var observerProxy = proxy.AsObserver();
            main(Observable.Merge(drivers.Select(driver =>
                    driver(observableProxy)
                        .Where(response => !(response is EmptyResponse))
                    )
                )
                .SubscribeOn(Scheduler)
                .ObserveOn(Scheduler)
                .Publish()
                .RefCount()
            )
            .Subscribe(observerProxy);
        }
    }
}