using CommandLine;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.Configuration.Options
{
    [Verb("check", HelpText = "Check a converted Tfvc2Git repository.")]
    public sealed class CheckOptions : OptionsBase
    {
        #region Properties
        [Option('c', "config", Required = true,
            HelpText = "json configuration file path")]
        public string ConfigurationFilePath { get; set; }
        #endregion

        public override void ApplyTo(Tfvc2GitConfig config)
        {
            config.UseGit = true;
            config.UseTfvc = true;
            config.InitChangeSets = true;

            var configFromDisk = Io.FromJson<Tfvc2GitConfig>(ConfigurationFilePath);
            config.Apply(configFromDisk);

            base.ApplyTo(config);
        }
    }
}