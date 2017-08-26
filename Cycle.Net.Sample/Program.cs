using System;
using Cycle.Net.Run;
using Cycle.Net.Run.Abstract;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace Cycle.Net.Sample
{
    using Driver = IObservable<IResponse>;
    using DriverMaker = Func<IObservable<IRequest>, IObservable<IResponse>>;
    using Drivers = Dictionary<string, Func<IObservable<IRequest>, IObservable<IResponse>>>;

    class Program
    {
        static void Main(string[] args)
        {
            new CycleNet().Run(Flow, new Drivers()
            {
                [LogDriver.ID] = LogDriver.Create,
                [HttpDriver.ID] = HttpDriver.Create(new EventLoopScheduler())
            });
            Console.Read();
        }

        static HttpRequest RequestPosts = new HttpRequest("posts", "https://jsonplaceholder.typicode.com/posts");
        static HttpRequest RequestUsers = new HttpRequest("users", "https://jsonplaceholder.typicode.com/users");
        static HttpRequest RequestComments = new HttpRequest("comments", "https://jsonplaceholder.typicode.com/comments");

        class State
        {
            public static State Initial = new State(ImmutableList.Create<HttpResponse>());
            public ImmutableList<HttpResponse> Responses { get; }
            public State(ImmutableList<HttpResponse> responses)
            {
                Responses = responses;
            }
        }

        static IObservable<State> StateStream(IObservable<HttpResponse> httpStream) =>
            httpStream
                .Scan(State.Initial, (state, response) => new State(state.Responses.Add(response)));

        static IObservable<LogRequest> LogSink(IObservable<State> stateStream, IObservable<HttpResponse> httpStream) =>
            httpStream
                .Zip
                (
                    stateStream,
                    (response, state) => new LogRequest($"nb of responses: {state.Responses.Count}, data received: {response}")
                );

        static IObservable<IRequest> Ignore(IObservable<IResponse> stream) =>
            stream.Select(response => EmptyRequest.Instance);

        static IObservable<IRequest> Flow(ISource source)
        {
            var httpStream = source.GetDriver(HttpDriver.ID)
                .OfType<HttpResponse>();
            var logStream = source.GetDriver(LogDriver.ID)
                .OfType<LogResponse>();

            var stateStream = StateStream(httpStream);

            var logSink = LogSink(stateStream, httpStream);

            var logAckSink = Ignore(logStream);

            var httpSink = new[]
                {
                    RequestPosts,
                    RequestUsers,
                    RequestComments
                }.ToObservable();

            return Observable.Merge<IRequest>(httpSink, logSink, logAckSink);
        }
    }
}
