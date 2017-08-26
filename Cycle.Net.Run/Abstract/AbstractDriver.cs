using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Cycle.Net.Run.Abstract
{
    public abstract class AbstractDriver : AbstractObservable<IResponse>, IDriver
    {
        public string Id { get; }

        public AbstractDriver(string id)
        {
            Id = id;
        }

        public abstract void OnNext(IRequest value);

        public virtual void OnCompleted() { }

        public virtual void OnError(Exception error) { }
    }
}