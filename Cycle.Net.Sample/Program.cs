using System;
using Cycle.Net.Run;
using Cycle.Net.Run.Abstract;
using System.Reactive.Linq;

namespace Cycle.Net.Sample
{
    class Program
    {
        static readonly IDriver[] Drivers = new[]
        {
            new HttpDriver()
        };

        static void Main(string[] args)
        {
            new CycleNet<SimpleSource>(new SimpleSource(Drivers)).Run(Flow);
        }

        static IObservable<IRequest> Flow(SimpleSource source)
        {
            var stream = source.GetDriver(HttpDriver.ID)
                .OfType<HttpResponse>()
                .StartWith(new HttpResponse
                {
                    Origin = new HttpRequest
                    {
                        Id = "1",
                        Url = "test"
                    },
                    Content = "test"
                });

            var firstStep = stream.Where(response => response.Origin.Id == "1")
                .Do(response => Console.WriteLine("Step1"))
                .Select(response => new HttpRequest
                {
                    Id = "2",
                    Url = "step1"
                });

            var secondStep = stream.Where(response => response.Origin.Id == "2")
                .Do(response => Console.WriteLine("Step2"))
                .Select(response => new HttpRequest
                {
                    Id = "3",
                    Url = "step2"
                });

            var thirdStep = stream.Where(response => response.Origin.Id == "3")
                .Do(response => Console.WriteLine("Step3"))
                .Select(response => new HttpRequest
                {
                    Id = "4",
                    Url = "step3"
                });


            return Observable.Merge(firstStep, secondStep, thirdStep);
        }
    }
}
