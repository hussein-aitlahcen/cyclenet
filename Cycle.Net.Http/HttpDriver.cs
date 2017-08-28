using System;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Http
{
    public static class HttpDriver
    {
        public const string ID = "http-driver";

        public static Func<IObservable<IRequest>, IObservable<IHttpResponse>> Create(IScheduler scheduler) =>
            requests => requests
                .OfType<HttpRequest>()
                .SelectMany
                (
                    request => Observable
                        .FromAsync(() => new HttpClient().GetStringAsync(request.Url), scheduler)
                        .Select(content => new HttpResponse(request, content))
                );
    }
}