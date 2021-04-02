using System;
using System.Linq;
using Tfvc2Git.Core.Configuration;
using Tfvc2Git.Core.Configuration.Options;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Repositories;
using Tfvc2Git.Core.RunHandlers.Base;

namespace Tfvc2Git.Core.RunHandlers
{
    public sealed class CheckUpdateRunHandler : RunHandlerBase<CheckUpdateOptions>
    {
        #region Fields
        private readonly Tfvc2GitRepository _repository;
        #endregion

        public CheckUpdateRunHandler(Tfvc2GitRepository repository, ConfigBuilder configBuilder, Tfvc2GitConfig config)
            : base(configBuilder, config)
        {
            _repository = repository;
        }

        protected override void Process()
        {
            var notCommitted = Config.History.Where(x => !x.Committed).ToArray();

            if (notCommitted.Any() || !Config.History.Any())
            {
                Log.Error(" - Repository is not up to date : {notCommittedCount}/{total}", notCommitted.Length, Config.History.Count);
                Log.Error(" - Update repository!");
                Environment.Exit(1);
            }

            Log.Information(" - Repository looks up to date : {committedCount}/{total}", Config.History.Count - notCommitted.Length, Config.History.Count);
        }
    }
}