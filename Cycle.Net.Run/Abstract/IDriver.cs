using System;

namespace Cycle.Net.Run.Abstract
{
    public interface IDriver : IObserver<IRequest>, IObservable<IResponse>
    {
        string Id { get; }
    }
}