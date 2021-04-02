using Serilog;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.Extensions
{
    public static class HistoryEntryExtensions
    {
        #region Statics & Constants
        public const string Skipped = "Skipped";
        public const string Converted = "Converted/";
        public const string Process = "Process";
        #endregion

        public static HistoryEntry LogSkipped(this HistoryEntry h)
        {
            Log.Information(" - {ActionTag}", Skipped);
            return h;
        }

        public static void LogMigrated(this HistoryEntry h, int current, int total) => h.LogInfo(Converted, current, total);

        public static void LogStartProcess(this HistoryEntry h, int current, int total) => h.LogInfo(Process, current, total);

        public static void LogEndProcess(this HistoryEntry h, int current, int total) => h.LogInfo(Process, current, total, true);

        private static void LogInfo(this HistoryEntry h, string header, int current, int total, bool end = false)
        {
            var template = end
                ? "</{header}({current}/{total})> {ChangeSetId} : {TfvcServerPath} => {GitBranch} : {ActionTag} {Sha}"
                : "<{header}({current}/{total})> {ChangeSetId} : {TfvcServerPath} => {GitBranch} : {ActionTag} {Sha}";

            Log.Information(template,
                header,
                current + 1,
                total,
                h.ChangesetId,
                h.Branch.TfvcServerPath,
                h.Branch.GitBranchName,
                h.ActionTag ?? string.Empty,
                h.GitSha ?? string.Empty
            );
        }
    }
}