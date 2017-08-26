using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Cycle.Net.Run;
using Cycle.Net.Run.Abstract;

namespace Cycle.Net.Sample
{
    public sealed class LogRequest : IRequest
    {
        public string Content { get; }
        public LogRequest(string content) => Content = content;

        public override string ToString() =>
            $"LogRequest(contentLength={Content})";
    }

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