using System.Diagnostics;
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
    public sealed class CollectTfvcAuthorsRunHandler : RunHandlerBase<CollectTfvcAuthorsOptions>
    {
        #region Fields
        private readonly Tfvc2GitRepository _repository;
        #endregion

        public CollectTfvcAuthorsRunHandler(Tfvc2GitRepository repository, ConfigBuilder configBuilder, Tfvc2GitConfig config)
            : base(configBuilder, config)
        {
            _repository = repository;
        }

        protected override void Process()
        {
            var allHistory = _repository.Tfvc.GetHistory("$/");

            var current = 0;
            var total = allHistory.Length;
            foreach (var changeset in allHistory)
            {
                current++;
                var author = _repository.Tfvc.GetAuthor(changeset);
                Log.Information(" ({current}/{total}) : {ChangesetId} : {Author}", current, total, changeset.ChangesetId, author.ToString());
            }

            var sb = new StringBuilder();
            foreach (var author in _repository.Tfvc.Authors.Authors.OrderBy(x => x.Key))
            {
                sb.AppendLine(author.Value.ToString());
            }

            var content = sb.ToString();
            File.WriteAllText(Options.FilePath, content);

            Debugger.Break();
        }
    }
}