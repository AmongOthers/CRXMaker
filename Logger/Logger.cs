using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using log4net;
using log4net.Config;

namespace Logger
{
    public class Logger
    {
        public const string ALL_LEVEL = "ALL";
        public const string INFO_LEVEL = "INFO";

        static Logger()
        {
            XmlConfigurator.Configure();
        }

        public static ILog GetLogger(object o)
        {
            return log4net.LogManager.GetLogger(o.GetType());
        }

        public static ILog GetLogger(Type type)
        {
            return log4net.LogManager.GetLogger(type);
        }

        public static ILog GetLogger(string name)
        {
            return log4net.LogManager.GetLogger(name);
        }

        /// <summary>
        /// ALL、WARN、INFO、DEBUG、ERROR、FATAL
        /// </summary>
        /// <param name="logLevel"></param>
        public static void SetLogingLevel(string logLevel)
        {
            string strChecker = "ALL_WARN_INFO_DEBUG_ERROR_FATAL";
            if (String.IsNullOrEmpty(logLevel) || !strChecker.Contains(logLevel))
                throw new Exception("错误的日志级别" + logLevel);

            log4net.Repository.ILoggerRepository[] repositories = log4net.LogManager.GetAllRepositories();
            //Configure all loggers to be at the debug level. 
            foreach (log4net.Repository.ILoggerRepository repository in repositories)
            {
                repository.Threshold = repository.LevelMap[logLevel];
                log4net.Repository.Hierarchy.Hierarchy hier = (log4net.Repository.Hierarchy.Hierarchy)repository;
                log4net.Core.ILogger[] loggers = hier.GetCurrentLoggers();
                foreach (log4net.Core.ILogger logger in loggers)
                {
                    ((log4net.Repository.Hierarchy.Logger)logger).Level = hier.LevelMap[logLevel];
                }
            }
            //Configure the root logger. 
            log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            log4net.Repository.Hierarchy.Logger rootLogger = h.Root;
            rootLogger.Level = h.LevelMap[logLevel];
        }
    }
}
