using CommandLine;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.Configuration.Options
{
    [Verb("push-to-upstream", HelpText = "Push a converted Tfvc2Git repository to remote upstream.")]
    public sealed class PushToUpstreamOptions : OptionsBase
    {
        #region Properties
        [Option('c', "config", Required = true,
            HelpText = "json configuration file path")]
        public string ConfigurationFilePath { get; set; }

        [Option("force",
            HelpText = "Force push")]
        public bool Force { get; set; }
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