using System;

namespace HttpSiraStatus.WebSockets
{
    public interface ILogger
    {
        string File { get; set; }
        LogLevel Level { get; set; }
        Action<LogData, string> Output { get; set; }

        void Debug(string message);
        void Error(string message);
        void Fatal(string message);
        void Info(string message);
        void Trace(string message);
        void Warn(string message);
    }
}