using System;
using LibGit2Sharp;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Repositories;

namespace Tfvc2Git.Core.ActionHandlers
{
    public sealed class FirstCommitHandler : Tfvc2GitActionBase
    {
        public void Handle(Tfvc2GitRepository repository, BranchMap branchMap, HistoryEntry historyEntry)
        {
            var gitBranch = repository.Git.Branches[branchMap.GitBranchName];
            if (null == gitBranch)
                gitBranch = repository.Git.CreateBranch(branchMap.GitBranchName, repository.InitialCommit);

            repository.Clean();
            repository.Checkout(gitBranch.FriendlyName);

            var gitStatus = repository.Git.RetrieveStatus();
            if (gitStatus.IsDirty || !gitBranch.IsCurrentRepositoryHead)
            {
                throw new ApplicationException("Unexpected repository state...");
            }

            repository.Tfvc.DownloadFolderAtVersion(historyEntry.Branch.TfvcServerPath, repository.Config.GitLocalPath, historyEntry.ChangesetId,
                relativePath => repository.Git.Ignore.IsPathIgnored(relativePath));
        }
    }
}