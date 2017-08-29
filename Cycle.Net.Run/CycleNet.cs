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
            // replay subjects ensure initial requests and responses are dispatched
            var requestSubject = new Subject<IRequest>();
            var responseSubject = new Subject<IResponse>();
            // make sure our source and sink are cached
            var sink = requestSubject.ObserveOn(Scheduler).SubscribeOn(Scheduler).Publish().RefCount();
            var source = responseSubject.ObserveOn(Scheduler).SubscribeOn(Scheduler).Publish().RefCount();
            foreach (var driver in drivers)
            {
                driver(sink)
                    // ignore driver that does not give an output
                    .Where(response => !(response is EmptyResponse))
                    .SubscribeOn(Scheduler)
                    .Subscribe(responseSubject);
            }
            main(source)
                .SubscribeOn(Scheduler)
                .Subscribe(requestSubject);
        }
    }
}