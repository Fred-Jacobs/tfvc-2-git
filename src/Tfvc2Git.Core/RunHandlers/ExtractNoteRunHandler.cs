using System;
using System.IO;
using System.Linq;
using System.Text;
using Tfvc2Git.Core.Configuration;
using Tfvc2Git.Core.Configuration.Options;
using Tfvc2Git.Core.Models;
using Tfvc2Git.Core.Repositories;
using Tfvc2Git.Core.RunHandlers.Base;

namespace Tfvc2Git.Core.RunHandlers
{
    public sealed class ExtractNoteRunHandler : RunHandlerBase<ExtractNoteOptions>
    {
        #region Fields
        private readonly Tfvc2GitRepository _repository;
        #endregion

        public ExtractNoteRunHandler(Tfvc2GitRepository repository, ConfigBuilder configBuilder, Tfvc2GitConfig config)
            : base(configBuilder, config)
        {
            _repository = repository;
        }

        protected override void PreProcess()
        {
            base.PreProcess();
            var notCommitted = Config.History.Where(x => !x.Committed).ToArray();
            if (!notCommitted.Any())
                return;

            Log.Error(" - Repository is not up to date : {notCommittedCount}/{total}", notCommitted.Length, Config.History.Count);
            Log.Error(" - Update repository!");
            Environment.Exit(1);
        }

        protected override void Process()
        {
            var filePath = Options.FilePath;
            var noteMessage = _repository.MigrationNote.Message.Split(Environment.NewLine.ToCharArray());
            var sb = new StringBuilder();
            foreach (var s in noteMessage)
            {
                if (string.IsNullOrWhiteSpace(s))
                    continue;

                sb.AppendLine(s);
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.WriteAllText(filePath, sb.ToString());
        }
    }
}