using System;
using System.Linq;
using LibGit2Sharp;
using Tfvc2Git.Core.Extensions;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Repositories;

namespace Tfvc2Git.Core.ActionHandlers
{
    public sealed class MergeHandler : Tfvc2GitActionBase
    {
        #region Fields
        private readonly MergeOptions _mergeOptions = new MergeOptions
        {
            FailOnConflict = true,
            FastForwardStrategy = FastForwardStrategy.NoFastForward,
            FindRenames = true,
            MergeFileFavor = MergeFileFavor.Theirs,
            CommitOnSuccess = false,
            FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
            IgnoreWhitespaceChange = false
        };

        private readonly MergeOptions _mergeOptionsNoFail = new MergeOptions
        {
            FailOnConflict = false,
            FastForwardStrategy = FastForwardStrategy.NoFastForward,
            FindRenames = true,
            MergeFileFavor = MergeFileFavor.Theirs,
            CommitOnSuccess = false,
            FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
            IgnoreWhitespaceChange = false
        };
        #endregion

        public void Handle(Tfvc2GitRepository repository, BranchMap branchMap, HistoryEntry historyEntry, HistoryEntry parentHistoryEntry)
        {
            Guard.IsNotNull(parentHistoryEntry, nameof(parentHistoryEntry));

            historyEntry
                .WithSha(parentHistoryEntry.GitSha)
                .WithParentChangesetId(parentHistoryEntry.ChangesetId);

            var changeset = branchMap.TfvcHistory.Single(x => x.ChangesetId == historyEntry.ChangesetId);

            historyEntry.Author = repository.Tfvc.GetAuthor(changeset);

            var branch = repository.Git.Branches[branchMap.GitBranchName];
            if (!branch.IsCurrentRepositoryHead || !repository.Git.Head.FriendlyName.Equals(branch.FriendlyName))
            {
                repository.Clean();
                repository.Checkout(branchMap.GitBranchName);
            }

            var author = repository.GetSignature(historyEntry.Author, new DateTimeOffset(changeset.CreationDate));
            var mergeResult = repository.Git.Merge(parentHistoryEntry.GitSha, author, _mergeOptions);

            switch (mergeResult.Status)
            {
                case MergeStatus.UpToDate:
                case MergeStatus.NonFastForward:
                    break;
                case MergeStatus.Conflicts:
                    mergeResult = repository.Git.Merge(parentHistoryEntry.GitSha, author, _mergeOptionsNoFail);
                    Log.Warning(" - First chance on {State}", mergeResult.Status);
                    break;
                case MergeStatus.FastForward:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //Guard.IsFalse(mergeResult.Status == MergeStatus.Conflicts, "Merge conflicts");
        }
    }
}