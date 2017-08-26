using System;
using System.Collections.Generic;
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

    public sealed class HttpDriver : AbstractDriver
    {
        public static string ID => "http-driver";

        private readonly IScheduler m_scheduler;

        public HttpDriver(IScheduler sheduler) : base(ID)
        {
            m_scheduler = sheduler;
        }

        public override void OnNext(IRequest value)
        {
            switch (value)
            {
                case HttpRequest httpRequest:
                    var client = new System.Net.Http.HttpClient();
                    Observable.FromAsync(() => client.GetStringAsync(httpRequest.Url))
                        .Select(content => new HttpResponse(httpRequest, content))
                        .ObserveOn(m_scheduler)
                        .Subscribe(response =>
                        {
                            NotifyNext(response);
                        }, error =>
                        {
                            NotifyError(error);
                        });
                    break;
            }
        }
    }
}