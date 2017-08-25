using System.Collections.Generic;

namespace Cycle.Net.Abstract
{
    public interface ISource
    {
        void AddDriver(IDriver driver);

        IDriver GetDriver(string identifier);

        IEnumerable<IDriver> GetDrivers();
    }
}