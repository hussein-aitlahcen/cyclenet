using System;
using Cycle.Net.Run;
using Cycle.Net.Run.Abstract;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace Cycle.Net.Sample
{
    class Program
    {
        static readonly IDriver[] Drivers = new[]
        {
            new HttpDriver(DefaultScheduler.Instance)
        };

        static void Main(string[] args)
        {
            new CycleNet<SimpleSource>(new SimpleSource(Drivers)).Run(Flow);
            Console.Read();
        }

        static readonly HttpResponse InitialResponse = new HttpResponse
        {
            Origin = new HttpRequest
            {
                Id = "init",
                Url = "init"
            },
            Content = "init"
        };

        static IObservable<IRequest> Flow(SimpleSource source)
        {
            Console.WriteLine("starting threadId: " + Environment.CurrentManagedThreadId);

            var stream = source.GetDriver(HttpDriver.ID)
                .OfType<HttpResponse>()
                .StartWith(InitialResponse);

            IObservable<IRequest> firstStep = stream.Where(response => response == InitialResponse)
                .Do(response => Console.WriteLine("fetching posts"))
                .Select(response => new HttpRequest
                {
                    Id = "posts",
                    Url = "https://jsonplaceholder.typicode.com/posts"
                });

            IObservable<IRequest> secondStep = stream.Where(response => response.Origin.Id == "posts")
                .Do(response => Console.WriteLine("posts fetched, fetching users"))
                .Select(response => new HttpRequest
                {
                    Id = "users",
                    Url = "https://jsonplaceholder.typicode.com/users"
                });

            IObservable<IRequest> thirdStep = stream.Where(response => response.Origin.Id == "users")
                .Do(response => Console.WriteLine("users fetched, fetching comments"))
                .Select(response => new HttpRequest
                {
                    Id = "comments",
                    Url = "https://jsonplaceholder.typicode.com/comments"
                });

            IObservable<IRequest> lastStep = stream.Where(response => response.Origin.Id == "comments")
                .Do(response => Console.WriteLine("comments fetched"))
                .Select(response => EmptyRequest.Instance);

            return Observable.Merge(firstStep, secondStep, thirdStep, lastStep);
        }
    }
}
