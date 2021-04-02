namespace Tfvc2Git.Core.Models
{
    public sealed class Author
    {
        #region Properties
        public string TfvcName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        #endregion

        public override string ToString() => $"{TfvcName};{DisplayName};{Email}";
    }
}