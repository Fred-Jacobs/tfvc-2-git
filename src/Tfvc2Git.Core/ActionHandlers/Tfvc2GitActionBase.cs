using System;
using System.IO;
using LibGit2Sharp;
using Microsoft.TeamFoundation.VersionControl.Client;
using Serilog;
using Tfvc2Git.Core.Extensions;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Models.BranchIntegrity;
using Tfvc2Git.Core.Repositories;

namespace Tfvc2Git.Core.ActionHandlers
{
    public abstract class Tfvc2GitActionBase : IDisposable
    {
        #region Properties
        protected ILogger Log { get; }
        #endregion

        protected Tfvc2GitActionBase()
        {
            var type = GetType();
            Log = Serilog.Log.Logger.ForContext(type);
            Log.Information(" <{TypeName}>", type.Name.Replace("Handler", string.Empty));
        }

        public IntegrityResult ValidateIntegrity(Tfvc2GitRepository repository, BranchMap branchMap, int changesetId)
        {
            using (var integrity = new StateIntegrityCheckHandler())
            {
                var branchIntegrityResult = integrity.Validate(repository, branchMap, repository.Config.GitLocalPath, changesetId);
                return branchIntegrityResult;
            }
        }

        public IntegrityResult FixIntegrity(Tfvc2GitRepository repository, BranchMap branchMap, int changesetId, bool throwIfInvalid = true)
        {
            // fix up bad practices of adding/editing/delete files on tfvc branch/merge/clone...
            var integrity = ValidateIntegrity(repository, branchMap, changesetId);
            if (integrity.IsValid)
                return integrity;

            var versionSpec = new ChangesetVersionSpec(changesetId);
            foreach (var invalidFile in integrity.InvalidFiles)
            {
                foreach (var fileIntegrityIssueType in invalidFile.Issues)
                {
                    switch (fileIntegrityIssueType)
                    {
                        case FileIntegrityIssueType.MissingInTfvc:
                            if (!invalidFile.GitStates.Contains(FileStatus.Conflicted))
                            {
                                repository.Unstage(invalidFile.RelativePath);
                            }

                            repository.Git.Index.Remove(invalidFile.RelativePath);
                            if (File.Exists(invalidFile.GitLocalPath))
                            {
                                File.Delete(invalidFile.GitLocalPath);
                            }

                            break;
                        case FileIntegrityIssueType.MissingInGit:
                            if (File.Exists(invalidFile.GitLocalPath))
                            {
                                File.Delete(invalidFile.GitLocalPath);
                            }

                            var gitLocalPath = invalidFile.GitLocalPath;
                            if (string.IsNullOrWhiteSpace(gitLocalPath))
                            {
                                gitLocalPath = Path.Combine(repository.Config.GitLocalPath, invalidFile.RelativePath);
                            }

                            repository.Tfvc.Vcs.DownloadFile(invalidFile.TfvcServerPath, 0, versionSpec, gitLocalPath);
                            break;
                        case FileIntegrityIssueType.ContentMismatch:
                            if (File.Exists(invalidFile.GitLocalPath))
                            {
                                File.Delete(invalidFile.GitLocalPath);
                            }

                            repository.Tfvc.Vcs.DownloadFile(invalidFile.TfvcServerPath, 0, versionSpec, invalidFile.GitLocalPath);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            var status = repository.Git.RetrieveStatus(new StatusOptions
            {
                IncludeUntracked = true
            });
            foreach (var statusEntry in status.Untracked)
            {
                repository.Stage(statusEntry.FilePath);
            }

            foreach (var statusEntry in status.Modified)
            {
                repository.Stage(statusEntry.FilePath);
            }

            integrity = ValidateIntegrity(repository, branchMap, changesetId);
            Guard.IsTrue(integrity.IsValid, "Repository integrity cannot be validated.");
            return integrity;
        }

        #region
        public void Dispose()
        {
            Log.Information(" </{TypeName}>", GetType().Name.Replace("Handler", string.Empty));
        }
        #endregion
    }
}