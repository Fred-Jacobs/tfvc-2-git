using System;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Serilog;
using Tfvc2Git.Core.Extensions;
using Tfvc2Git.Core.Infrastructure;
using Tfvc2Git.Core.Mappers;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.Repositories
{
    public class TfvcRepository
    {
        #region Properties
        public string TfsCollectionUri => _config.TfvcCollectionUri;
        public AuthorsMap Authors { get; }
        public TfsTeamProjectCollection TeamProjectCollection => _tpc ?? (_tpc = GetTfsTeamProjectCollection());
        public VersionControlServer Vcs => _vcs ?? (_vcs = TeamProjectCollection.GetService<VersionControlServer>());
#pragma warning disable 618
        public IGroupSecurityService GroupSecurity => _gss ?? (_gss = TeamProjectCollection.GetService<IGroupSecurityService>());
#pragma warning restore 618
        #endregion

        #region Fields
        private readonly Tfvc2GitConfig _config;
        private readonly FileSystem _io;
        private readonly ILogger _log = Log.Logger.ForContext<TfvcRepository>();
#pragma warning disable 618
        private IGroupSecurityService _gss;
#pragma warning restore 618
        private TfsTeamProjectCollection _tpc;
        private VersionControlServer _vcs;
        #endregion

        private TfvcRepository()
        {
        }

        public TfvcRepository(Tfvc2GitConfig config, AuthorsMap authors, FileSystem io)
        {
            _io = io;
            _config = config;
            Authors = authors;
        }

        public Author GetAuthor(Changeset changeset)
        {
            var committer = changeset.Committer;
            if (committer.Contains(":"))
            {
                committer = committer.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries).First();
            }

            var author = Authors.Get(committer);
            if (null != author)
            {
                return author;
            }

            Identity identity = null;
            try
            {
                identity = GroupSecurity.ReadIdentity(SearchFactor.AccountName, committer, QueryMembership.None);
            }
            catch
            {
            }

            var name = changeset.CommitterDisplayName;
            var email = committer;

            if (identity != null)
            {
                if (!string.IsNullOrWhiteSpace(identity.DisplayName))
                    name = identity.DisplayName;

                if (!string.IsNullOrWhiteSpace(identity.MailAddress))
                    email = identity.MailAddress;
            }
            else if (!string.IsNullOrWhiteSpace(committer))
            {
                var split = committer.Split('\\');
                switch (split.Length)
                {
                    case 0:
                        break;
                    case 1:
                        email = $"{split[0].ToLower()}@tfs.local";
                        break;
                    default:
                        name = split[1].ToLower();
                        email = $"{name}@{split[0].ToLower()}.tfs.local";
                        break;
                }
            }

            if (!email.Contains("@"))
            {
                email = AsEmail(email);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Unknown TFS user";
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                email = "unknown@tfs.local";
            }

            author = Authors.Register(committer, name, email);

            return author;
        }

        public Change[] GetChanges(int changesetId, string tfvcServerPath, bool filterByServerPath = false, bool includeMergeSourceInfos = true,
            bool includeFolders = false)
        {
            var itemSpecification = new ItemSpec(tfvcServerPath, RecursionType.Full);
            _log.Debug(" - Fetching Tfvc changes for ChangesetId {ChangesetId}", changesetId);
            var changes = Vcs.GetChangesForChangeset(changesetId, true, int.MaxValue, itemSpecification, null, includeMergeSourceInfos);
            _log.Verbose(" - Found {ChangesCount} changes for ChangesetId {ChangesetId}", changes.Length, changesetId);
            if (filterByServerPath)
            {
                changes = changes.Where(x => x.Item.ServerItem.StartsWith(tfvcServerPath, StringComparison.OrdinalIgnoreCase)).ToArray();
                _log.Verbose(" - {ChangesCount} changes left after filtering by path {ServerPath}", changes.Length, tfvcServerPath);
            }

            if (!includeFolders)
            {
                changes = changes.Where(x => x.Item.ItemType != ItemType.Folder).ToArray();
                _log.Verbose(" - {ChangesCount} changes left after filtering folders", changes.Length);
            }

            return changes;
        }

        public Changeset[] GetHistory(string tfvcPath, int maxChangeSets = int.MaxValue)
        {
            _log.Verbose(" - Fetching Tfvc history for path {ServerPath}", tfvcPath);
            var result = Vcs
                .QueryHistory(tfvcPath, RecursionType.Full, maxChangeSets)
                .ToArray();
            _log.Verbose(" - Found {HistoryEntriesCount} history entries for path {ServerPath}", result.Length, tfvcPath);

            return result;
        }

        public Changeset[] GetHistory(string tfvcPath, int fromChangesetId, int toChangesetId, int maxChangeSets = int.MaxValue)
        {
            _log.Verbose(" - Fetching Tfvc history for path {ServerPath}", tfvcPath);
            var result = Vcs
                .QueryHistory(tfvcPath, RecursionType.Full, maxChangeSets)
                .ToArray();
            _log.Verbose(" - Found {HistoryEntriesCount} history entries for path {ServerPath}", result.Length, tfvcPath);

            return result;
        }

        public ItemSet GetFiles(string serverPath, bool includeDeleted, int changesetId = int.MaxValue)
        {
            var versionSpec = changesetId == int.MaxValue
                ? VersionSpec.Latest
                : new ChangesetVersionSpec(changesetId);
            var deletedState = includeDeleted ? DeletedState.Any : DeletedState.NonDeleted;
            var items = Vcs.GetItems(serverPath, versionSpec, RecursionType.Full, deletedState, ItemType.File);
            return items;
        }

        public void DownloadFolderAtVersion(string serverPath, string localPath, int changesetId, Func<string, bool> ignoreFilter)
        {
            var versionSpec = new ChangesetVersionSpec(changesetId);
            _log.Debug(" - Fetching Tfvc items for ChangesetId {ChangesetId} in path {ServerPath}", changesetId, serverPath);
            var items = Vcs.GetItems(serverPath, versionSpec, RecursionType.Full, DeletedState.Any, ItemType.File);
            _log.Verbose(" - Found {ItemsCount} items for ChangesetId {ChangesetId} in path {ServerPath}", items.Items.Length, changesetId, serverPath);
            var current = 0;
            var total = items.Items.Length;
            foreach (var item in items.Items)
            {
                current++;
                if (!item.ServerItem.StartsWith(serverPath, StringComparison.OrdinalIgnoreCase))
                {
                    _log.Verbose(" - Skip downloading item ({current}/{total}) {ServerItem}", current, total, item.ServerItem);
                    continue;
                }

                var relativePath = item.GetRelativePath(serverPath);

                if (relativePath.StartsWith("$/"))
                {
                    throw new ApplicationException($"This path '{relativePath}' shouldn't be copied over!");
                }

                if (ignoreFilter.Invoke(relativePath))
                {
                    _log.Verbose(" - Skip downloading item ({current}/{total}) {ServerItem}", current, total, item.ServerItem);
                    continue;
                }

                var localFilePath = Path.Combine(localPath, relativePath);
                _io.EnsureParentFolderExists(localFilePath);

                if (item.DeletionId != 0)
                {
                    _log.Verbose(" - Skip downloading item with deletionId '{deletionId}' ({current}/{total}) {ServerItem}",
                        item.DeletionId, current, total, item.ServerItem);
                    continue;
                }

                _log.Verbose(" - Download item ({current}/{total}) {ServerItem} at Changeset {ChangesetId} to {GitItemPath}",
                    current, total, item.ServerItem, changesetId, localFilePath);
                Vcs.DownloadFile(item.ServerItem, item.DeletionId, versionSpec, localFilePath);
            }
        }

        private static string AsEmail(string value)
        {
            var split = value.Split('\\');
            switch (split.Length)
            {
                case 0:
                    return value;
                case 1:
                    return $"{split[0].ToLower()}@tfs.local";
                default:
                    var name = split[1].ToLower();
                    return $"{name}@{split[0].ToLower()}.tfs.local";
            }
        }

        private TfsTeamProjectCollection GetTfsTeamProjectCollection()
        {
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(TfsCollectionUri));
            tpc.Authenticate();
            return tpc;
        }
    }
}