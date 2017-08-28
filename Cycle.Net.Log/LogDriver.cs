using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Cycle.Net.Core;
using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Log
{

    public sealed class LogDriver
    {
        public const string ID = "log-driver";

        public static IObservable<IResponse> Create(IObservable<IRequest> requests) =>
            requests
                .OfType<LogRequest>()
                .Do(request => Console.WriteLine(request.Content))
                .Select(request => EmptyResponse.Instance);
    }
}