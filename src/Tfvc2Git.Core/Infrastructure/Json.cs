using Newtonsoft.Json;

namespace Tfvc2Git.Core.Infrastructure
{
    public sealed class Json
    {
        #region Properties
        public JsonSerializerSettings Settings { get; } = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
        };
        #endregion

        public T FromString<T>(string content) where T : class => JsonConvert.DeserializeObject<T>(content, Settings);
        public string AsString<T>(T content) where T : class => JsonConvert.SerializeObject(content, Settings);
    }
}