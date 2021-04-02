using System.Diagnostics;
using System.Linq;
using Tfvc2Git.Core.Configuration;
using Tfvc2Git.Core.Configuration.Options;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Repositories;
using Tfvc2Git.Core.RunHandlers.Base;

namespace Tfvc2Git.Core.RunHandlers
{
    public sealed class CollectTfvcWorkitemsRunHandler : RunHandlerBase<CollectTfvcWorkitemsOptions>
    {
        #region Fields
        private readonly Tfvc2GitRepository _repository;
        #endregion

        public CollectTfvcWorkitemsRunHandler(Tfvc2GitRepository repository, ConfigBuilder configBuilder, Tfvc2GitConfig config)
            : base(configBuilder, config)
        {
            _repository = repository;
        }

        protected override void Process()
        {
            //var current = 0;
            //var total = Config.History.Count;
            //foreach (var historyEntry in Config.History)
            //{
            //    Log.Information(" ({current}/{total}) : {ChangesetId}", current, total, historyEntry.ChangesetId);
            //}

            var allHistory = _repository.Tfvc.GetHistory("$/", Config.FromChangesetId, Config.ToChangesetId);

            var current = 0;
            var total = allHistory.Length;
            foreach (var changeset in allHistory)
            {
                current++;
                if (changeset.AssociatedWorkItems.Any() || changeset.WorkItems.Any() || changeset.Properties.Any() ||
                    !string.IsNullOrWhiteSpace(changeset.PolicyOverride.Comment))
                {
                    Log.Information(
                        "({current}/{total}) : {ChangesetId} : AssociatedWorkItems {AssociatedWorkItems} | WorkItems {WorkItems} | Properties {Properties} | PolicyOverride {PolicyOverride}",
                        current, total, changeset.ChangesetId,
                        changeset.AssociatedWorkItems.Length,
                        changeset.WorkItems.Length,
                        changeset.Properties.Count,
                        changeset.PolicyOverride.PolicyFailures.Length
                    );
                    //Debugger.Break();
                }
                else
                {
                    Log.Information("({current}/{total}) : {ChangesetId} : No workitems found.", current, total, changeset.ChangesetId);
                }
            }

            //var sb = new StringBuilder();
            //foreach (var author in _repository.Tfvc.Authors.Authors.OrderBy(x => x.Key))
            //{
            //    sb.AppendLine(author.Value.ToString());
            //}

            //var content = sb.ToString();
            //File.WriteAllText(Options.FilePath, content);

            Debugger.Break();
        }
    }
}