using System;

namespace Tfvc2Git.Core.Models.BranchIntegrity
{
    [Serializable]
    public enum FileIntegrityIssueType
    {
        MissingInTfvc,
        MissingInGit,
        ContentMismatch,
        ShouldBeDeletedInGit
    }
}