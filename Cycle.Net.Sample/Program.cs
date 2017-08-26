using System;
using Cycle.Net.Run;
using Cycle.Net.Run.Abstract;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

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

        static readonly HttpResponse InitialResponse =
            new HttpResponse
            (
                new HttpRequest("init", "init"),
                "init"
            );

        static IObservable<IRequest> Flow(SimpleSource source)
        {
            var httpSource = source.GetDriver(HttpDriver.ID)
                .OfType<HttpResponse>()
                .StartWith(InitialResponse);

            var logging = httpSource
                .Zip
                (
                    httpSource.Scan(0, (counter, response) => counter + 1),
                    (counter, response) => new LogRequest($"nb of response: {counter}, data received: {response}")
                );

            var fetchPosts = httpSource.Where(response => response == InitialResponse)
                .Select(response => new HttpRequest("posts", "https://jsonplaceholder.typicode.com/posts"));

            var fetchUsers = httpSource.Where(response => response.Origin.Id == "posts")
                .Select(response => new HttpRequest("users", "https://jsonplaceholder.typicode.com/users"));

            var fetchComments = httpSource.Where(response => response.Origin.Id == "users")
                .Select(response => new HttpRequest("comments", "https://jsonplaceholder.typicode.com/comments"));

            return Observable.Merge<IRequest>(logging, fetchPosts, fetchUsers, fetchComments);
        }
    }
}
