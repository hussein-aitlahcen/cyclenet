using System.Collections.Generic;
using Cycle.Net.Driver;

namespace Cycle.Net
{
    public interface ISource
    {
        void AddDriver(IDriver driver);

        IDriver GetDriver(string identifier);

        IEnumerable<IDriver> GetDrivers();
    }
}