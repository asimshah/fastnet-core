namespace Fastnet.Core
{
    public class LaunchMethods
    {
        public LaunchMode ParseArgs(string[] args)
        {
            LaunchMode lm = LaunchMode.Service;
            if (args != null && args.Length == 1 && args[0].Length > 1
                && (args[0][0] == '-' || args[0][0] == '/'))
            {
                switch (args[0].Substring(1).ToLower())
                {
                    default:
                        break;
                    case "install":
                    case "i":
                        lm = LaunchMode.Install;
                        //SelfInstaller.InstallMe();
                        break;
                    case "uninstall":
                    case "u":
                        lm = LaunchMode.Uninstall;
                        //SelfInstaller.UninstallMe();
                        break;
                    case "console":
                    case "c":
                        lm = LaunchMode.Console;
                        //MyConsoleHost.Launch();
                        break;
                    case "resetEventSources":
                    case "r":
                        ClearAllEventSources();
                        lm = LaunchMode.Noop;
                        //MyConsoleHost.Launch();
                        break;
                    case "addEventSources":
                    case "a":
                        AddAllEventSources();
                        lm = LaunchMode.Noop;
                        //MyConsoleHost.Launch();
                        break;
                }
            }
            return lm;
        }
        public virtual void ClearAllEventSources()
        {
            //if (EventLog.SourceExists(ProjectInstaller.EventSource))
            //{
            //    EventLog.DeleteEventSource(ProjectInstaller.EventSource);
            //}
            //if (EventLog.SourceExists(TimerService.EventSource))
            //{
            //    EventLog.DeleteEventSource(TimerService.EventSource);
            //}
            //if (EventLog.SourceExists(MonitorService.EventSource))
            //{
            //    EventLog.DeleteEventSource(MonitorService.EventSource);
            //}
        }
        public virtual void AddAllEventSources()
        {
            //if (!EventLog.SourceExists(ProjectInstaller.EventSource))
            //{
            //    EventLog.CreateEventSource(ProjectInstaller.EventSource, Globals.FastnetServicesEventLog);
            //}
            //if (!EventLog.SourceExists(TimerService.EventSource))
            //{
            //    EventLog.CreateEventSource(TimerService.EventSource, Globals.FastnetServicesEventLog);
            //}
            //if (!EventLog.SourceExists(MonitorService.EventSource))
            //{
            //    EventLog.CreateEventSource(MonitorService.EventSource, Globals.FastnetServicesEventLog);
            //}
        }
    }
}
