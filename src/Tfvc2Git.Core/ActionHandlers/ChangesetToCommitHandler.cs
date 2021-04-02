using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Microsoft.TeamFoundation.VersionControl.Client;
using Tfvc2Git.Core.Extensions;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Repositories;

namespace Tfvc2Git.Core.ActionHandlers
{
    public sealed class ChangesetToCommitHandler : Tfvc2GitActionBase
    {
        #region Enums
        public enum TransformResult
        {
            Fail = 1,
            Success = 2,
            Nochanges = 4
        }
        #endregion

        public TransformResult Transform(Tfvc2GitRepository repository, HistoryEntry historyEntry, Change[] fileChanges, BranchMap branchMap)
        {
            var anyDeleteDone = DeleteChanges(repository, historyEntry, fileChanges, branchMap);
            var anyOtherChangeDone = OtherChanges(repository, historyEntry, fileChanges, branchMap);

            if (!repository.Git.RetrieveStatus().IsDirty)
            {
                return TransformResult.Nochanges;
            }

            Guard.IsFalse(!anyDeleteDone && !anyOtherChangeDone, $"No change??? : {nameof(anyDeleteDone)} | {nameof(anyOtherChangeDone)}");

            return TransformResult.Success;
        }

        private bool OtherChanges(Tfvc2GitRepository repository, HistoryEntry historyEntry, IEnumerable<Change> fileChanges, BranchMap branchMap)
        {
            var anyOtherChangeDone = false;
            var versionSpec = new ChangesetVersionSpec(historyEntry.ChangesetId);
            var otherChanges = fileChanges.Where(x => !x.ChangeType.ShouldBeDeleted()).ToArray();
            foreach (var change in otherChanges)
            {
                if (!change.Item.ServerItem.StartsWith(branchMap.TfvcServerPath, StringComparison.OrdinalIgnoreCase) && !historyEntry.IgnoreBranchesFilter)
                {
                    Log.Debug(" - [SKIP] {ChangeTypes} from outside configured branches : {ServerItem}", change.ChangeType, change.Item.ServerItem);
                    continue;
                }

                var relativePath = change.Item.GetRelativePath(branchMap.TfvcServerPath);
                var gitLocalPath = Path.Combine(repository.Config.GitLocalPath, relativePath);
                var gitLocalSubFolder = Directory.GetParent(gitLocalPath);

                Log.Verbose(" - {ChangeType} : {RelativePath}", change.ChangeType, relativePath);

                if (relativePath.StartsWith("$/"))
                {
                    //throw new ApplicationException($"This path '{relativePath}' shouldn't be copied over!");
                    Log.Error("   This path '{relativePath}' shouldn't be copied over!", relativePath);
                    continue;
                }

                if (change.ChangeType.HasFlag(ChangeType.Add) || change.ChangeType.HasFlag(ChangeType.Edit))
                {
                    if (repository.Git.Ignore.IsPathIgnored(relativePath))
                    {
                        Log.Information("   [SKIP] in .gitignore {RelativePath}", relativePath);
                        continue;
                    }

                    if (!gitLocalSubFolder.Exists)
                    {
                        gitLocalSubFolder.Create();
                    }

                    if (File.Exists(gitLocalPath))
                    {
                        File.Delete(gitLocalPath);
                    }

                    repository.Tfvc.Vcs.DownloadFile(change.Item.ServerItem, change.Item.DeletionId, versionSpec, gitLocalPath);
                }
                else if (change.ChangeType.HasFlag(ChangeType.Rename)
                         || change.ChangeType.HasFlag(ChangeType.Branch)
                         || change.ChangeType.HasFlag(ChangeType.Merge)
                         || change.ChangeType == (ChangeType.None | ChangeType.Rollback)
                         || change.ChangeType.HasFlag(ChangeType.Undelete))
                {
                    if (repository.Git.Ignore.IsPathIgnored(relativePath))
                    {
                        Log.Information("   [SKIP] in .gitignore {RelativePath}", relativePath);
                        continue;
                    }

                    if (!gitLocalSubFolder.Exists)
                    {
                        gitLocalSubFolder.Create();
                    }

                    if (File.Exists(gitLocalPath))
                    {
                        File.Delete(gitLocalPath);
                    }

                    repository.Tfvc.Vcs.DownloadFile(change.Item.ServerItem, change.Item.DeletionId, versionSpec, gitLocalPath);
                }
                else if (change.ChangeType.HasFlag(ChangeType.None) && change.ChangeType.HasFlag(ChangeType.SourceRename))
                {
                    Log.Warning("   [SKIP] ChangeType {ChangeType}", change.ChangeType);
                    continue;
                }
                else
                {
                    Log.Error("   [SKIP] ChangeType {ChangeType}  is not implemented yet...", change.ChangeType);
                    Debugger.Break();
                    throw new ApplicationException($"ChangeType {change.ChangeType} is not implemented yet...");
                }

                anyOtherChangeDone = true;
            }

            return anyOtherChangeDone;
        }

        private bool DeleteChanges(Tfvc2GitRepository repository, HistoryEntry historyEntry, IEnumerable<Change> fileChanges, BranchMap branchMap)
        {
            var anyDeleteDone = false;
            var deleteChanges = fileChanges.Where(x => x.ChangeType.ShouldBeDeleted()).ToArray();
            foreach (var deleteChange in deleteChanges)
            {
                if (!deleteChange.Item.ServerItem.StartsWith(branchMap.TfvcServerPath, StringComparison.OrdinalIgnoreCase) &&
                    !historyEntry.IgnoreBranchesFilter)
                {
                    Log.Debug(" - [SKIP] Delete from outside configured branches : {ServerItem}", deleteChange.Item.ServerItem);
                    continue;
                }

                var relativePath = deleteChange.Item.GetRelativePath(branchMap.TfvcServerPath);
                var gitLocalPath = Path.Combine(repository.Config.GitLocalPath, relativePath);

                if (File.Exists(gitLocalPath))
                {
                    Log.Verbose(" - {ChangeType} file {RelativePath}", deleteChange.ChangeType, relativePath);
                    File.Delete(gitLocalPath);
                    anyDeleteDone = true;
                }
                else
                {
                    Log.Verbose(" - File to delete not found. : {RelativePath}", relativePath);
                }
            }

            return anyDeleteDone;
        }
    }
}