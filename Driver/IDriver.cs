using System;

namespace Cycle.Net.Driver
{
    public interface IDriver : IObserver<IRequest>, IObservable<IResponse>
    {
        string Id { get; }
    }
}