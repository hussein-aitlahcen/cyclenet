using System;

namespace Cycle.Net.Abstract
{
    public interface IDriver : IObserver<IRequest>, IObservable<IResponse>
    {
        string Id { get; }
    }
}