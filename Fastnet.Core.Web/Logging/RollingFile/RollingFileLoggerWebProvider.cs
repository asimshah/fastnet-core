using Fastnet.Core.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Fastnet.Core.Web.Logging
{
    public class RollingFileLoggerWebProvider : RollingFileLoggerProvider // ILoggerProvider
    {
        //public static IWebHost host;
        //private readonly ConcurrentDictionary<string, RollingFileLogger> _loggers = new ConcurrentDictionary<string, RollingFileLogger>();
        //private readonly Func<string, LogLevel, bool> _filter;
        //private IRollingFileLoggerSettings _settings;
        private IHostingEnvironment _env;
        public RollingFileLoggerWebProvider(IHostingEnvironment env, Func<string, LogLevel, bool> filter, bool includeScopes) : base(filter, includeScopes)
        {
            _env = env;
            //_filter = filter ?? throw new ArgumentNullException(nameof(filter));
            //_settings = new RollingFileLoggerSettings()
            //{
            //    LogFolder = "logs",
            //    AppFolder = null,
            //    IncludeScopes = includeScopes
            //};
        }
        public RollingFileLoggerWebProvider(IHostingEnvironment env, IRollingFileLoggerSettings settings) : base(settings)
        {
            _env = env;
            //_settings = settings ?? throw new ArgumentNullException(nameof(settings));
            //if (_settings.ChangeToken != null)
            //{
            //    _settings.ChangeToken.RegisterChangeCallback(OnConfigurationReload, null);
            //}
        }
        //public ILogger CreateLogger(string name)
        //{
        //    return _loggers.GetOrAdd(name, CreateLoggerImplementation);
        //}
        //private void OnConfigurationReload(object state)
        //{
        //    try
        //    {
        //        // The settings object needs to change here, because the old one is probably holding on
        //        // to an old change token.
        //        _settings = _settings.Reload();

        //        foreach (var logger in _loggers.Values)
        //        {
        //            logger.Filter = GetFilter(logger.Name, _settings);
        //            logger.IncludeScopes = _settings.IncludeScopes;

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Console.WriteLine($"Error while loading configuration changes.{Environment.NewLine}{ex}");
        //    }
        //    finally
        //    {
        //        // The token will change each time it reloads, so we need to register again.
        //        if (_settings?.ChangeToken != null)
        //        {
        //            _settings.ChangeToken.RegisterChangeCallback(OnConfigurationReload, null);
        //        }
        //    }
        //}
        protected override RollingFileLogger CreateLoggerImplementation(string name)
        {
            string basePath = _settings.AppFolder ?? _env.ContentRootPath;
            if (string.Compare(basePath, "default", true) == 0)
            {
                return base.CreateLoggerImplementation(name);
            }
            else
            {
                return base.CreateLoggerImplementation(name, basePath);
            }
        }

        //private RollingFileLogger CreateLoggerImplementation(string name)
        //{
        //    string basePath = string.IsNullOrWhiteSpace(_settings.AppFolder) ? _env.ContentRootPath
        //        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fastnet", _settings.AppFolder);
        //    if(!Directory.Exists(basePath))
        //    {
        //        Directory.CreateDirectory(basePath);
        //    }
        //    string logFolder = Path.Combine(basePath, _settings.LogFolder);
        //    return new RollingFileLogger(logFolder, name, GetFilter(name, _settings), _settings.IncludeScopes);
        //}

        //private Func<string, LogLevel, bool> GetFilter(string name, IRollingFileLoggerSettings settings)
        //{
        //    if (_filter != null)
        //    {
        //        return _filter;
        //    }

        //    if (settings != null)
        //    {
        //        foreach (var prefix in GetKeyPrefixes(name))
        //        {
        //            LogLevel level;
        //            if (settings.TryGetSwitch(prefix, out level))
        //            {
        //                return (n, l) => l >= level;
        //            }
        //        }
        //    }

        //    return (n, l) => false;
        //}

        //private IEnumerable<string> GetKeyPrefixes(string name)
        //{
        //    while (!string.IsNullOrEmpty(name))
        //    {
        //        yield return name;
        //        var lastIndexOfDot = name.LastIndexOf('.');
        //        if (lastIndexOfDot == -1)
        //        {
        //            yield return "Default";
        //            break;
        //        }
        //        name = name.Substring(0, lastIndexOfDot);
        //    }
        //}
        //public void Dispose()
        //{

        //}
    }
}
