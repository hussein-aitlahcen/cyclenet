using System;
using System.Collections.Generic;

namespace Cycle.Net.Core.Abstract
{
    using Driver = IObservable<IResponse>;

    public interface ISource
    {
        void AddDriver(string id, Driver driver);

        Driver GetDriver(string id);

        IEnumerable<Driver> GetDrivers();
    }
}