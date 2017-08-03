using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;

namespace Fastnet.Core.Logging
{
    public class ConfigurationRollingFileLoggerSettings : IRollingFileLoggerSettings
    {
        private readonly IConfiguration _configuration;

        public ConfigurationRollingFileLoggerSettings(IConfiguration configuration)
        {
            _configuration = configuration;
            ChangeToken = configuration.GetReloadToken();
        }

        public IChangeToken ChangeToken { get; private set; }
        public string AppName
        {
            get
            {
                var value = _configuration["AppName"];
                if (string.IsNullOrWhiteSpace(value))
                {
                    return null;
                }
                else //if (bool.TryParse(value, out includeScopes))
                {
                    return value;
                }
            }
        }
        public string AppFolder
        {
            get
            {
                var value = _configuration["AppFolder"];
                if (string.IsNullOrWhiteSpace(value))
                {
                    return null;
                }
                else //if (bool.TryParse(value, out includeScopes))
                {
                    return value;
                }
            }
        }
        public string LogFolder
        {
            get
            {       
                var value = _configuration["LogFolder"];
                if (string.IsNullOrWhiteSpace(value))
                {
                    return "logs";
                }
                else //if (bool.TryParse(value, out includeScopes))
                {
                    return value;
                }
            }
        }
        public bool IncludeScopes
        {
            get
            {
                bool includeScopes;
                var value = _configuration["IncludeScopes"];
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }
                else if (bool.TryParse(value, out includeScopes))
                {
                    return includeScopes;
                }
                else
                {
                    var message = $"Configuration value '{value}' for setting '{nameof(IncludeScopes)}' is not supported.";
                    throw new InvalidOperationException(message);
                }
            }
        }

        public IRollingFileLoggerSettings Reload()
        {
            ChangeToken = null;
            return new ConfigurationRollingFileLoggerSettings(_configuration);
        }

        public bool TryGetSwitch(string name, out LogLevel level)
        {
            var switches = _configuration.GetSection("LogLevel");
            if (switches == null)
            {
                level = LogLevel.None;
                return false;
            }

            var value = switches[name];
            if (string.IsNullOrEmpty(value))
            {
                level = LogLevel.None;
                return false;
            }
            else if (Enum.TryParse<LogLevel>(value, out level))
            {
                return true;
            }
            else
            {
                var message = $"Configuration value '{value}' for category '{name}' is not supported.";
                throw new InvalidOperationException(message);
            }
        }
    }
}
