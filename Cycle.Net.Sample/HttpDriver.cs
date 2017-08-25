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
        public string Id { get; set; }
        public string Url { get; set; }
    }

    public sealed class HttpResponse : IResponse
    {
        public HttpRequest Origin { get; set; }
        public string Content { get; set; }
    }

    public sealed class HttpDriver : AbstractDriver, IObserver<HttpResponse>
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
                    // Our driver should retrieve the content from the URL
                    var client = new System.Net.Http.HttpClient();
                    Observable.FromAsync(() => client.GetStringAsync(httpRequest.Url))
                        .Select(content => new HttpResponse
                        {
                            Origin = httpRequest,
                            Content = content
                        })
                        .ObserveOn(m_scheduler)
                        .Subscribe(this);
                    break;
            }
        }

        public void OnNext(HttpResponse value)
        {
            Console.WriteLine("callback thread: " + Environment.CurrentManagedThreadId);
            Dispatch(value);
        }
    }
}