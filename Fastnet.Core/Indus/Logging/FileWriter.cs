using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Core.Indus
{
    internal class FileWriter
    {
        private static object _lock = new object();
        private static FileWriter fw;
        private BlockingCollection<string> messageQueue;
        private StreamWriter writer;
        private string logFilename;
        private DateTime today;
        internal static FileWriter Get()
        {
            if (fw == null)
            {
                lock (_lock)
                {
                    if (fw == null)
                    {
                        fw = new FileWriter();
                        fw.Initialise();
                    }
                }
            }
            return fw;
        }
        public void WriteMessage(string message)
        {
            messageQueue.Add(message);
        }
        private void Initialise()
        {
            messageQueue = new BlockingCollection<string>();
            StartQueueService();
        }
        private void StartQueueService()
        {
            Task.Run(async () =>
            {
                while (!messageQueue.IsCompleted)
                {
                    try
                    {
                        await EnsureValidLogFile();
                        foreach (string t in messageQueue.GetConsumingEnumerable())
                        {
                            //Debug.WriteLine("messageQueue.GetConsumingEnumerable");
                            await writer.WriteLineAsync(t);
                            await writer.FlushAsync();
                        }
                    }
                    catch (Exception xe)
                    {
                        Debug.WriteLine($"RollingFileLogger failed: {xe.Message}");
                        throw;
                    }
                }
            });
        }
        private async Task EnsureValidLogFile()
        {
            LoggingOptions options = OptionsProvider.Get<LoggingOptions>();
            var logFolder = Path.Combine(Environment.GetEnvironmentVariable("ProgramData"), "Fastnet", options.LogFolder, "logs");
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
                            var stream = File.Open(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                            writer = new StreamWriter(stream);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }
    }

}
