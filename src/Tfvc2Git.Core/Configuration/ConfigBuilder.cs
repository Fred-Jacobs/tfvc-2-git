using Serilog;
using Tfvc2Git.Core.Configuration.Options;
using Tfvc2Git.Core.Infrastructure;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Repositories;

namespace Tfvc2Git.Core.Configuration
{
    public sealed partial class ConfigBuilder
    {
        #region Fields
        private readonly Tfvc2GitConfig _config;
        private readonly FileSystem _io;
        private readonly ILogger _log = Log.Logger.ForContext<ConfigBuilder>();
        private readonly OptionsBase _options;
        private readonly Tfvc2GitRepository _repository;
        #endregion

        private ConfigBuilder()
        {
        }

        public ConfigBuilder(Tfvc2GitRepository repository, OptionsBase options, Tfvc2GitConfig config, FileSystem io)
        {
            _repository = repository;
            _options = options;
            _config = config;
            _io = io;
        }

        public void Hydrate()
        {
            _log.Information("{StepName}", "ApplyOptions");
            _options
                .Use(_io)
                .Use(_log)
                .ApplyTo(_config);

            InitChangesets()
                .HydrateFromTfvc2GitBranch();
        }
    }
}