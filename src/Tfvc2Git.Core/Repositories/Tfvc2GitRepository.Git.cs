using System;
using System.Linq;
using LibGit2Sharp;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.Repositories
{
    public sealed partial class Tfvc2GitRepository
    {
        public void Clean()
        {
            var gitStatus = Git.RetrieveStatus();
            if (!gitStatus.IsDirty)
                return;

            _log.Information(" - Git repository is dirty, {GitStatusCount} changes found, clean and reset to current branch head.", gitStatus.Count());
            Io.DeleteAllFiles(Config.GitLocalPath);
            Git.Reset(ResetMode.Hard, Git.Head.Commits.First());
        }

        public Branch Checkout(string branchName)
        {
            var gitBranch = Git.Branches[branchName];
            if (null == gitBranch)
                return null;

            var currentBranch = Commands.Checkout(Git, gitBranch);
            _log.Debug(" - Checkout branch '{GitBranchName}'", currentBranch.FriendlyName);
            return currentBranch;
        }

        public Tfvc2GitRepository Stage(string pattern = "*")
        {
            Commands.Stage(Git, pattern);
            _log.Debug(" - Stage files '{pattern}'", pattern);
            return this;
        }

        public Tfvc2GitRepository Unstage(string pattern = "*")
        {
            Commands.Unstage(Git, pattern);
            _log.Debug(" - Unstage files '{pattern}'", pattern);
            return this;
        }

        public Commit Commit(HistoryEntry historyEntry, bool allowEmpty = false, bool throwOnFailure = true)
        {
            var status = Git.RetrieveStatus();
            if (!status.IsDirty && !allowEmpty)
            {
                return null;
            }

            var options = allowEmpty ? CommitOptionsAllowEmpty : CommitOptions;
            var author = GetSignature(historyEntry.Author, new DateTimeOffset(historyEntry.Date));
            var commit = Git.Commit(historyEntry.Message, author, author, options);

            if (null != commit)
                _log.Debug(" - Commit done '{GitSha}'", commit.Sha);
            else if (throwOnFailure)
                throw new ApplicationException("Commit failed...");
            else
                _log.Warning(" - Commit aborted?");

            return commit;
        }
    }
}