using System;
using Cycle.Net.Run;
using Cycle.Net.Run.Abstract;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Collections.Immutable;

namespace Cycle.Net.Sample
{
    class Program
    {
        static readonly IDriver[] Drivers = new IDriver[]
        {
            new HttpDriver(DefaultScheduler.Instance),
            new LogDriver()
        };

        static void Main(string[] args)
        {
            new CycleNet<SimpleSource>(new SimpleSource(Drivers)).Run(Flow);
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

        static IObservable<IRequest> Flow(SimpleSource source)
        {
            var httpSource = source.GetDriver(HttpDriver.ID)
                .OfType<HttpResponse>();

            var stateStream = httpSource
                .Scan(State.Initial, (state, response) => new State(state.Responses.Add(response)))
                .StartWith(State.Initial);

            var logStream = httpSource
                .Zip
                (
                    stateStream,
                    (response, state) => new LogRequest($"nb of response: {state.Responses.Count}, data received: {response}")
                );

            var fetchPosts = stateStream.Where(state => state.Responses.Count == 0)
                .Select(state => RequestPosts);

            var fetchUsers = stateStream.Where(state => state.Responses.Count == 1)
                .Select(state => RequestUsers);

            var fetchComments = stateStream.Where(state => state.Responses.Count == 2)
                .Select(state => RequestComments);

            return Observable.Merge<IRequest>(logStream, fetchPosts, fetchUsers, fetchComments);
        }
    }
}
