using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoogleSheets.Loader;
using Newtonsoft.Json;
using UnityEngine;

namespace GoogleSheets
{
    public static class GoogleSheetsManager
    {
        public static async Task<Dictionary<string, T>> ReadDictionary<T>(string url)
        {
            var json = await GoogleSheetsLoader.Process(url, JsonMode.Dictionary);
            var data = await Task.Run(() => JsonConvert.DeserializeObject<Dictionary<string, T>>(json));
            return data;
        }

        public static async Task<T[]> ReadArray<T>(string url)
        {
            var json = await GoogleSheetsLoader.Process(url, JsonMode.Array);
            var data = await Task.Run(() => JsonConvert.DeserializeObject<T[]>(json));
            return data;
        }

        public static async Task<List<T>> ReadById<T>(GoogleSheetsId id)
        {
            var sheetsToken = (SheetsTokens) Resources.Load("SheetsToken");
            if (sheetsToken == null)
            {
                throw new NullReferenceException("Sheets token not found!");
            }
            var url = sheetsToken.GetUrlById(id);
            var data = await ReadArray<T>(url);
            return data.ToList();
        }
    }

    [CreateAssetMenu(menuName = "GoogleSheets/Token", fileName = "SheetsToken")]
    public class SheetsTokens : ScriptableObject
    {
        [SerializeField] 
        private SheetsToken[] _tokens;

        public string GetUrlById(GoogleSheetsId id)
        {
            return _tokens.ToList().Find(x => x.Id == id).Url;
        }
    }

    [Serializable]
    public class SheetsToken
    {
        public GoogleSheetsId Id;
        public string Url;
    }
}