namespace Cycle.Net.Abstract
{
    public interface IEnvelope
    {
        string DriverId { get; }
        IRequest Request { get; }
    }
}