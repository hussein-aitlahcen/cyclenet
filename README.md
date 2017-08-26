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

A basic implementation of the HttpDriver and LogDriver are given in the [Sample](https://github.com/hussein-aitlahcen/cyclenet/tree/master/Cycle.Net.Sample) directory and here is an exemple of pure dataflow, fully functionnal and immutable (function **Flow**)

```csharp
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
```
