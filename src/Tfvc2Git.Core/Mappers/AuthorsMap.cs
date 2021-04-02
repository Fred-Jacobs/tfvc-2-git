using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Tfvc2Git.Core.Configuration;
using Tfvc2Git.Core.Infrastructure;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.Mappers
{
    public sealed class AuthorsMap
    {
        #region Properties
        public IReadOnlyDictionary<string, Author> Authors => GetAuthors();
        #endregion

        #region Fields
        private readonly Dictionary<string, Author> _authors = new Dictionary<string, Author>(StringComparer.OrdinalIgnoreCase);
        private readonly Tfvc2GitConfig _config;
        private readonly bool _initialized = false;
        private readonly FileSystem _io;
        private readonly ILogger _log = Log.Logger.ForContext<AuthorsMap>();
        #endregion

        private AuthorsMap()
        {
        }

        public AuthorsMap(Tfvc2GitConfig config, FileSystem io)
        {
            _config = config;
            _io = io;
        }

        public Author Get(string name) => _authors.ContainsKey(name) ? _authors[name] : null;

        public Author Register(string changesetCommitter, string name, string email)
        {
            var author = new Author
            {
                TfvcName = changesetCommitter,
                DisplayName = name,
                Email = email
            };
            if (!Authors.ContainsKey(author.TfvcName))
                _authors.Add(author.TfvcName, author);

            return author;
        }

        private IReadOnlyDictionary<string, Author> GetAuthors()
        {
            if (!_initialized && !_authors.Any())
            {
                _log.Debug(" - Initialize {Type}", nameof(AuthorsMap));
                var lines = _io.ReadLines(_config.AuthorMapFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(_io.CsvSeparator);
                    var author = new Author
                    {
                        TfvcName = parts[0],
                        DisplayName = parts[1],
                        Email = parts[2]
                    };
                    _authors.Add(author.TfvcName, author);
                }
            }

            return _authors;
        }
    }
}