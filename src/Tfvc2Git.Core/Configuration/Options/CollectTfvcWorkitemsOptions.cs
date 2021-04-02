using CommandLine;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.Configuration.Options
{
    [Verb("collect-tfvc-workitems", HelpText = "Collect workitems associated with a converted Tfvc2Git repository, reading from Tfvc history.")]
    public sealed class CollectTfvcWorkitemsOptions : OptionsBase
    {
        #region Properties
        [Option('c', "config", Required = true,
            HelpText = "json configuration file path")]
        public string ConfigurationFilePath { get; set; }

        [Option('f', "file", Required = true,
            HelpText = "write to file, must be a valid file name")]
        public string FilePath { get; set; }
        #endregion

        public override void ApplyTo(Tfvc2GitConfig config)
        {
            config.UseGit = false;
            config.UseTfvc = true;
            config.InitChangeSets = true;

            var configFromDisk = Io.FromJson<Tfvc2GitConfig>(ConfigurationFilePath);
            config.Apply(configFromDisk);

            base.ApplyTo(config);
        }
    }
}