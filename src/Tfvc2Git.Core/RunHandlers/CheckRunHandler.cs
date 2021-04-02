using System;
using System.Collections.Generic;
using System.Linq;
using Tfvc2Git.Core.ActionHandlers;
using Tfvc2Git.Core.Configuration;
using Tfvc2Git.Core.Configuration.Options;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Models.BranchIntegrity;
using Tfvc2Git.Core.Repositories;
using Tfvc2Git.Core.RunHandlers.Base;

namespace Tfvc2Git.Core.RunHandlers
{
    public sealed class CheckRunHandler : RunHandlerBase<CheckOptions>
    {
        #region Fields
        private readonly Tfvc2GitRepository _repository;
        #endregion

        public CheckRunHandler(Tfvc2GitRepository repository, ConfigBuilder configBuilder, Tfvc2GitConfig config)
            : base(configBuilder, config)
        {
            _repository = repository;
        }

        protected override void PreProcess()
        {
            base.PreProcess();
            var notCommitted = Config.History.Where(x => !x.Committed).ToArray();
            if (!notCommitted.Any())
                return;

            Log.Error(" - Repository is not up to date : {notCommittedCount}/{total}", notCommitted.Length, Config.History.Count);
            Log.Error(" - Update repository!");
            Environment.Exit(1);
        }

        protected override void Process()
        {
            var results = new Dictionary<BranchMap, IntegrityResult>();
            foreach (var branchMap in Config.Branches)
            {
                Log.Information("Branch : {GitBranch}:{TfvcServerPath}", branchMap.GitBranchName, branchMap.TfvcServerPath);

                using (var integrity = new StateIntegrityCheckHandler())
                {
                    var branch = _repository.Git.Branches[branchMap.GitBranchName];
                    if (!branch.IsCurrentRepositoryHead || !_repository.Git.Head.FriendlyName.Equals(branch.FriendlyName))
                    {
                        _repository.Clean();
                        _repository.Checkout(branchMap.GitBranchName);
                    }

                    var branchIntegrityResult = integrity.Validate(_repository, branchMap, Config.GitLocalPath);
                    results.Add(branchMap, branchIntegrityResult);
                    if (!branchIntegrityResult.IsValid)
                    {
                        Log.Error(" - Branch looks invalid.");
                    }
                    else
                    {
                        Log.Information(" - Branch looks good.");
                    }
                }

                /*
                //if (changeset.AssociatedWorkItems.Any() || changeset.WorkItems.Any() || changeset.Properties.Any() ||
                //    !string.IsNullOrWhiteSpace(changeset.PolicyOverride.Comment))
                //{
                //    Log.Debug(
                //        " - AssociatedWorkItems {AssociatedWorkItems} | WorkItems {WorkItems} | Properties {Properties} | PolicyOverride {PolicyOverride}",
                //        changeset.AssociatedWorkItems.Length,
                //        changeset.WorkItems.Length,
                //        changeset.Properties.Count,
                //        changeset.PolicyOverride.PolicyFailures.Length
                //    );
                //}
                 */
            }

            foreach (var result in results.Where(x => !x.Value.IsValid))
            {
                Log.Error(" {BranchName} : {TfvcServerPath} : {InvalidFilesCount}",
                    result.Key.GitBranchName, result.Key.TfvcServerPath, result.Value.InvalidFiles.Count);
            }

            if (results.Any(x => !x.Value.IsValid))
                Environment.Exit(1);

            Log.Information("Everything looks good.");
        }
    }
}