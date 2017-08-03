using Fastnet.Core.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Fastnet.Core.Web.Logging
{
    public static class RollingFileLoggerExtensions
    {
        public static void Log(this ILogger logger, LogLevel level, string text)
        {
            switch (level)
            {
                default:
                case LogLevel.None:
                    break;
                case LogLevel.Critical:
                    logger.LogCritical(text);
                    break;
                case LogLevel.Debug:
                    logger.LogDebug(text);
                    break;
                case LogLevel.Error:
                    logger.LogError(text);
                    break;
                case LogLevel.Information:
                    logger.LogInformation(text);
                    break;
                case LogLevel.Trace:
                    logger.LogTrace(text);
                    break;
                case LogLevel.Warning:
                    logger.LogWarning(text);
                    break;
            }
        }
        /// <summary>
        /// Adds a rolling file logger that is enabled for <see cref="LogLevel"/>.Information or higher.
        /// </summary>
        /// <param name="env">IHostingEnvironment (used to locate the default folder for log files)</param>
        public static ILoggerFactory AddRollingFile(this ILoggerFactory factory, IHostingEnvironment env)
        {
            return factory.AddRollingFile(env, includeScopes: false);
        }

        /// <summary>
        /// Adds a rolling file logger that is enabled for <see cref="LogLevel"/>.Information or higher.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="env">IHostingEnvironment (used to locate the default folder for log files)</param>
        /// <param name="includeScopes">A value which indicates whether log scope information should be displayed
        /// in the output.</param>
        public static ILoggerFactory AddRollingFile(this ILoggerFactory factory, IHostingEnvironment env, bool includeScopes)
        {
            factory.AddRollingFile(env, (n, l) => l >= LogLevel.Information, includeScopes);
            return factory;
        }
        /// <summary>
        /// Adds a rolling file logger that is enabled for <see cref="LogLevel"/>s of minLevel or higher.
        /// </summary>
        /// <param name="factory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <param name="env">IHostingEnvironment (used to locate the default folder for log files)</param>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> to be logged</param>
        public static ILoggerFactory AddRollingFile(this ILoggerFactory factory, IHostingEnvironment env, LogLevel minLevel)
        {
            factory.AddRollingFile(env, minLevel, includeScopes: false);
            return factory;
        }
        /// <summary>
        /// Adds a rolling file logger that is enabled for <see cref="LogLevel"/>s of minLevel or higher.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="env">IHostingEnvironment (used to locate the default folder for log files)</param>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> to be logged</param>
        /// <param name="includeScopes">A value which indicates whether log scope information should be displayed
        /// in the output.</param>
        public static ILoggerFactory AddRollingFile(this ILoggerFactory factory, IHostingEnvironment env, LogLevel minLevel, bool includeScopes)
        {
            factory.AddRollingFile(env, (category, logLevel) => logLevel >= minLevel, includeScopes);
            return factory;
        }
        /// <summary>
        /// Adds a rolling file logger that is enabled as defined by the filter function.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="env">IHostingEnvironment (used to locate the default folder for log files)</param>
        /// <param name="filter"></param>
        public static ILoggerFactory AddRollingFile(this ILoggerFactory factory, IHostingEnvironment env, Func<string, LogLevel, bool> filter)
        {
            factory.AddRollingFile(env, filter, includeScopes: false);
            return factory;
        }
        /// <summary>
        /// Adds a rolling file logger that is enabled as defined by the filter function.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="env">IHostingEnvironment (used to locate the default folder for log files)</param>
        /// <param name="filter"></param>
        /// <param name="includeScopes">A value which indicates whether log scope information should be displayed
        /// in the output. (not implemented!)</param>
        public static ILoggerFactory AddRollingFile(this ILoggerFactory factory, IHostingEnvironment env, Func<string, LogLevel, bool> filter, bool includeScopes)
        {
            factory.AddProvider(new RollingFileLoggerWebProvider(env, filter, includeScopes));
            return factory;
        }
        public static ILoggerFactory AddRollingFile(this ILoggerFactory factory, IHostingEnvironment env, IRollingFileLoggerSettings settings)
        {
            factory.AddProvider(new RollingFileLoggerWebProvider(env, settings));
            return factory;
        }
        public static ILoggerFactory AddRollingFile(this ILoggerFactory factory, IHostingEnvironment env,  IConfiguration configuration)
        {
            var settings = new ConfigurationRollingFileLoggerSettings(configuration);

            return factory.AddRollingFile(env, settings);
        }
    }
}
