using System;
using System.Collections.Generic;
using System.Linq;

namespace Tfvc2Git.Core.Models.BranchIntegrity
{
    [Serializable]
    public sealed class IntegrityResult
    {
        #region Properties
        public bool IsValid => Files.All(x => x.IsValid) && !DuplicateGitItems.Any();
        public IReadOnlyList<FileIntegrityResult> Files => _files;
        public IReadOnlyList<FileIntegrityResult> InvalidFiles => _files.Where(x => !x.IsValid).ToList();
        public IReadOnlyList<string> DuplicateGitItems => _duplicateGitItems;
        #endregion

        #region Fields
        private readonly List<string> _duplicateGitItems = new List<string>();
        private readonly List<FileIntegrityResult> _files = new List<FileIntegrityResult>();
        #endregion

        internal IntegrityResult RegisterItems(IEnumerable<FileIntegrityResult> items)
        {
            _files.AddRange(items);
            return this;
        }

        internal IntegrityResult RegisterDuplicateGitItem(string relativePath)
        {
            _duplicateGitItems.Add(relativePath);
            return this;
        }
    }
}