using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System;

namespace Fastnet.Core.Logging
{
    public class RollingFileLoggerSettings : IRollingFileLoggerSettings
    {
        public string AppName { get; set; }
        public IChangeToken ChangeToken { get; set; }
        public bool IncludeScopes { get; set; }
        public IDictionary<string, LogLevel> Switches { get; set; } = new Dictionary<string, LogLevel>();
        public string LogFolder { get; set; }
        public string AppFolder { get; set; }
        public IRollingFileLoggerSettings Reload()
        {
            return this;
        }
        public bool TryGetSwitch(string name, out LogLevel level)
        {
            return Switches.TryGetValue(name, out level);
        }
    }
}
