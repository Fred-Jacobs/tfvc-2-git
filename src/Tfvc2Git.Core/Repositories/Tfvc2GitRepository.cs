using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using Serilog;
using Tfvc2Git.Core.Infrastructure;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.Repositories
{
    public sealed partial class Tfvc2GitRepository
    {
        #region Statics & Constants
        public const string Namespace = "tfvc-2-git";
        public const string MigrationEmail = "tfvc-2-git@noreply.com";
        public const string MigrationBranchName = "migration/tfvc-2-git";
        public const string InitMessage = "tfvc-2-git.init";
        public const string LogNoteMessage = "tfvc-2-git.notes";
        public const string GitIgnoreFileName = ".gitignore";
        public const string Tfvc2GitFileName = ".tfvc-2-git";
        #endregion

        #region Properties
        public static DateTimeOffset MigrationDateTimeOffset { get; } = new DateTimeOffset(2000, 01, 01, 0, 0, 42, TimeSpan.Zero);
        public Branch MigrationBranch => Git.Branches[MigrationBranchName];
        public Commit InitialCommit => Git.Lookup<Commit>(InitialCommitSha);
        public Commit MigrationNoteCommit => Git.Lookup<Commit>(MigrationNotesCommitSha);
        public Note MigrationNote => MigrationNoteCommit.Notes.SingleOrDefault(x => x.Namespace.Equals(Namespace));
        public string InitialCommitSha { get; private set; }
        public string MigrationNotesCommitSha { get; private set; }
        public Tfvc2GitConfig Config { get; }
        public TfvcRepository Tfvc { get; }
        public IRepository Git { get; private set; }
        public IReadOnlyDictionary<string, string> ExtraFiles => _extraFiles;

        public CommitOptions CommitOptions { get; set; } = new CommitOptions
        {
            AllowEmptyCommit = false,
            AmendPreviousCommit = false,
            PrettifyMessage = false
            //CommentaryChar = 
        };

        public CommitOptions CommitOptionsAllowEmpty { get; set; } = new CommitOptions
        {
            AllowEmptyCommit = true,
            AmendPreviousCommit = false,
            PrettifyMessage = false
            //CommentaryChar = 
        };

        public CommitOptions CommitOptionsAmend { get; set; } = new CommitOptions
        {
            AllowEmptyCommit = false,
            AmendPreviousCommit = true,
            PrettifyMessage = false
            //CommentaryChar = 
        };

        public FileSystem Io { get; }
        #endregion

        #region Fields
        private readonly Dictionary<string, string> _extraFiles = new Dictionary<string, string>();
        private readonly ILogger _log = Log.Logger.ForContext<Tfvc2GitRepository>();
        #endregion

        private Tfvc2GitRepository()
        {
        }

        public Tfvc2GitRepository(Tfvc2GitConfig config, FileSystem io, TfvcRepository tfvc)
        {
            Io = io;
            Config = config;
            Tfvc = tfvc;
        }

        public HistoryEntry Register(HistoryEntry historyEntry)
        {
            var note = MigrationNote;
            if (null == note)
                throw new ApplicationException("Migration note not found!");

            var notesAuthor = GetSignature(MigrationDateTimeOffset);
            var logEntry = MigrationLogEntry.From(historyEntry);

            if (note.Message.Equals(LogNoteMessage)) // first note
            {
                Git.Notes.Add(MigrationNoteCommit.Id, logEntry.ToString(), notesAuthor, notesAuthor, Namespace);
                historyEntry.Committed = true;
                return historyEntry;
            }

            var noteBuilder = new StringBuilder();
            noteBuilder.AppendLine(note.Message);
            noteBuilder.AppendLine(logEntry.ToString());
            var noteMessage = noteBuilder.ToString();

            Git.Notes.Add(MigrationNoteCommit.Id, noteMessage, notesAuthor, notesAuthor, Namespace);

            historyEntry.Committed = true;
            return historyEntry;
        }
    }
}