using System;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
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
                    request => ExecuteRequest(request)
                            .ToObservable(scheduler)
                            .Catch((Exception exception) => Observable.Return(new HttpError(request, exception)))
                );

        private static async Task<IHttpResponse> ExecuteRequest(HttpRequest request)
        {
            var client = new HttpClient();
            var content = await client.GetStringAsync(request.Url);
            return new HttpResponse(request, content);
        }
    }
}