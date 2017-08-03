using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Core.Web.Controllers
{
    //public class LogCalls2 : ActionFilterAttribute
    //{
    //    private ILogger log;
    //    private string[] exclusionList;
    //    public LogCalls2(ILogger<LogCallsAttribute> log, params string[] exclusionList)
    //    {
    //        this.exclusionList = exclusionList;
    //        this.log = log;
    //    }
    //    public async override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    //    {
    //        var nameMethod = context.ActionDescriptor.GetType().GetMethod("get_ActionName");
    //        string name = (string)nameMethod.Invoke(context.ActionDescriptor, null);
    //        if (exclusionList == null || exclusionList.Length == 0 || !exclusionList.Contains(name, StringComparer.CurrentCultureIgnoreCase))
    //        {
    //            var t = context.ActionArguments.Select(args => $"{args.Key}={args.Value.ToString()}");
    //            var text = $"{context.Controller.GetType().Name} called with {context.HttpContext.Request.Path.Value} --> {name}({(string.Join(", ", t.ToArray()))})";
    //            Debug.WriteLine(text);
    //        }
    //        await next();
    //    }
    //    public override void OnResultExecuted(ResultExecutedContext context)
    //    {
    //        base.OnResultExecuted(context);
    //    }
    //}
    public class LogCallsAttribute : TypeFilterAttribute
    {
        
        public LogCallsAttribute(params string[] exclusionList): base (typeof(LogCallsImplementation))
        {
            //this.exclusionList = exclusionList;
            this.Arguments = new object[] { exclusionList };
        }
        private class LogCallsImplementation : IAsyncActionFilter, IAsyncResultFilter
        {
            private ILogger log;
            private string[] exclusionList;
            public LogCallsImplementation(ILogger<LogCallsAttribute> log, object[] arguments)
            {
                this.log = log;
                if(arguments != null && arguments.Length > 0)
                {
                    exclusionList = (string[])arguments;
                }
            }
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                (bool canLog, string actionName) r = CanLog(context.ActionDescriptor);
                //var nameMethod = context.ActionDescriptor.GetType().GetMethod("get_ActionName");
                //string name = (string)nameMethod.Invoke(context.ActionDescriptor, null);
                if(r.canLog)
                //if (exclusionList == null || exclusionList.Length == 0 || !exclusionList.Contains(name, StringComparer.CurrentCultureIgnoreCase))
                {
                    var remoteName = context.HttpContext.GetRemoteName();
                    var t = context.ActionArguments.Select(args => $"{args.Key}={args.Value.ToString()}");
                    var text = $"{context.Controller.GetType().Name} called (from {remoteName}) with {context.HttpContext.Request.Path.Value} --> {r.actionName}({(string.Join(", ", t.ToArray()))})";
                    log.LogInformation(text);
                }
                await next();
            }
            public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
            {
                (bool canLog, string actionName) r = CanLog(context.ActionDescriptor);
                if(r.canLog)
                {
                    var dr = (context.Result as ObjectResult)?.Value as DataResult;
                    string returned = dr?.ToString();
                    //var text = $"{context.Controller.GetType().Name} called with {context.HttpContext.Request.Path.Value} <-- {r.actionName}() returned {(returned ?? context.Result.ToString())}";
                    var text = $"{context.Controller.GetType().Name} returned {(returned ?? context.Result.ToString())} from {r.actionName}() <-- {context.HttpContext.Request.Path.Value}";
                    log.LogInformation(text);
                }
                await next();
            }
            //private string GetRemoteName(HttpContext ctx)
            //{
            //    string name = null;
            //    IPHostEntry entry = null;
            //    var remoteIp = ctx.Connection.RemoteIpAddress;
            //    try
            //    {
            //        entry = Dns.GetHostEntry(remoteIp);
            //    }
            //    catch { }
            //    if (entry == null || string.IsNullOrWhiteSpace(entry.HostName))
            //    {
            //        name = remoteIp.ToString();
            //    }
            //    else
            //    {
            //        name = entry.HostName.ToLower();
            //    }
            //    return name;
            //}
            private (bool, string) CanLog(ActionDescriptor actionDescriptor)
            {
                var nameMethod = actionDescriptor.GetType().GetMethod("get_ActionName");
                string name = (string)nameMethod.Invoke(actionDescriptor, null);
                bool canLog = exclusionList == null || exclusionList.Length == 0 || !exclusionList.Contains(name, StringComparer.CurrentCultureIgnoreCase);
                return (canLog, name);
            }
        }
    }

}
