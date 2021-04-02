using Microsoft.TeamFoundation.VersionControl.Client;

namespace Tfvc2Git.Core.Extensions
{
    public static class ChangeTypeExtensions
    {
        public static bool ShouldBeDeleted(this ChangeType changeType) => changeType.HasFlag(ChangeType.Delete);
        public static bool HasBranch(this ChangeType changeType) => changeType.HasFlag(ChangeType.Branch);
        public static bool HasMerge(this ChangeType changeType) => changeType.HasFlag(ChangeType.Merge);

        public static bool HasRename(this ChangeType changeType, bool any = false) =>
            changeType.HasFlag(ChangeType.Rename) || !any && changeType.HasFlag(ChangeType.SourceRename);

        //public static bool Has(this ChangeType changeType) => changeType.HasFlag(ChangeType.Delete);
    }
}