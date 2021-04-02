using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace Tfvc2Git.Core.Models.BranchIntegrity
{
    [Serializable]
    public class FileIntegrityResult
    {
        #region Properties
        public string RelativePath { get; internal set; }
        public string TfvcServerPath { get; internal set; }
        public byte[] TfvcHashValue { get; internal set; }
        public byte[] GitHashValue { get; internal set; }
        public bool HasSameHashValues { get; internal set; }
        public bool InGit => !string.IsNullOrWhiteSpace(GitLocalPath);
        public bool InTfvc => !string.IsNullOrWhiteSpace(TfvcServerPath);
        public string GitLocalPath { get; internal set; }
        public bool IsGitIgnored { get; internal set; }
        public IReadOnlyList<FileIntegrityIssueType> Issues => _issues;
        public bool IsValid => !Issues.Any();
        public List<FileStatus> GitStates { get; internal set; } = new List<FileStatus>();
        public bool TfvcDeleted { get; internal set; }
        #endregion

        #region Fields
        private readonly List<FileIntegrityIssueType> _issues = new List<FileIntegrityIssueType>();
        #endregion

        internal FileIntegrityResult RegisterIssue(FileIntegrityIssueType issue)
        {
            _issues.Add(issue);
            return this;
        }
    }
}