using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LibGit2Sharp;
using Tfvc2Git.Core.Extensions;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Models.BranchIntegrity;
using Tfvc2Git.Core.Repositories;

namespace Tfvc2Git.Core.ActionHandlers
{
    public sealed class StateIntegrityCheckHandler : Tfvc2GitActionBase
    {
        #region Fields
        private readonly bool _extraDebugMessage;

        private readonly StatusOptions _statusOptions = new StatusOptions
        {
            IncludeUnaltered = true,
            IncludeUntracked = true,
            IncludeIgnored = true,
            RecurseIgnoredDirs = true,
            RecurseUntrackedDirs = true
        };
        #endregion

        public StateIntegrityCheckHandler(bool extraDebugMessage = false)
        {
            _extraDebugMessage = extraDebugMessage;
        }

        public IntegrityResult Validate(Tfvc2GitRepository repository, BranchMap branchMap, string gitLocalPath, int changesetId = int.MaxValue)
        {
            var result = new IntegrityResult();
            var itemsMap = new Dictionary<string, FileIntegrityResult>(StringComparer.OrdinalIgnoreCase);
            IterateGit(repository, gitLocalPath, itemsMap, result);
            IterateTfvc(repository, branchMap, changesetId, itemsMap);
            ValidateItems(repository, itemsMap, result);
            return result;
        }

        private void ValidateItems(Tfvc2GitRepository repository, Dictionary<string, FileIntegrityResult> itemsMap, IntegrityResult result)
        {
            var items = itemsMap.Values.OrderBy(x => x.RelativePath);
            foreach (var item in items)
            {
                switch (item.InGit)
                {
                    case true when !item.InTfvc:
                        var canSkip = repository.ExtraFiles.ContainsKey(item.RelativePath)
                                      || item.GitStates.Count == 1 && item.GitStates.Any(x => x == FileStatus.DeletedFromIndex)
                            ;
                        MissingInTfvc(item, canSkip);
                        break;
                    case false when item.InTfvc:
                        MissingInGit(item);
                        break;
                    case true when item.InTfvc:
                        if (item.TfvcDeleted)
                        {
                            Deleted(item);
                        }
                        else
                        {
                            Found(item);
                        }

                        break;
                    default:
                        Log.Error("WTF??? {x}", item.TfvcServerPath);
                        throw new NotImplementedException();
                }
            }

            result.RegisterItems(itemsMap.Values);
        }

        private void Deleted(FileIntegrityResult item)
        {
            Guard.IsTrue(item.TfvcDeleted, "Application flow error...");

            if (item.GitStates.Contains(FileStatus.DeletedFromIndex) || item.GitStates.Contains(FileStatus.DeletedFromWorkdir))
                return;

            item.RegisterIssue(FileIntegrityIssueType.ShouldBeDeletedInGit);
            Log.Warning(" - Should be deleted from Git : {RelativePath}", item.RelativePath);
        }

        private void IterateGit(Tfvc2GitRepository repository, string gitLocalPath, Dictionary<string, FileIntegrityResult> itemsMap, IntegrityResult result)
        {
            var gitItems = repository.Git.RetrieveStatus(_statusOptions);
            foreach (var gitItem in gitItems)
            {
                if (itemsMap.ContainsKey(gitItem.FilePath))
                {
                    // check for duplicate names (git is case sensitive)
                    var existing = itemsMap[gitItem.FilePath];
                    if (!gitItem.FilePath.Equals(existing.RelativePath, StringComparison.InvariantCulture))
                    {
                        if (!result.DuplicateGitItems.Contains(existing.RelativePath))
                        {
                            result.RegisterDuplicateGitItem(existing.RelativePath);
                        }

                        result.RegisterDuplicateGitItem(gitItem.FilePath);
                    }

                    itemsMap[gitItem.FilePath].GitStates.Add(gitItem.State);
                    continue;
                }

                itemsMap.Add(gitItem.FilePath, new FileIntegrityResult
                {
                    RelativePath = gitItem.FilePath,
                    GitLocalPath = Path.Combine(gitLocalPath, gitItem.FilePath),
                    IsGitIgnored = repository.Git.Ignore.IsPathIgnored(gitItem.FilePath),
                    GitStates = new List<FileStatus> {gitItem.State}
                });
            }
        }

        private static void IterateTfvc(Tfvc2GitRepository repository, BranchMap branchMap, int changesetId, Dictionary<string, FileIntegrityResult> itemsMap)
        {
            var tfvcItemsSet = repository.Tfvc.GetFiles(branchMap.TfvcServerPath, false, changesetId);
            var tfvcItems = tfvcItemsSet.Items;
            foreach (var tfvcItem in tfvcItems)
            {
                var relativePath = tfvcItem.GetRelativePath(branchMap.TfvcServerPath);

                if (!itemsMap.ContainsKey(relativePath))
                {
                    itemsMap.Add(relativePath, new FileIntegrityResult
                    {
                        RelativePath = relativePath,
                        IsGitIgnored = repository.Git.Ignore.IsPathIgnored(relativePath)
                    });
                }

                var itemMap = itemsMap[relativePath];
                itemMap.TfvcServerPath = tfvcItem.ServerItem;
                itemMap.TfvcHashValue = tfvcItem.HashValue;
                itemMap.TfvcDeleted = tfvcItem.DeletionId != 0;
            }
        }

        private void MissingInTfvc(FileIntegrityResult item, bool canSkip)
        {
            const string messageTemplate = " - Missing in {Status} : {GitIgnored} : {RelativePath}";
            const string text = "Tfvc";
            if (canSkip)
            {
                if (_extraDebugMessage)
                    Log.Debug(messageTemplate, text, item.RelativePath);
            }
            else
            {
                if (item.IsGitIgnored)
                    return;

                item.RegisterIssue(FileIntegrityIssueType.MissingInTfvc);
                Log.Warning(messageTemplate, text, item.IsGitIgnored, item.RelativePath);
            }
        }

        private void MissingInGit(FileIntegrityResult item)
        {
            const string messageTemplate = " - Missing in {Status} : {GitIgnored} : {RelativePath}";
            const string text = "Git";
            if (!item.IsGitIgnored)
            {
                item.RegisterIssue(FileIntegrityIssueType.MissingInGit);
                Log.Warning(messageTemplate, text, item.IsGitIgnored, item.RelativePath);
            }
            else if (_extraDebugMessage)
            {
                Log.Debug(messageTemplate, text, item.IsGitIgnored, item.RelativePath);
            }
        }

        private void Found(FileIntegrityResult item)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(item.GitLocalPath))
                    {
                        item.GitHashValue = md5.ComputeHash(stream);
                        item.HasSameHashValues =
                            null != item.TfvcHashValue
                            && null != item.GitHashValue
                            && item.TfvcHashValue.SequenceEqual(item.GitHashValue);
                    }
                }
            }
            catch (Exception)
            {
                if (!item.GitStates.Contains(FileStatus.DeletedFromIndex) && !item.GitStates.Contains(FileStatus.DeletedFromWorkdir))
                    throw;
            }

            if (!item.HasSameHashValues)
            {
                item.RegisterIssue(FileIntegrityIssueType.ContentMismatch);
                Log.Warning(" - {Status} : {RelativePath}", "MD5 Hash mismatch", item.RelativePath);
            }
            else
            {
                if (_extraDebugMessage)
                    Log.Debug(" - {Status} : {RelativePath}", "MD5 Hash comparison ok", item.RelativePath);
            }
        }
    }
}