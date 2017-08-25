using System.Collections.Generic;
using System.Linq;
using Cycle.Net.Run.Abstract;

namespace Cycle.Net.Run
{
    public class SimpleSource : ISource
    {
        private List<IDriver> m_drivers;

        public SimpleSource() : this(Enumerable.Empty<IDriver>()) { }

        public SimpleSource(IEnumerable<IDriver> drivers) =>
            m_drivers = new List<IDriver>(drivers);

        public void AddDriver(IDriver driver) =>
            m_drivers.Add(driver);

        public IDriver GetDriver(string id) =>
            m_drivers.First(driver => driver.Id == id);

        public IEnumerable<IDriver> GetDrivers() =>
            m_drivers;
    }
}