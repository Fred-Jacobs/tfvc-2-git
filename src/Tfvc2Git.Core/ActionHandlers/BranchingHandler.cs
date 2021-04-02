using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LibGit2Sharp;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;
using Tfvc2Git.Core.Extensions;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Repositories;

namespace Tfvc2Git.Core.ActionHandlers
{
    public sealed class BranchingHandler : Tfvc2GitActionBase
    {
        public void Handle(Tfvc2GitRepository repository, BranchMap branchMap, HistoryEntry historyEntry, bool filterByHistory = true)
        {
            var branchFilter = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            branchFilter.AddRange(repository.Config.Branches.Select(x => x.TfvcServerPath));
            var currentTfvcBranchIdentifier = new ItemIdentifier(branchMap.TfvcServerPath);
            var parentTfvcBranches = repository.Tfvc.Vcs
                .QueryBranchObjects(currentTfvcBranchIdentifier, RecursionType.OneLevel)
                .Where(x => branchFilter.Contains(x.Properties.RootItem.Item))
                .ToArray();
            if (parentTfvcBranches.Length > 1)
            {
                Log.Error(" - Multiples parent branches found for {ServerPath}!", branchMap.TfvcServerPath);
                throw new ApplicationException($"Multiples parent branches found for {branchMap.TfvcServerPath} : {parentTfvcBranches.Length}");
            }

            var parentTfvcBranch = parentTfvcBranches.SingleOrDefault()?.Properties.ParentBranch;
            if (!parentTfvcBranches.Any())
            {
                Log.Error(" - No parent branch found for {ServerPath}!", branchMap.TfvcServerPath);
            }
            else if (null != parentTfvcBranch)
            {
                var parentRootChangesetId = ((ChangesetVersionSpec) parentTfvcBranch.Version).ChangesetId;

                var sourceItemIdentifier = new ItemIdentifier(parentTfvcBranch.Item);
                var targetItemIdentifier = new[] {new ItemIdentifier(branchMap.TfvcServerPath)};
                var allChangesetIds = repository.Config.History
                    .Where(x => x.ChangesetId < historyEntry.ChangesetId)
                    .Where(x => x.ChangesetId >= parentRootChangesetId)
                    .Select(x => x.ChangesetId)
                    .Distinct()
                    .ToArray();
                var extendedMerges = new List<ExtendedMerge>();
                var changesetIdsTemp = allChangesetIds.ToList();
                const int chunkSize = 50;
                while (changesetIdsTemp.Any())
                {
                    var currentIds = changesetIdsTemp.Take(chunkSize).ToList();
                    var merges = repository.Tfvc.Vcs.TrackMerges(currentIds.ToArray(), sourceItemIdentifier, targetItemIdentifier, null);
                    extendedMerges.AddRange(merges);
                    changesetIdsTemp = changesetIdsTemp.Skip(chunkSize).ToList();
                }

                var mergeChangesetIds = extendedMerges
                    .Where(x => x.TargetChangeset.ChangesetId == historyEntry.ChangesetId)
                    .Select(x => x.SourceChangeset.ChangesetId)
                    .ToList();

                Log.Debug(" - Found {mergeChangesetIdsCount} possibles source changesetIds", mergeChangesetIds.Count);
                if (filterByHistory)
                {
                    mergeChangesetIds = mergeChangesetIds
                        .Where(x => repository.Config.History.Any(xx => xx.ChangesetId == x))
                        .ToList();
                    Log.Debug(" - {mergeChangesetIdsCount} possibles source changesetIds left after filtering by know history", mergeChangesetIds.Count);
                }

                if (mergeChangesetIds.Any())
                {
                    var rootChangeset = mergeChangesetIds.Max();
                    Handle(repository, historyEntry, rootChangeset, parentTfvcBranch.Item);
                    return;
                }
            }

            Log.Debug(" - No source changesetId found in history, fallback to Initial commit {GitSha} as source", repository.InitialCommitSha);
            Handle(repository, historyEntry, repository.InitialCommitSha);
        }

        private void Handle(Tfvc2GitRepository repository, HistoryEntry historyEntry, string sha)
        {
            Log.Information(" - Use commit {GitSha} as branch source.", sha);

            var commit = repository.Git.Lookup<Commit>(sha);
            Guard.IsNotNull(commit, $"Commit '{sha}' not found!");

            historyEntry
                .WithSha(sha)
                .WithActionTag($"branched:{historyEntry.ChangesetId}:{historyEntry.ParentChangesetId}");

            repository.Git.CreateBranch(historyEntry.Branch.GitBranchName, commit);
            repository.Checkout(historyEntry.Branch.GitBranchName);
        }

        private void Handle(Tfvc2GitRepository repository, HistoryEntry historyEntry, int rootChangeset, string serverPath)
        {
            Log.Information(" - Use ChangesetId {ChangesetId} from branch {ServerPath} as branch source.", rootChangeset, serverPath);
            var parentHistoryEntry = repository.Config.History.SingleOrDefault(x => x.ChangesetId == rootChangeset);
            if (null == parentHistoryEntry)
            {
                Log.Debug(" - ChangesetId {ChangesetId} not found in configured branches.");
                Debugger.Break();
                throw new ApplicationException("parent branch not found in configured branches!");
            }

            historyEntry.ParentChangesetId = parentHistoryEntry.ChangesetId;

            Handle(repository, historyEntry, parentHistoryEntry.GitSha);
        }
    }
}