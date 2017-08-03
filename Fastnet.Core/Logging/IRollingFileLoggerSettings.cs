using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Fastnet.Core.Logging
{
    public interface IRollingFileLoggerSettings
    {
        string AppName { get; }
        bool IncludeScopes { get; }
        string AppFolder { get; }
        string LogFolder { get; }
        IChangeToken ChangeToken { get; }
        bool TryGetSwitch(string name, out LogLevel level);
        IRollingFileLoggerSettings Reload();
    }
}
