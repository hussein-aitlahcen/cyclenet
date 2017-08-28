using System;
using System.Collections.Generic;
using System.Linq;
using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Core
{
    using Driver = IObservable<IResponse>;

    public class SimpleSource : ISource
    {
        private readonly Dictionary<string, Driver> m_drivers;

        public SimpleSource() : this(new Dictionary<string, Driver>()) { }

        public SimpleSource(Dictionary<string, Driver> drivers) =>
            m_drivers = drivers;

        public void AddDriver(string id, Driver driver) =>
            m_drivers[id] = driver;

        public Driver GetDriver(string id) =>
            m_drivers[id];

        public IEnumerable<Driver> GetDrivers() =>
            m_drivers.Values;
    }
}