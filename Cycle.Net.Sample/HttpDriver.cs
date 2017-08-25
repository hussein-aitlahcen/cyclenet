using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
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

    public sealed class HttpDriver : AbstractDriver
    {
        public static string ID => "http-driver";

        public HttpDriver() : base(ID)
        {
        }

        public override void OnNext(IRequest value)
        {
            switch (value)
            {
                case HttpRequest httpRequest:
                    // Our driver should retrieve the content from the URL
                    Dispatch(new HttpResponse
                    {
                        Origin = httpRequest,
                        Content = "content"
                    });
                    break;
            }
        }
    }
}