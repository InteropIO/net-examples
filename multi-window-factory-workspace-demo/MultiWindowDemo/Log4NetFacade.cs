using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DOT.Core.Logging;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;

namespace MultiWindowFactoryDemo;

public class Log4NetFacade : IDotLoggingFacade
{
    public IDotLog GetLogger(string loggerName)
    {
        return new DotLog(loggerName);
    }

    public List<string> GetLogFolders()
    {
        // ensure repo is initialized
        var logger = GetLogger(GetType().FullName);

        var fileAppenders = GetFileAppenders();

        var dirs = new HashSet<string>(fileAppenders.Select(fa => Path.GetDirectoryName(fa.File)));

        return dirs.ToList();
    }

    public List<string> GetLogFileNames()
    {
        // ensure repo is initialized
        var logger = GetLogger(GetType().FullName);

        var fileAppenders = GetFileAppenders();

        var files = new HashSet<string>(fileAppenders.Select(fa => fa.File));

        return files.ToList();
    }

    private static IEnumerable<FileAppender> GetFileAppenders()
    {
        var repository = LogManager.GetRepository();

        var fileAppenders =
            repository.GetAppenders().Where(iAppender => iAppender is FileAppender).Cast<FileAppender>();
        return fileAppenders;
    }

    private class DotLog : IDotLog
    {
        static DotLog()
        {
            DeclaringType = typeof(DotLog);
        }

        public DotLog(string loggerName)
        {
            Log4NetLogger = LogManager.GetLogger(loggerName);
        }

        internal DotLog(ILog log4NetLogger)
        {
            Log4NetLogger = log4NetLogger;
        }

        private static Type DeclaringType { get; }

        private ILog Log4NetLogger { get; }

        public bool IsEnabled(DotLogLevel logLevel)
        {
            return Log4NetLogger.Logger.IsEnabledFor(GetLevel(logLevel));
        }

        public void Log(DotLogLevel logLevel, object message)
        {
            Log4NetLogger.Logger.Log(DeclaringType, GetLevel(logLevel), message, null);
        }

        public void Log(
            DotLogLevel logLevel,
            object message,
            Exception exception)
        {
            Log4NetLogger.Logger.Log(DeclaringType, GetLevel(logLevel), message, exception);
        }

        public void FlushBuffers()
        {
            ILoggerRepository rep = LogManager.GetRepository();
            foreach (IAppender appender in rep.GetAppenders())
            {
                if (appender is BufferingAppenderSkeleton buffered)
                {
                    buffered.Flush();
                }
            }
        }

        public bool IsTraceEnabled => Log4NetLogger.Logger.IsEnabledFor(Level.Trace);

        public bool IsDebugEnabled => Log4NetLogger.IsDebugEnabled;

        public bool IsInfoEnabled => Log4NetLogger.IsInfoEnabled;

        public bool IsWarnEnabled => Log4NetLogger.IsWarnEnabled;

        public bool IsErrorEnabled => Log4NetLogger.IsErrorEnabled;

        public bool IsFatalEnabled => Log4NetLogger.IsFatalEnabled;

        public DotLogLevel SetLogLevel(DotLogLevel logLevel)
        {
            var logger = (Logger)Log4NetLogger.Logger;
            var oldLevel = logger.Level;
            logger.Level = GetLevel(logLevel);
            return GetLevel(oldLevel);
        }

        private static DotLogLevel GetLevel(Level logLevel)
        {
            if (logLevel == Level.All)
            {
                return DotLogLevel.All;
            }

            if (logLevel == Level.Trace)
            {
                return DotLogLevel.Trace;
            }

            if (logLevel == Level.Debug)
            {
                return DotLogLevel.Debug;
            }

            if (logLevel == Level.Info)
            {
                return DotLogLevel.Info;
            }

            if (logLevel == Level.Warn)
            {
                return DotLogLevel.Warn;
            }

            if (logLevel == Level.Error)
            {
                return DotLogLevel.Error;
            }

            if (logLevel == Level.Fatal)
            {
                return DotLogLevel.Fatal;
            }

            return DotLogLevel.Off;
        }

        private static Level GetLevel(DotLogLevel logLevel)
        {
            switch (logLevel)
            {
                case DotLogLevel.All:
                    return Level.All;
                case DotLogLevel.Trace:
                    return Level.Trace;
                case DotLogLevel.Debug:
                    return Level.Debug;
                case DotLogLevel.Info:
                    return Level.Info;
                case DotLogLevel.Warn:
                    return Level.Warn;
                case DotLogLevel.Error:
                    return Level.Error;
                case DotLogLevel.Fatal:
                    return Level.Fatal;
                case DotLogLevel.Off:
                default:
                    return Level.Off;
            }
        }
    }
}