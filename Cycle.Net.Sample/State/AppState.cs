namespace Cycle.Net.Sample.State
{
    public class AppState
    {
        public HttpState Http { get; }
        public TcpState Tcp { get; }
        public AppState(HttpState httpState, TcpState tcpState)
        {
            Http = httpState;
            Tcp = tcpState;
        }

        public static AppState Combine(HttpState httpState, TcpState tcpState) =>
            new AppState(httpState, tcpState);
    }
}