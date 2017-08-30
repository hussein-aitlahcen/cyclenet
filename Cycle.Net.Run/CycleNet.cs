using System;
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
            var requestSubject = new Subject<IRequest>();
            var responseSubject = new Subject<IResponse>();
            // make sure our source and sink are cached and subscription happen on the scheduler
            var sink = requestSubject.AsObservable().SubscribeOn(Scheduler);
            var source = responseSubject.AsObservable().SubscribeOn(Scheduler);
            foreach (var driver in drivers)
            {
                driver(sink)
                    // ignore driver that does not give an output
                    .Where(response => !(response is EmptyResponse))
                    .Subscribe(responseSubject.AsObserver());
            }
            main(source)
                .Subscribe(requestSubject.AsObserver());
        }
    }
}