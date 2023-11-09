using System.IO;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Serilog;
using ZLogger;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Benchmark.Benchmarks;

// file class MsExtConsoleLoggerFormatter : ConsoleFormatter
// {
//     public class Options : ConsoleFormatterOptions
//     {
//     }
//
//     public MsExtConsoleLoggerFormatter() : base("Benchmark")
//     {
//     }
//
//     public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
//     {
//         var message = logEntry.Formatter.Invoke(logEntry.State, logEntry.Exception);
//         var timestamp = DateTime.UtcNow;
//         textWriter.Write(timestamp);
//         textWriter.Write(" [");
//         textWriter.Write(logEntry.LogLevel);
//         textWriter.Write("] ");
//         textWriter.WriteLine(message);
//     }
// }

[MemoryDiagnoser]
public class PostLogEntry
{
    static readonly string NullDevicePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "NUL" : "/dev/null";
    
    ILogger zLogger = default!;
    ILogger msExtConsoleLogger = default!;
    ILogger serilogMsExtLogger = default!;
    ILogger nLogMsExtLogger = default!;

    Serilog.ILogger serilogLogger = default!;
    NLog.Logger nLogLogger = default!;

    [GlobalSetup]
    public void SetUp()
    {
        System.Console.SetOut(TextWriter.Null);
        
        // ZLogger
        
        var zLoggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddZLoggerStream(Stream.Null);
        });

        zLogger = zLoggerFactory.CreateLogger<PostLogEntry>();
        
        // Microsoft.Extensions.Logging.Console
        
        using var msExtLoggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddConsole(options =>
            {
                // options.QueueFullMode = ConsoleLoggerQueueFullMode.DropWrite;
                // options.MaxQueueLength = 1024;
            });
        });

        msExtConsoleLogger = msExtLoggerFactory.CreateLogger<Program>();
        
        // Serilog
        
        serilogLogger = new LoggerConfiguration()
            .WriteTo.Async(a => a.TextWriter(TextWriter.Null))
            .CreateLogger();
        
        var serilogMsExtLoggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddSerilog(new LoggerConfiguration()
                .WriteTo.Async(a => a.TextWriter(TextWriter.Null))
                .CreateLogger());
        });
        
        serilogMsExtLogger = serilogMsExtLoggerFactory.CreateLogger<PostLogEntry>();
        
        // NLog

        {
            var nLogConfig = new NLog.Config.LoggingConfiguration();
            var target = new NLog.Targets.FileTarget("Null")
            {
                FileName = NullDevicePath
            };
            var asyncTarget = new NLog.Targets.Wrappers.AsyncTargetWrapper(target);
            nLogConfig.AddTarget(asyncTarget);
            nLogConfig.AddRuleForAllLevels(asyncTarget);

            nLogLogger = nLogConfig.LogFactory.GetLogger("NLog");
        }
        {
            var nLogConfigForMsExt = new NLog.Config.LoggingConfiguration();
            var target = new NLog.Targets.FileTarget("Null")
            {
                FileName = NullDevicePath
            };
            var asyncTarget = new NLog.Targets.Wrappers.AsyncTargetWrapper(target);
            nLogConfigForMsExt.AddTarget(asyncTarget);
            nLogConfigForMsExt.AddRuleForAllLevels(asyncTarget);

            var nLogMsExtLoggerFactory = LoggerFactory.Create(logging =>
            {
                logging.AddNLog(nLogConfigForMsExt);
            });
            nLogMsExtLogger = nLogMsExtLoggerFactory.CreateLogger<PostLogEntry>();
        }
    }

    [Benchmark]
    public void ZLogger_ZLog()
    {
        var x = 100;
        var y = 200;
        var z = 300;
        zLogger.ZLogInformation($"foo{x} bar{y} nazo{z}");
    }

    [Benchmark]
    public void MicrosoftExtensionsLoggingConsole_Log()
    {
        msExtConsoleLogger.LogInformation("x={X} y={Y} z={Z}", 100, 200, 300);
    }

    [Benchmark]
    public void Serilog_MicrosoftExtensionsLogging_Log()
    {
        serilogMsExtLogger.LogInformation("x={X} y={Y} z={Z}", 100, 200, 300);
    }

    [Benchmark]
    public void NLog_MicrosoftExtensionsLogging_Log()
    {
        nLogMsExtLogger.LogInformation("x={X} y={Y} z={Z}", 100, 200, 300);
    }
    
    [Benchmark]
    public void Serilog_Log()
    {
        serilogLogger.Information("x={X} y={Y} z={Z}", 100, 200, 300);
    }
    
    [Benchmark]
    public void NLog_Log()
    {
        nLogLogger.Info("x={X} y={Y} z={Z}", 100, 200, 300);
    }
}
