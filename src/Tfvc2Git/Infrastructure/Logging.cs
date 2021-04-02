using Serilog;
using Tfvc2Git.Core.Configuration.Options;

namespace Tfvc2Git.Infrastructure
{
    internal static class LoggingExtensions
    {
        internal static OptionsBase ConfigureLogging(this OptionsBase options)
        {
            Log.Logger = new LoggerConfiguration()
                .SetMinimumLevel(options)
                .WriteTo.Console()
                .SetLogFile(options)
                .CreateLogger();

            return options;
        }

        private static LoggerConfiguration SetMinimumLevel(this LoggerConfiguration config, OptionsBase options)
        {
            if (options.Verbose)
                config = config.MinimumLevel.Verbose();
            else if (options.Debug)
                config = config.MinimumLevel.Debug();
            else
                config = config.MinimumLevel.Information();

            return config;
        }

        private static LoggerConfiguration SetLogFile(this LoggerConfiguration config, OptionsBase options)
        {
            if (!string.IsNullOrWhiteSpace(options.LogFilePath))
            {
                config = config.WriteTo.File(options.LogFilePath);
            }

            return config;
        }
    }
}