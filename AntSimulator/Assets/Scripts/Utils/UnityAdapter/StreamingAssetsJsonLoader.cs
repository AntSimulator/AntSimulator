using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Utils.Core;

namespace Utils.UnityAdapter
{
    public static class StreamingAssetsJsonLoader
    {
        public static async Task<TextLoadResult> LoadTextAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return TextLoadResult.Fail("fileName is null or empty.", string.Empty);
            }

            var path = Path.Combine(Application.streamingAssetsPath, fileName);

            if (path.Contains("://") || path.Contains("jar:"))
            {
                using var req = UnityWebRequest.Get(path);
                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    await Task.Yield();
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    return TextLoadResult.Fail(req.error, path);
                }

                return TextLoadResult.Ok(req.downloadHandler.text, path);
            }

            if (!File.Exists(path))
            {
                return TextLoadResult.Fail("File not found.", path);
            }

            var json = File.ReadAllText(path);
            return TextLoadResult.Ok(json, path);
        }
    }
}
