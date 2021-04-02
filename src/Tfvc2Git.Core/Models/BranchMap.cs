using System;
using System.Diagnostics;
using Microsoft.TeamFoundation.VersionControl.Client;
using Newtonsoft.Json;

namespace Tfvc2Git.Core.Models
{
    [Serializable]
    [DebuggerDisplay("{GitBranchName}:{TfvcServerPath}")]
    public sealed class BranchMap
    {
        #region Properties
        public string TfvcServerPath { get; set; }
        public string GitBranchName { get; set; }
        public string SubFolder { get; set; }
        public bool InitFromFirstMainCommit { get; set; }
        public int? InitFromChangesetId { get; set; }

        [JsonIgnore] public Changeset[] TfvcHistory { get; set; }
        #endregion

        public override string ToString() => $"{GitBranchName}:{TfvcServerPath}";
    }
}