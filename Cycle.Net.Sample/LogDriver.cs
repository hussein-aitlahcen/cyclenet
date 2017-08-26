using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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

    public sealed class LogDriver : AbstractDriver
    {
        public static string ID => "log-driver";

        public LogDriver() : base(ID)
        {
        }

        public override void OnNext(IRequest value)
        {
            switch (value)
            {
                case LogRequest logRequest:
                    Console.WriteLine(logRequest.Content);
                    break;
            }
        }
    }
}