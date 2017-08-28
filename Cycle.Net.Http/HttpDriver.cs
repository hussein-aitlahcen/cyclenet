using System;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Http
{
    public static class HttpDriver
    {
        public const string ID = "http-driver";

        public static Func<IObservable<IRequest>, IObservable<IHttpResponse>> Create() =>
            requests => requests
                    .OfType<HttpRequest>()
                    .SelectMany
                    (
                        request => Task.Run(() => ExecuteRequest(request))
                                .ToObservable()
                                .DistinctUntilChanged()
                                .Catch((Exception exception) => Observable.Return(new HttpError(request, exception)))
                    );

        private static async Task<IHttpResponse> ExecuteRequest(HttpRequest request)
        {
            var client = new HttpClient();
            var content = await client.GetAsync(request.Url);
            return new HttpResponse(request, content);
        }
    }
}