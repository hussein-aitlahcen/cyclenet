using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Cycle.Net.Run.Abstract;

namespace Cycle.Net.Sample.Driver
{
    public sealed class ApiRequest : IRequest
    {
        public string Id { get; }
        public string Url { get; }
        public ApiRequest(string id, string url)
        {
            Id = id;
            Url = url;
        }

        public override string ToString() =>
            $"ApiRequest(id={Id}, url={Url})";
    }

    public sealed class ApiResponse : IResponse
    {
        public ApiRequest Origin { get; }
        public string Content { get; }
        public ApiResponse(ApiRequest origin, string content)
        {
            Origin = origin;
            Content = content;
        }

        public override string ToString() =>
            $"ApiResponse(origin={Origin}, contentLength={Content.Length}";
    }

    public static class ApiDriver
    {
        public const string ID = "api-driver";

        public static Func<IObservable<IRequest>, IObservable<IResponse>> Create(IScheduler scheduler) =>
            requests => requests
                .OfType<ApiRequest>()
                .SelectMany
                (
                    request => Observable
                        .FromAsync(() => new HttpClient().GetStringAsync(request.Url), scheduler)
                        .Select(content => new ApiResponse(request, content))
                );
    }
}