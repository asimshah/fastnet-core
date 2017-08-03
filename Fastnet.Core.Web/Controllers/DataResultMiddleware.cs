using Fastnet.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.Core.Web.Controllers
{
    [Obsolete]
    public class DataResultMiddleware
    {
        //ExceptionDetailsProvider x;
        private readonly ILogger log;
        private readonly RequestDelegate next;
        //private readonly IFileProvider fileProvider;
        //private readonly ExceptionDetailsProvider exceptionDetailsProvider;
        public DataResultMiddleware(RequestDelegate n, ILoggerFactory lf/*, IHostingEnvironment hostingEnvironment*/)
        {
            next = n;
            log = lf.CreateLogger<DataResultMiddleware>();
            //fileProvider = hostingEnvironment.ContentRootFileProvider;
            //exceptionDetailsProvider = new ExceptionDetailsProvider(fileProvider, 1);
        }
        public async Task Invoke(HttpContext context)
        {
            DataResult dr = null;
            try
            {
                await next(context);
            }
            catch (TaskCanceledException tce)
            {
                Debug.Write(tce.Message);
            }
            //catch (AggregateException ae)
            //{
            //    Debugger.Break();
            //}
            catch (Exception xe1)
            {
                // 27Jun2017
                // I tried to get source code line and line number
                // information into the log using 
                // stuff copied from the UseDeveloperExceptionPage()
                // but it just wouldn't work
                // I can't be bothered so I have commented it all out

                try
                {
                    //if (xe1 is ICompilationException)
                    //{
                    //    log.LogError(xe1);
                    //}
                    //else
                    //{
                    //    LogRunTimeException(xe1);
                    //}
                    log.LogError(xe1);
                    dr = new DataResult { success = false, data = null, message = null, exceptionMessage = xe1.Message };

                }
                catch (Exception xe2)
                {
                    log.LogError($"Exception {xe2.Message} within excpetion handler");
                    throw;
                }
                //throw;
            }
            if(!context.Response.HasStarted && dr != null)
            {
                context.Response.ContentType = "application/json";
                var json = JsonConvert.SerializeObject(dr, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                await context.Response.WriteAsync(json);
            }
        }

        private void LogRunTimeException(Exception xe1)
        {
            //try
            //{
            //    var details = exceptionDetailsProvider.GetDetails(xe1);

            //}
            //catch (Exception xe)
            //{
            //    Debugger.Break();
            //    throw;
            //}
        }
    }
    /// <summary>
    /// Contains details for individual exception messages.
    /// </summary>
    internal class ExceptionDetails
    {
        /// <summary>
        /// An individual exception
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// The generated stack frames
        /// </summary>
        public IEnumerable<StackFrameSourceCodeInfo> StackFrames { get; set; }

        /// <summary>
        /// Gets or sets the summary message.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
    internal class ExceptionDetailsProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly int _sourceCodeLineCount;

        public ExceptionDetailsProvider(IFileProvider fileProvider, int sourceCodeLineCount)
        {
            _fileProvider = fileProvider;
            _sourceCodeLineCount = sourceCodeLineCount;
        }

        public IEnumerable<ExceptionDetails> GetDetails(Exception exception)
        {
            var exceptions = FlattenAndReverseExceptionTree(exception);

            foreach (var ex in exceptions)
            {
                yield return new ExceptionDetails
                {
                    Error = ex,
                    StackFrames = StackTraceHelper.GetFrames(ex)
                            .Select(frame => GetStackFrameSourceCodeInfo(
                                frame.MethodDisplayInfo.ToString(),
                                frame.FilePath,
                                frame.LineNumber))
                };
            }
        }

        private static IEnumerable<Exception> FlattenAndReverseExceptionTree(Exception ex)
        {
            // ReflectionTypeLoadException is special because the details are in
            // the LoaderExceptions property
            var typeLoadException = ex as ReflectionTypeLoadException;
            if (typeLoadException != null)
            {
                var typeLoadExceptions = new List<Exception>();
                foreach (var loadException in typeLoadException.LoaderExceptions)
                {
                    typeLoadExceptions.AddRange(FlattenAndReverseExceptionTree(loadException));
                }

                typeLoadExceptions.Add(ex);
                return typeLoadExceptions;
            }

            var list = new List<Exception>();
            while (ex != null)
            {
                list.Add(ex);
                ex = ex.InnerException;
            }
            list.Reverse();
            return list;
        }

        // make it internal to enable unit testing
        internal StackFrameSourceCodeInfo GetStackFrameSourceCodeInfo(string method, string filePath, int lineNumber)
        {
            var stackFrame = new StackFrameSourceCodeInfo
            {
                Function = method,
                File = filePath,
                Line = lineNumber
            };

            if (string.IsNullOrEmpty(stackFrame.File))
            {
                return stackFrame;
            }

            IEnumerable<string> lines = null;
            if (File.Exists(stackFrame.File))
            {
                lines = File.ReadLines(stackFrame.File);
            }
            else
            {
                // Handle relative paths and embedded files
                var fileInfo = _fileProvider.GetFileInfo(stackFrame.File);
                if (fileInfo.Exists)
                {
                    // ReadLines doesn't accept a stream. Use ReadLines as its more efficient
                    // relative to reading lines via stream reader
                    if (!string.IsNullOrEmpty(fileInfo.PhysicalPath))
                    {
                        lines = File.ReadLines(fileInfo.PhysicalPath);
                    }
                    else
                    {
                        lines = ReadLines(fileInfo);
                    }
                }
            }

            if (lines != null)
            {
                ReadFrameContent(stackFrame, lines, stackFrame.Line, stackFrame.Line);
            }

            return stackFrame;
        }

        // make it internal to enable unit testing
        internal void ReadFrameContent(
            StackFrameSourceCodeInfo frame,
            IEnumerable<string> allLines,
            int errorStartLineNumberInFile,
            int errorEndLineNumberInFile)
        {
            // Get the line boundaries in the file to be read and read all these lines at once into an array.
            var preErrorLineNumberInFile = Math.Max(errorStartLineNumberInFile - _sourceCodeLineCount, 1);
            var postErrorLineNumberInFile = errorEndLineNumberInFile + _sourceCodeLineCount;
            var codeBlock = allLines
                .Skip(preErrorLineNumberInFile - 1)
                .Take(postErrorLineNumberInFile - preErrorLineNumberInFile + 1)
                .ToArray();

            var numOfErrorLines = (errorEndLineNumberInFile - errorStartLineNumberInFile) + 1;
            var errorStartLineNumberInArray = errorStartLineNumberInFile - preErrorLineNumberInFile;

            frame.PreContextLine = preErrorLineNumberInFile;
            frame.PreContextCode = codeBlock.Take(errorStartLineNumberInArray).ToArray();
            frame.ContextCode = codeBlock
                .Skip(errorStartLineNumberInArray)
                .Take(numOfErrorLines)
                .ToArray();
            frame.PostContextCode = codeBlock
                .Skip(errorStartLineNumberInArray + numOfErrorLines)
                .ToArray();
        }

        private static IEnumerable<string> ReadLines(IFileInfo fileInfo)
        {
            using (var reader = new StreamReader(fileInfo.CreateReadStream()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
    /// <summary>
    /// Contains the source code where the exception occurred.
    /// </summary>
    internal class StackFrameSourceCodeInfo
    {
        /// <summary>
        /// Function containing instruction
        /// </summary>
        public string Function { get; set; }

        /// <summary>
        /// File containing the instruction
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// The line number of the instruction
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// The line preceeding the frame line
        /// </summary>
        public int PreContextLine { get; set; }

        /// <summary>
        /// Lines of code before the actual error line(s).
        /// </summary>
        public IEnumerable<string> PreContextCode { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// Line(s) of code responsible for the error.
        /// </summary>
        public IEnumerable<string> ContextCode { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// Lines of code after the actual error line(s).
        /// </summary>
        public IEnumerable<string> PostContextCode { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// Specific error details for this stack frame.
        /// </summary>
        public string ErrorDetails { get; set; }
    }
    internal class StackTraceHelper
    {
        public static IList<StackFrameInfo> GetFrames(Exception exception)
        {
            var frames = new List<StackFrameInfo>();

            if (exception == null)
            {
                return frames;
            }

#if NET451
            using (var portablePdbReader = new PortablePdbReader())
#endif
            {
                var needFileInfo = true;
                var stackTrace = new System.Diagnostics.StackTrace(exception, needFileInfo);
                var stackFrames = stackTrace.GetFrames();

                if (stackFrames == null)
                {
                    return frames;
                }

                foreach (var frame in stackFrames)
                {
                    var method = frame.GetMethod();

                    var stackFrame = new StackFrameInfo
                    {
                        StackFrame = frame,
                        FilePath = frame.GetFileName(),
                        LineNumber = frame.GetFileLineNumber(),
                        MethodDisplayInfo = GetMethodDisplayString(frame.GetMethod()),
                    };

#if NET451
                    if (string.IsNullOrEmpty(stackFrame.FilePath))
                    {
                        // .NET Framework and older versions of mono don't support portable PDBs
                        // so we read it manually to get file name and line information
                        portablePdbReader.PopulateStackFrame(stackFrame, method, frame.GetILOffset());
                    }
#endif

                    frames.Add(stackFrame);

                }

                return frames;
            }
        }

        internal static MethodDisplayInfo GetMethodDisplayString(MethodBase method)
        {
            // Special case: no method available
            if (method == null)
            {
                return null;
            }

            var methodDisplayInfo = new MethodDisplayInfo();

            // Type name
            var type = method.DeclaringType;
            if (type != null)
            {
                methodDisplayInfo.DeclaringTypeName = TypeNameHelper.GetTypeDisplayName(type);
            }

            // Method name
            methodDisplayInfo.Name = method.Name;
            if (method.IsGenericMethod)
            {
                var genericArguments = string.Join(", ", method.GetGenericArguments()
                    .Select(arg => TypeNameHelper.GetTypeDisplayName(arg/*, fullName: false*/)));
                methodDisplayInfo.GenericArguments += "<" + genericArguments + ">";
            }

            // Method parameters
            methodDisplayInfo.Parameters = method.GetParameters().Select(parameter =>
            {
                var parameterType = parameter.ParameterType;

                var prefix = string.Empty;
                if (parameter.IsOut)
                {
                    prefix = "out";
                }
                else if (parameterType != null && parameterType.IsByRef)
                {
                    prefix = "ref";
                }

                var parameterTypeString = "?";
                if (parameterType != null)
                {
                    if (parameterType.IsByRef)
                    {
                        parameterType = parameterType.GetElementType();
                    }

                    parameterTypeString = TypeNameHelper.GetTypeDisplayName(parameterType/*, fullName: false*/);
                }

                return new ParameterDisplayInfo
                {
                    Prefix = prefix,
                    Name = parameter.Name,
                    Type = parameterTypeString,
                };
            });

            return methodDisplayInfo;
        }

    }
    internal class StackFrameInfo
    {
        public int LineNumber { get; set; }

        public string FilePath { get; set; }

        public StackFrame StackFrame { get; set; }

        public MethodDisplayInfo MethodDisplayInfo { get; set; }
    }
    internal class MethodDisplayInfo
    {
        public string DeclaringTypeName { get; set; }

        public string Name { get; set; }

        public string GenericArguments { get; set; }

        public IEnumerable<ParameterDisplayInfo> Parameters { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(DeclaringTypeName))
            {
                builder
                    .Append(DeclaringTypeName)
                    .Append(".");
            }

            builder.Append(Name);
            builder.Append(GenericArguments);

            builder.Append("(");
            builder.Append(string.Join(", ", Parameters.Select(p => p.ToString())));
            builder.Append(")");

            return builder.ToString();
        }
    }
    internal class ParameterDisplayInfo
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string Prefix { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(Prefix))
            {
                builder
                    .Append(Prefix)
                    .Append(" ");
            }

            builder.Append(Type);
            builder.Append(" ");
            builder.Append(Name);

            return builder.ToString();
        }
    }
}
