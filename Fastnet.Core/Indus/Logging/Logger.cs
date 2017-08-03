using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Core.Indus
{
    public interface ILogger
    {
        void Write(string text);
        void Write(LogLevel level, string text);
        void Write(LogLevel level, string text, Exception exceptiont);
        void Write(Exception exception);
    }
    public interface ILogger<T> : ILogger
    {
    }
    //public class Logger<T> : Logger 
    public class Logger<T> : Logger, ILogger<T>
    {
        public Logger() : base(typeof(T).Name)
        {

        }
    }
    public class Logger : ILogger
    {
        private FileWriter fw;
        private string loggerName;
        public Logger(string name)
        {
            this.loggerName = name;// typeof(T).Name;// loggerName;
            fw = FileWriter.Get();
        }  
        public void Write(Exception exception)
        {
            var xe = exception;
            while (xe != null)
            {
                var text = $"Exception: {xe.Message}: {xe.StackTrace}";
                Write(LogLevel.Error, text);
                xe = xe.InnerException;
            }
        }
        public void Write(LogLevel level, string text, Exception exception)
        {
            Write(level, text);
            IndentedWrite(exception);
        }
        public virtual void Write(LogLevel level, string text)
        {
            string message = Format(level, text);
            switch(level)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                case LogLevel.Warning:
                case LogLevel.Information:
                    fw.WriteMessage(message);
                    break;
                case LogLevel.Debug:
                case LogLevel.Trace:
                    break;
            }
            if (Debugger.IsAttached)
            {
                Debug.WriteLine(message);
            }
        }
        public void Write(string text)
        {
            Write(LogLevel.Information, text);
        }
        private void IndentedWrite(Exception exception)
        {
            var xe = exception;
            while (xe != null)
            {
                var text = $"    Exception: {xe.Message}: {xe.StackTrace}";
                Write(LogLevel.Error, text);
                xe = xe.InnerException;
            }
        }
        private string Format(LogLevel logLevel, string message)
        {
            var level = logLevel.ToString().Substring(0, 4).ToUpper();
            var now = DateTime.Now;
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var text = $"{now:ddMMMyyyy HH:mm:ss} [{threadId:000}] [{loggerName}] {level} {message}";
            return text;
        }
    }
}
