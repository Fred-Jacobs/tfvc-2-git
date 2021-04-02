using System;
using LibGit2Sharp;
using Tfvc2Git.Core.Models;

namespace Tfvc2Git.Core.Repositories
{
    public sealed partial class Tfvc2GitRepository
    {
        public Signature GetSignature(DateTimeOffset when) => new Signature(Namespace, MigrationEmail, when);

        public Signature GetSignature(Author author, DateTimeOffset when) => new Signature(author.DisplayName, author.Email, when);
    }
}