using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Tfvc2Git.Core.Configuration.Options;

namespace Tfvc2Git.Core.Models
{
    [Serializable]
    public sealed class Tfvc2GitConfig
    {
        #region Properties
        public string TfvcCollectionUri { get; set; }
        public string GitLocalPath { get; set; }
        public string GitRemoteUri { get; set; }
        public string GitIgnoreFilePath { get; set; }
        public int FromChangesetId { get; set; } = -1;
        public int ToChangesetId { get; set; } = -1;
        public List<BranchMap> Branches { get; set; } = new List<BranchMap>();
        public string AuthorMapFilePath { get; set; }
        public bool ValidateEachStep { get; set; }

        [JsonIgnore] public List<HistoryEntry> History { get; set; }
        [JsonIgnore] public OptionsBase Options { get; set; }
        [JsonIgnore] public bool UseTfvc { get; set; }
        [JsonIgnore] public bool UseGit { get; set; }
        [JsonIgnore] public bool InitChangeSets { get; set; }
        #endregion

        public void Apply(Tfvc2GitConfig config)
        {
            TfvcCollectionUri = config.TfvcCollectionUri;
            GitLocalPath = config.GitLocalPath;
            GitRemoteUri = config.GitRemoteUri;
            GitIgnoreFilePath = config.GitIgnoreFilePath;
            FromChangesetId = config.FromChangesetId;
            ToChangesetId = config.ToChangesetId;
            Branches = config.Branches;
            AuthorMapFilePath = config.AuthorMapFilePath;
            ValidateEachStep = config.ValidateEachStep;
        }
    }
}