using System;
using System.Diagnostics;
using Microsoft.TeamFoundation.VersionControl.Client;
using Tfvc2Git.Core.Extensions;
using Tfvc2Git.Core.Repositories;

namespace Tfvc2Git.Core.Models
{
    [DebuggerDisplay("{ChangesetId}:{ParentChangesetId}:{GitSha}:{ActionTag}")]
    public class HistoryEntry
    {
        #region Properties
        public int ChangesetId { get; set; }
        public int? ParentChangesetId { get; set; }
        public string GitSha { get; set; }
        public BranchMap Branch { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public Author Author { get; set; }
        public bool Committed { get; set; }
        public string ActionTag { get; set; }
        public bool IsSplittedChangeset { get; set; }
        public bool IgnoreBranchesFilter { get; set; }
        public bool IsFirstCommit { get; set; }
        public Changeset Changeset { get; set; }
        #endregion

        public HistoryEntry WithSha(string tag)
        {
            GitSha = tag;
            return this;
        }

        public HistoryEntry WithChangesetId(int changesetId)
        {
            ChangesetId = changesetId;
            return this;
        }

        public HistoryEntry WithParentChangesetId(int changesetId)
        {
            ParentChangesetId = changesetId;
            return this;
        }

        public HistoryEntry WithActionTag(string tag)
        {
            ActionTag = tag;
            return this;
        }

        public HistoryEntry PersistInNote(Tfvc2GitRepository tfvc2GitRepository)
        {
            Guard.IsFalse(string.IsNullOrWhiteSpace(ActionTag), "ActionTag cannot be null or empty");
            tfvc2GitRepository.Register(this);
            return this;
        }

        public HistoryEntry SetAsFirstCommit()
        {
            IsFirstCommit = true;
            return this;
        }
    }
}