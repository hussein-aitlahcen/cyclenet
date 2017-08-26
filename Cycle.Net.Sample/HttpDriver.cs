using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Cycle.Net.Run.Abstract;

namespace Cycle.Net.Sample
{
    public sealed class HttpRequest : IRequest
    {
        public string Id { get; }
        public string Url { get; }
        public HttpRequest(string id, string url)
        {
            Id = id;
            Url = url;
        }

        public override string ToString() =>
            $"HttpRequest(id={Id}, url={Url})";
    }

    public sealed class HttpResponse : IResponse
    {
        public HttpRequest Origin { get; }
        public string Content { get; }
        public HttpResponse(HttpRequest origin, string content)
        {
            Origin = origin;
            Content = content;
        }

        public override string ToString() =>
            $"HttpResponse(origin={Origin}, contentLength={Content.Length}";
    }

    public static class HttpDriver
    {
        public const string ID = "http-driver";

        public static Func<IObservable<IRequest>, IObservable<IResponse>> Create(IScheduler scheduler) =>
            requests => requests
                .OfType<HttpRequest>()
                .SelectMany
                (
                    request => Observable
                        .FromAsync(() => new HttpClient().GetStringAsync(request.Url))
                        .Select(content => new HttpResponse(request, content))
                );
    }
}