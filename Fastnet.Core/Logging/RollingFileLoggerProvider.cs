using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Fastnet.Core.Logging
{
    public class RollingFileLoggerProvider : ILoggerProvider
    {
        protected readonly ConcurrentDictionary<string, RollingFileLogger> _loggers = new ConcurrentDictionary<string, RollingFileLogger>();
        protected readonly Func<string, LogLevel, bool> _filter;
        protected IRollingFileLoggerSettings _settings;
        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, CreateLoggerImplementation);
        }
        public static ILogger CreateLogger<T>()
        {
            var settings = new RollingFileLoggerSettings();
            settings.LogFolder = "logs";
            settings.Switches = new Dictionary<string, LogLevel>();
            settings.Switches.Add("Default", LogLevel.Information);
            var lp = new RollingFileLoggerProvider(settings);
            return lp.CreateLogger(typeof(T).Name);
        }
        public RollingFileLoggerProvider(Func<string, LogLevel, bool> filter, bool includeScopes)
        {
            //_env = env;
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            _settings = new RollingFileLoggerSettings()
            {
                LogFolder = "logs",
                AppFolder = null,
                IncludeScopes = includeScopes
            };
        }
        public RollingFileLoggerProvider(IRollingFileLoggerSettings settings)
        {
            //_env = env;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (_settings.ChangeToken != null)
            {
                _settings.ChangeToken.RegisterChangeCallback(OnConfigurationReload, null);
            }
        }
        public void Dispose()
        {
            
        }
        protected virtual RollingFileLogger CreateLoggerImplementation(string name)
        {
            string appName = _settings.AppName ?? GetExecutableName();
            string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fastnet", appName);
            return CreateLoggerImplementation(name, basePath);
            //string basePath = string.IsNullOrWhiteSpace(_settings.AppFolder) ? GetExecutableName()
            //    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fastnet", _settings.AppFolder);
            //if (!Directory.Exists(basePath))
            //{
            //    Directory.CreateDirectory(basePath);
            //}
            //string logFolder = Path.Combine(basePath, _settings.LogFolder);
            //return new RollingFileLogger(logFolder, name, GetFilter(name, _settings), _settings.IncludeScopes);
        }
        protected RollingFileLogger CreateLoggerImplementation(string name, string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            string logFolder = Path.Combine(basePath, _settings.LogFolder);
            return new RollingFileLogger(logFolder, name, GetFilter(name, _settings), _settings.IncludeScopes);
        }
        protected virtual IEnumerable<string> GetKeyPrefixes(string name)
        {
            while (!string.IsNullOrEmpty(name))
            {
                yield return name;
                var lastIndexOfDot = name.LastIndexOf('.');
                if (lastIndexOfDot == -1)
                {
                    yield return "Default";
                    break;
                }
                name = name.Substring(0, lastIndexOfDot);
            }
        }
        protected virtual Func<string, LogLevel, bool> GetFilter(string name, IRollingFileLoggerSettings settings)
        {
            if (_filter != null)
            {
                return _filter;
            }

            if (settings != null)
            {
                foreach (var prefix in GetKeyPrefixes(name))
                {
                    LogLevel level;
                    if (settings.TryGetSwitch(prefix, out level))
                    {
                        return (n, l) => l >= level;
                    }
                }
            }

            return (n, l) => false;
        }
        protected virtual void OnConfigurationReload(object state)
        {
            try
            {
                // The settings object needs to change here, because the old one is probably holding on
                // to an old change token.
                _settings = _settings.Reload();

                foreach (var logger in _loggers.Values)
                {
                    logger.Filter = GetFilter(logger.Name, _settings);
                    logger.IncludeScopes = _settings.IncludeScopes;

                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error while loading configuration changes.{Environment.NewLine}{ex}");
            }
            finally
            {
                // The token will change each time it reloads, so we need to register again.
                if (_settings?.ChangeToken != null)
                {
                    _settings.ChangeToken.RegisterChangeCallback(OnConfigurationReload, null);
                }
            }
        }
        private string GetExecutableName()
        {
            return Assembly.GetEntryAssembly().GetName().Name;
            //return "something";
        }
    }
}
