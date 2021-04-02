using CommandLine;
using Serilog;
using Tfvc2Git.Core.Infrastructure;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.Configuration.Options
{
    public abstract class OptionsBase
    {
        #region Properties
        [Option("tfvc",
            HelpText = "override configuration value for tfvc connection")]
        public string TfvcCollectionUri { get; set; }

        [Option('l', "log-file",
            HelpText = "log to file, must be a valid file name")]
        public string LogFilePath { get; set; }

        [Option('d', "debug",
            HelpText = "set logging level to debug")]
        public bool Debug { get; set; }

        [Option('v', "verbose",
            HelpText = "set logging level to verbose (take precedence over debug)")]
        public bool Verbose { get; set; }
        #endregion

        #region Fields
        protected FileSystem Io;
        protected ILogger Log;
        #endregion

        public OptionsBase Use(ILogger log)
        {
            Log = log;
            return this;
        }

        public OptionsBase Use(FileSystem io)
        {
            Io = io;
            return this;
        }

        public virtual void ApplyTo(Tfvc2GitConfig config)
        {
            config.Options = this;

            if (!string.IsNullOrWhiteSpace(TfvcCollectionUri))
            {
                Log?.Debug(" - Use '{NameOfTfvcCollectionUri}' value from command line arguments : '{TfvcCollectionUri}'", nameof(TfvcCollectionUri),
                    TfvcCollectionUri);
                config.TfvcCollectionUri = TfvcCollectionUri;
            }
        }
    }
}