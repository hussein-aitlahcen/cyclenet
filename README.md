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

    static IObservable<IRequest> Flow(ISource source)
    {
        var httpStream = source.GetDriver(HttpDriver.ID)
            .OfType<HttpResponse>();

        var stateStream = StateStream(httpStream);

        var logSink = LogSink(stateStream, httpStream);

        var httpSink = new[]
            {
                RequestPosts,
                RequestUsers,
                RequestComments
            }.ToObservable();

        return Observable.Merge<IRequest>(httpSink, logSink);
    }
}
```
