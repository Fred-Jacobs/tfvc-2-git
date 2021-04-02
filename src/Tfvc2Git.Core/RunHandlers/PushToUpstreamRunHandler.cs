using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using Tfvc2Git.Core.Configuration;
using Tfvc2Git.Core.Configuration.Options;
using Tfvc2Git.Core.Extensions;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Repositories;
using Tfvc2Git.Core.RunHandlers.Base;

namespace Tfvc2Git.Core.RunHandlers
{
    public sealed class PushToUpstreamRunHandler : RunHandlerBase<PushToUpstreamOptions>
    {
        #region Statics & Constants
        public const string UpstreamName = "tfvc-2-git-upstream";
        #endregion

        #region Fields
        private readonly PushOptions _pushOptions;
        private readonly Tfvc2GitRepository _repository;
        #endregion

        public PushToUpstreamRunHandler(Tfvc2GitRepository repository, ConfigBuilder configBuilder, Tfvc2GitConfig config)
            : base(configBuilder, config)
        {
            _repository = repository;
            _pushOptions = new PushOptions
            {
                OnPushTransferProgress = (a, b, c) =>
                {
                    Log.Information("Transfer status: {0} {1} {2}", a, b, c);
                    return true;
                },
                OnPackBuilderProgress = (a, b, c) =>
                {
                    Log.Information("Builder status: {0} {1} {2}", a, b, c);
                    return true;
                },
                OnPushStatusError = a => Log.Error("Push error: {0}", a.Message)
            };
        }

        protected override void PreProcess()
        {
            base.PreProcess();
            Guard.IsFalse(string.IsNullOrWhiteSpace(Config.GitRemoteUri), "Git remote is not defined in configuration");
            var notCommitted = Config.History.Where(x => !x.Committed).ToArray();
            if (!notCommitted.Any())
                return;

            Log.Error(" - Repository is not up to date : {notCommittedCount}/{total}", notCommitted.Length, Config.History.Count);
            Log.Error(" - Update repository!");
            Environment.Exit(1);
        }

        protected override void Process()
        {
            var remote = GetRemote();
            Guard.IsNotNull(remote, "remote != null");

            var branches = _repository.Git.Branches
                .Where(x => !x.IsRemote)
                .ToArray();

            foreach (var branch in branches)
            {
                if (!branch.IsTracking)
                {
                    var updatedBranch = _repository.Git.Branches.Update(branch,
                        b => b.Remote = remote.Name,
                        b => b.UpstreamBranch = branch.CanonicalName);
                }
            }

            branches = _repository.Git.Branches
                .Where(x => !x.IsRemote)
                .ToArray();
            var refSpecs = new List<string>();
            var pushMode = Options.Force ? "+" : string.Empty;
            foreach (var branch in branches)
            {
                Guard.IsTrue(branch.IsTracking, $"Branch '{branch.CanonicalName}' is not tracking.");
                var refSpec = $"{pushMode}refs/heads/{branch.FriendlyName}:refs/heads/{branch.FriendlyName}";
                refSpecs.Add(refSpec);
            }

            _repository.Git.Network.Push(remote, refSpecs, _pushOptions);
        }

        private Remote GetRemote()
        {
            var remote = _repository.Git.Network.Remotes.SingleOrDefault(x => x.Name.Equals(UpstreamName));
            if (null == remote)
            {
                Log.Information(" - Add remote {RemoteName} => {RemoteUri}", UpstreamName, Config.GitRemoteUri);
                _repository.Git.Network.Remotes.Add(UpstreamName, Config.GitRemoteUri);
                remote = _repository.Git.Network.Remotes.SingleOrDefault(x => x.Name.Equals(UpstreamName));
            }
            else if (!remote.PushUrl.Equals(Config.GitRemoteUri))
            {
                Log.Information(" - Found remote {RemoteName} => {RemoteUri}", UpstreamName, Config.GitRemoteUri);
            }
            else
            {
                Log.Information(" - Update remote {RemoteName} : {OldRemoteUri} => {RemoteUri}", UpstreamName, remote.PushUrl, Config.GitRemoteUri);
                _repository.Git.Network.Remotes.Remove(UpstreamName);
                _repository.Git.Network.Remotes.Add(UpstreamName, Config.GitRemoteUri);
                remote = _repository.Git.Network.Remotes.SingleOrDefault(x => x.Name.Equals(UpstreamName));
            }

            return remote;
        }
    }
}