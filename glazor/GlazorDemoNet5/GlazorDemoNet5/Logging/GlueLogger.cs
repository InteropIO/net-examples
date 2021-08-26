using System;
using Glue.Logging;
using Microsoft.Extensions.Logging;

namespace GlazorDemoNet5.Logging
{
    internal class GlueLogger : IGlueLog
    {
        private readonly ILogger logger_;

        public GlueLogger(ILogger logger)
        {
            logger_ = logger;
        }

        public void Info(string message, Exception e = null)
        {
            Log(GlueLogLevel.Info, message, e);
        }

        public void Error(string message, Exception e = null)
        {
            Log(GlueLogLevel.Error, message, e);
        }

        public void Debug(string message, Exception e = null)
        {
            Log(GlueLogLevel.Debug, message, e);
        }

        public void Debug(Func<string> message, Exception e = null)
        {
            Log(GlueLogLevel.Debug, message(), e);
        }

        public void Trace(Func<string> message, Exception e = null)
        {
            Log(GlueLogLevel.Trace, message(), e);
        }

        public bool IsEnabledFor(GlueLogLevel level)
        {
            return logger_.IsEnabled(GetLogLevel(level));
        }

        public void Warn(string message, Exception e = null)
        {
            Log(GlueLogLevel.Warn, message, e);
        }

        public void Log(GlueLogLevel level, string message, Exception exception)
        {
            logger_.Log(GetLogLevel(level), exception, message);
        }

        private LogLevel GetLogLevel(GlueLogLevel level)
        {
            return level switch
            {
                GlueLogLevel.Trace => LogLevel.Trace,
                GlueLogLevel.Debug => LogLevel.Debug,
                GlueLogLevel.Info => LogLevel.Information,
                GlueLogLevel.Warn => LogLevel.Warning,
                GlueLogLevel.Error => LogLevel.Error,
                GlueLogLevel.Fatal => LogLevel.Critical,
                GlueLogLevel.Off => LogLevel.None,
                _ => LogLevel.Information
            };
        }
    }
}