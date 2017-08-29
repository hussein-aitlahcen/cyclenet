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
            var sink = requestSubject
                .SubscribeOn(Scheduler);
            var source = responseSubject
                .SubscribeOn(Scheduler);
            foreach (var driver in drivers)
            {
                driver(sink)
                    .Where(response => !(response is EmptyResponse))
                    .Publish()
                    .RefCount()
                    .Subscribe(responseSubject);
            }
            main(source)
                .Publish()
                .RefCount()
                .Subscribe(requestSubject);
        }
    }
}