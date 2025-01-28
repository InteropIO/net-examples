using System;
using System.Threading;
using Glue.Logging;
using log4net;
using log4net.Core;

namespace MultiWindowWPFDemo
{
    public class Log4NetLoggerFactory : IGlueLoggerFactory
    {
        public IGlueLog GetLogger(string name)
        {
            return new Log4NetGlueLog(LogManager.GetLogger(name));
        }

        public IGlueLog GetLogger(Type type)
        {
            return new Log4NetGlueLog(LogManager.GetLogger(type));
        }

        public static Level GetLog4NetLevel(GlueLogLevel level)
        {
            return level switch
            {
                GlueLogLevel.All => Level.All,
                GlueLogLevel.Trace => Level.Trace,
                GlueLogLevel.Debug => Level.Debug,
                GlueLogLevel.Info => Level.Info,
                GlueLogLevel.Warn => Level.Warn,
                GlueLogLevel.Error => Level.Error,
                GlueLogLevel.Fatal => Level.Fatal,
                GlueLogLevel.Off => Level.Off,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
            };
        }

        public class Log4NetGlueLog : IGlueLog
        {
            private readonly ILog log_;

            public Log4NetGlueLog(ILog log)
            {
                log_ = log;
            }

            public void Trace(Func<string> messageCtor, Exception e = null)
            {
                if (log_.Logger.IsEnabledFor(Level.Trace))
                {
                    log_.Logger.Log(new LoggingEvent(new LoggingEventData
                    {
                        Level = Level.Trace,
                        Message = messageCtor(),
                        LoggerName = log_.Logger.Name,
                        ThreadName = Thread.CurrentThread.Name,
                        TimeStampUtc = DateTime.UtcNow,
                        ExceptionString = e?.ToString()
                    }));
                }
            }

            public void Info(string message, Exception e = null)
            {
                log_.Info(message, e);
            }

            public void Error(string message, Exception e = null)
            {
                log_.Error(message, e);
            }

            public void Debug(string message, Exception e = null)
            {
                log_.Debug(message, e);
            }

            public void Debug(Func<string> message, Exception e = null)
            {
                if (log_.IsDebugEnabled)
                {
                    log_.Debug(message(), e);
                }
            }

            public bool IsEnabledFor(GlueLogLevel level)
            {
                return log_.Logger.IsEnabledFor(GetLog4NetLevel(level));
            }

            public void Warn(string message, Exception e = null)
            {
                log_.Warn(message, e);
            }

            public void Log(GlueLogLevel level, string message, Exception exception)
            {
                log_.Logger.Log(typeof(LogImpl), GetLog4NetLevel(level), message, exception);
            }
        }
    }
}