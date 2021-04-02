using CommandLine;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.Configuration.Options
{
    [Verb("collect-tfvc-authors", HelpText = "Collect authors from a tfvc collection history.")]
    public sealed class CollectTfvcAuthorsOptions : OptionsBase
    {
        #region Properties
        [Option('f', "file", Required = true,
            HelpText = "write to file, must be a valid file name")]
        public string FilePath { get; set; }
        #endregion

        public override void ApplyTo(Tfvc2GitConfig config)
        {
            config.UseTfvc = true;

            base.ApplyTo(config);
        }
    }
}