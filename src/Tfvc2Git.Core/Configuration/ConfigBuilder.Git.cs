using System;
using System.Diagnostics;
using System.Linq;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Repositories;

namespace Tfvc2Git.Core.Configuration
{
    public sealed partial class ConfigBuilder
    {
        private ConfigBuilder HydrateFromTfvc2GitBranch()
        {
            if (!_config.UseGit)
                return this;

            _log.Information("{MethodName}", nameof(HydrateFromTfvc2GitBranch));
            _repository.Init();

            var note = _repository.MigrationNote;
            if (null == note)
                throw new ApplicationException("Migration note not found!");

            if (note.Message.Equals(Tfvc2GitRepository.LogNoteMessage))
                return this;

            var migrationLogEntries = MigrationLogEntry.FromNoteMessage(note.Message).ToList();
            foreach (var logEntry in migrationLogEntries)
            {
                var historyEntry = _config.History
                    .Where(x => x.ChangesetId == logEntry.ChangesetId)
                    .SingleOrDefault(x => x.Branch.TfvcServerPath.Equals(logEntry.TfvcServerPath));
                if (null == historyEntry || historyEntry.Committed)
                {
                    _log.Error("Changeset '{ChangesetId}' not found in history.", logEntry.ChangesetId);
                    Debugger.Break();
                    continue;
                }

                historyEntry.GitSha = logEntry.GitSha;
                historyEntry.ActionTag = logEntry.Action;
                historyEntry.Committed = true;

                if (historyEntry.ActionTag == "first_commit")
                {
                    historyEntry.IsFirstCommit = true;
                }
            }

            return this;
        }
    }
}