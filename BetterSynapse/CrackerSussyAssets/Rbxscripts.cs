using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SynapseX.CrackerSussyAssets
{
    public struct ScriptObject
    {
        public int Id;
        public string Title;
        public string Script;
        public string Date;
        public string Tags;
        public string Thumbnail;
    }

    public static class Rbxscripts
    {
        private const string URL = "https://rbxscripts.xyz/wp-json/wl/v1/posts/";
        private static readonly WebClient wc = new();

        public static ScriptObject[] FetchScripts()
        {
            var json = wc.DownloadString(URL);
            var arrays = JArray.Parse(json);

            return arrays.Values<JObject>()
                .Select(obj => new ScriptObject
                {
                    Id = obj.GetValue("id").ToObject<int>(),
                    Title = obj.GetValue("title").ToObject<string>(),
                    Script = obj.GetValue("excerpt").ToObject<string>(),
                    Tags = obj.GetValue("slug").ToObject<string>(),
                    Date = obj.GetValue("date").ToObject<string>(),
                    Thumbnail = obj.GetValue("featured_image").ToObject<JObject>().GetValue("thumbnail").ToObject<string>()
                }).ToArray();
        }

        public static async Task<ScriptObject[]> FetchScriptsAsync()
        {
            var json = await wc.DownloadStringTaskAsync(URL);
            var arrays = JArray.Parse(json);

            return arrays.Values<JObject>()
                .Select(obj => new ScriptObject
                {
                    Id = obj.GetValue("id").ToObject<int>(),
                    Title = obj.GetValue("title").ToObject<string>(),
                    Script = obj.GetValue("excerpt").ToObject<string>(),
                    Tags = obj.GetValue("slug").ToObject<string>(),
                    Date = obj.GetValue("date").ToObject<string>(),
                    Thumbnail = obj.GetValue("featured_image").ToObject<JObject>().GetValue("thumbnail").ToObject<string>()
                }).ToArray();
        }
    }
}
