namespace Cycle.Net.Log
{
    public sealed class LogRequest : ILogRequest
    {
        public string Content { get; }
        public LogRequest(string content) => Content = content;

        public override string ToString() =>
            $"LogRequest(contentLength={Content})";
    }
}