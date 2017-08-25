namespace Cycle.Net.Driver
{
    public interface IEnvelope
    {
        string DriverId { get; }
        IRequest Request { get; }
    }
}