using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp;

namespace Tfvc2Git.Core.Repositories
{
    public sealed partial class Tfvc2GitRepository
    {
        public Tfvc2GitRepository Init()
        {
            if (null != Git)
                throw new ApplicationException($"{nameof(Tfvc2GitRepository)}.{nameof(Init)}() can only be called once!");

            RegisterExtraFiles();
            GitInit();
            EnsureTfvc2GitCommits();
            _log.Debug(" - Folder '{FolderPath}' contains a valid Tfvc2Git repository.", Config.GitLocalPath);

            return this;
        }

        private void RegisterExtraFiles()
        {
            var gitignoreContent = string.Empty;
            if (File.Exists(Config.GitIgnoreFilePath))
            {
                _log.Debug(" - .gitignore file found.");
                gitignoreContent = File.ReadAllText(Config.GitIgnoreFilePath);
            }
            else
            {
                _log.Warning(" - .gitignore file with path '{GitIgnoreFilePath}' not found!", Config.GitIgnoreFilePath);
            }

            _extraFiles.Add(GitIgnoreFileName, gitignoreContent);

            var tfvc2GitConfigBuilder = new StringBuilder();
            tfvc2GitConfigBuilder.AppendLine($"TfvcCollectionUri={Config.TfvcCollectionUri}");
            tfvc2GitConfigBuilder.AppendLine($"ChangesetIdFrom={Config.FromChangesetId}");
            tfvc2GitConfigBuilder.AppendLine($"ChangesetIdTo={Config.ToChangesetId}");
            foreach (var tfvc2GitBranchMap in Config.Branches)
            {
                tfvc2GitConfigBuilder.AppendLine($"Branch:{tfvc2GitBranchMap.GitBranchName}={tfvc2GitBranchMap.TfvcServerPath}");
            }

            _extraFiles.Add(Tfvc2GitFileName, tfvc2GitConfigBuilder.ToString());
        }

        private void GitInit()
        {
            if (!Directory.Exists(Config.GitLocalPath))
            {
                Directory.CreateDirectory(Config.GitLocalPath);
                _log.Debug(" - Created folder '{FolderPath}'", Config.GitLocalPath);
            }

            if (!Repository.IsValid(Config.GitLocalPath))
            {
                Repository.Init(Config.GitLocalPath, false);
                _log.Debug(" - Initialized a new git repository in folder '{FolderPath}'", Config.GitLocalPath);
            }

            Git = new Repository(Config.GitLocalPath);

            if (Git.Commits.Any())
            {
                return;
            }

            if (ExtraFiles.Any())
            {
                foreach (var extraFile in ExtraFiles)
                {
                    File.WriteAllText(Path.Combine(Config.GitLocalPath, extraFile.Key), extraFile.Value);
                    Git.Index.Add(extraFile.Key);
                }

                Git.Index.Write();
            }

            var signature = GetSignature(MigrationDateTimeOffset.AddMinutes(42));
            var initialCommit = Git.Commit(InitMessage, signature, signature, CommitOptions);
            InitialCommitSha = initialCommit.Sha;
            Git.Notes.Add(initialCommit.Id, ExtraFiles[Tfvc2GitFileName], signature, signature, Namespace);

            Git.CreateBranch(MigrationBranchName, initialCommit);
            Checkout(MigrationBranchName);

            const string fileName = "how-to-read-notes.md";
            var fileContent = $"git notes --ref {Namespace}";
            File.WriteAllText(Path.Combine(Config.GitLocalPath, fileName), fileContent);
            Git.Index.Add(fileName);
            Git.Index.Write();
            var notesCommit = Git.Commit(LogNoteMessage, signature, signature, CommitOptions);
            MigrationNotesCommitSha = notesCommit.Sha;
            signature = GetSignature(MigrationDateTimeOffset);
            Git.Notes.Add(notesCommit.Id, LogNoteMessage, signature, signature, Namespace);

            Checkout(MigrationBranchName);

            foreach (var gitBranch in Git.Branches)
            {
                if (gitBranch.FriendlyName.Equals(MigrationBranchName))
                    continue;

                Git.Branches.Remove(gitBranch);
            }

            _log.Debug(" - Git repository '{FolderPath}' converted to a Tfvc2Git repository.", Config.GitLocalPath);
        }

        private void EnsureTfvc2GitCommits()
        {
            var tfvc2GitCommits = MigrationBranch.Commits.ToArray();
            foreach (var commit in tfvc2GitCommits)
            {
                if (!commit.Message.StartsWith(Namespace))
                    continue;
                switch (commit.Message)
                {
                    case InitMessage:
                        InitialCommitSha = commit.Sha;
                        break;
                    case LogNoteMessage:
                        MigrationNotesCommitSha = commit.Sha;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(InitialCommitSha))
                throw new ApplicationException(
                    $"'{nameof(InitialCommitSha)}' not found! Repository doesn't look like a valid {Namespace} repository!");
            if (string.IsNullOrWhiteSpace(MigrationNotesCommitSha))
                throw new ApplicationException(
                    $"'{nameof(MigrationNotesCommitSha)}' not found! Repository doesn't look like a valid {Namespace} repository!");
        }
    }
}