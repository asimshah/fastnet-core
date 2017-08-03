using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Core.Logging
{
    internal class FileWriter
    {
        const string EventSource = "Rolling File LogWriter";
        private static object _lock = new object();
        private static FileWriter fw;
        private EventLog eventWriterEventLog;
        private BlockingCollection<string> messageQueue;
        private StreamWriter writer;
        private string logFilename;
        private DateTime today;
        private string logFolder;
        internal static FileWriter Get(string logFolder)
        {
            if (fw == null)
            {
                lock (_lock)
                {
                    if (fw == null)
                    {
                        fw = new FileWriter(logFolder);
                        fw.Initialise();
                    }
                }
            }
            return fw;
        }
        private FileWriter(string logFolder)
        {
            eventWriterEventLog = new EventLog();
            if (!EventLog.SourceExists(EventSource))
            {
                EventLog.CreateEventSource(EventSource, "Application");
            }
            eventWriterEventLog.Source = EventSource;
            eventWriterEventLog.Log = "Application";
            this.logFolder = logFolder;
        }
        public void WriteMessage(LogLevel logLevel, string name, int processId, int threadId, string message, Exception exception)
        {

            var level = logLevel.ToString().Substring(0, 4).ToUpper();
            var now = DateTime.Now;
            var text = $"{now:ddMMMyyyy HH:mm:ss} [{processId:0000}] [{threadId:000}] [{name}] {level} {message}";
            messageQueue.Add(text);
            WriteException(exception);
        }

        private void WriteException(Exception exception)
        {
            var xe = exception;
            while (xe != null)
            {

                var text = $"   Exception: {xe.Message}: {xe.StackTrace}";
                messageQueue.Add(text);
                xe = xe.InnerException;
            }
        }

        private void Initialise()
        {
            messageQueue = new BlockingCollection<string>();
            //EnsureValidLogFileAsync();
            StartQueueService();
        }
        private void StartQueueService()
        {
            try
            {
                Task.Run(async () =>
                {
                    while (!messageQueue.IsCompleted)
                    {
                        try
                        {
                            foreach (string t in messageQueue.GetConsumingEnumerable())
                            {
                                //Debug.WriteLine("messageQueue.GetConsumingEnumerable");
                                await EnsureValidLogFileAsync();

                                await writer.WriteLineAsync(t);
                                await writer.FlushAsync();
                                //EnsureValidLogFile();
                                //writer.WriteLine(t);
                                //writer.Flush();
                            }
                        }
                        catch (Exception xe)
                        {
                            Debug.WriteLine($"RollingFileLogger failed: {xe.Message}");
                            //Debugger.Break();
                            throw;
                        }
                    }
                });
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task EnsureValidLogFileAsync()
        {
            if (writer == null || today != DateTime.Today)
            {
                using (SemaphoreSlim flag = new SemaphoreSlim(1))
                {
                    await flag.WaitAsync();
                    try
                    {
                        if (writer == null || today != DateTime.Today)
                        {
                            if (!Directory.Exists(logFolder))
                            {
                                Directory.CreateDirectory(logFolder);
                            }
                            if (writer != null)
                            {
                                writer.Flush();
                                writer.Dispose();
                            }
                            today = DateTime.Today;
                            logFilename = $"{today.Year}-{today.Month:00}-{today.Day:00}.log";
                            var fullPath = Path.Combine(logFolder, logFilename);
                            //Debug.WriteLine($"log file is {fullPath}");
                            eventWriterEventLog.WriteEntry($"log file is {fullPath}");
                            var stream = File.Open(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                            writer = new StreamWriter(stream);
                        }
                    }
                    catch (Exception xe)
                    {
                        eventWriterEventLog.WriteEntry($"exception {xe.Message}", EventLogEntryType.Error);
                        throw;
                    }
                }
            }
        }
        [Obsolete]
        private void EnsureValidLogFile()
        {
            //Debug.WriteLine("EnsureValidLogFile(1)");
            if (writer == null || today != DateTime.Today)
            {
                using (SemaphoreSlim flag = new SemaphoreSlim(1))
                {
                    flag.Wait();
                    try
                    {
                        if (writer == null || today != DateTime.Today)
                        {
                            if (!Directory.Exists(logFolder))
                            {
                                Directory.CreateDirectory(logFolder);
                            }
                            if (writer != null)
                            {
                                writer.Flush();
                                writer.Dispose();
                            }
                            today = DateTime.Today;
                            logFilename = $"{today.Year}-{today.Month:00}-{today.Day:00}.log";
                            var fullPath = Path.Combine(logFolder, logFilename);
                            //Debug.WriteLine($"log file is {fullPath}");
                            eventWriterEventLog.WriteEntry($"log file is is {fullPath}");
                            var stream = File.Open(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                            writer = new StreamWriter(stream);
                        }
                    }
                    catch (Exception xe)
                    {
                        eventWriterEventLog.WriteEntry($"exception: ${xe.Message}");
                        throw;
                    }
                }
            }
        }
    }
}
