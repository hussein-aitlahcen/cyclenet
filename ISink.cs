using System;
using Cycle.Net.Driver;

namespace Cycle.Net
{
    public interface ISink : IObservable<IRequest>
    {
    }
}