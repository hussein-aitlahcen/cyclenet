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

A basic implementation of the HttpDriver is given in the [Sample](https://github.com/hussein-aitlahcen/cyclenet/tree/master/Cycle.Net.Sample) directory and here is the pure dataflow

```csharp
static IObservable<IRequest> Flow(SimpleSource source)
        {
            var httpSource = source.GetDriver(HttpDriver.ID)
                .OfType<HttpResponse>()
                .StartWith(InitialResponse);

            var firstStep = httpSource.Where(response => response == InitialResponse)
                .Do(response => Console.WriteLine("fetching posts"))
                .Select(response => new HttpRequest
                {
                    Id = "posts",
                    Url = "https://jsonplaceholder.typicode.com/posts"
                });

            var secondStep = httpSource.Where(response => response.Origin.Id == "posts")
                .Do(response => Console.WriteLine($"posts fetched, length={response.Content.Length}, fetching users"))
                .Select(response => new HttpRequest
                {
                    Id = "users",
                    Url = "https://jsonplaceholder.typicode.com/users"
                });

            var thirdStep = httpSource.Where(response => response.Origin.Id == "users")
                .Do(response => Console.WriteLine($"users fetched, length={response.Content.Length}, fetching comments"))
                .Select(response => new HttpRequest
                {
                    Id = "comments",
                    Url = "https://jsonplaceholder.typicode.com/comments"
                });

            var lastStep = httpSource.Where(response => response.Origin.Id == "comments")
                .Do(response => Console.WriteLine($"comments fetched, length={response.Content.Length}"))
                .Select(response => EmptyRequest.Instance);

            return Observable.Merge<IRequest>(firstStep, secondStep, thirdStep, lastStep);
        }
```
