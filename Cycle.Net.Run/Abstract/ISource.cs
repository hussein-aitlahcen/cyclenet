using System.Collections.Generic;

namespace Cycle.Net.Run.Abstract
{
    public interface ISource
    {
        void AddDriver(IDriver driver);

        IDriver GetDriver(string id);

        IEnumerable<IDriver> GetDrivers();
    }
}