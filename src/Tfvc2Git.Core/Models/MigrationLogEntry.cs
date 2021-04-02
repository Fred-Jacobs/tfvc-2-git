using System;
using System.Collections.Generic;
using System.Linq;

namespace Tfvc2Git.Core.Models
{
    public sealed class MigrationLogEntry
    {
        #region Properties
        public int ChangesetId { get; private set; }
        public string GitBranchName { get; private set; }
        public string GitSha { get; private set; }
        public string TfvcServerPath { get; private set; }
        public string Action { get; private set; }
        #endregion

        private MigrationLogEntry()
        {
        }

        public override string ToString() => string.Join(";", GetParts());

        public static MigrationLogEntry FromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ApplicationException($"'{value}' is not a valid {nameof(MigrationLogEntry)}!");

            var result = new MigrationLogEntry();
            var parts = value.Split(new[] {';'}, StringSplitOptions.None);
            if (parts.Length < result.GetParts().Count())
                throw new ApplicationException($"'{value}' is not a valid {nameof(MigrationLogEntry)}!");

            result.ChangesetId = int.Parse(parts[0]);
            result.GitBranchName = parts[1];
            result.GitSha = parts[2];
            result.TfvcServerPath = parts[3];
            result.Action = parts[4];

            return result;
        }

        public static IEnumerable<MigrationLogEntry> FromNoteMessage(string value)
        {
            var lines = value.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                    continue;

                yield return FromString(line);
            }
        }

        public static MigrationLogEntry From(HistoryEntry historyEntry)
        {
            var result = new MigrationLogEntry
            {
                ChangesetId = historyEntry.ChangesetId,
                GitBranchName = historyEntry.Branch.GitBranchName,
                GitSha = historyEntry.GitSha,
                TfvcServerPath = historyEntry.Branch.TfvcServerPath,
                Action = historyEntry.ActionTag
            };

            return result;
        }

        private IEnumerable<string> GetParts()
        {
            yield return ChangesetId.ToString();
            yield return GitBranchName;
            yield return GitSha;
            yield return TfvcServerPath;
            yield return Action;
        }
    }
}