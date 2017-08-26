<h1 align="center">Cycle.Net</h1>

<div align="center">
  <strong>A functional and reactive framework for predictable code</strong>
  <i>Heavily based on <a href="https://github.com/cyclejs/cyclejs">cycle.js</a></i>
</div>

## Introduction
This project aim to port the Cycle.js pattern to .Net

## Contributions
Feel free to fork and make PR, any help will be appreciated !

## Sample

A basic implementation of the HttpDriver and LogDriver are given in the [Sample](https://github.com/hussein-aitlahcen/cyclenet/tree/master/Cycle.Net.Sample) directory and here is the pure dataflow (function **Flow**)

```csharp
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
```
