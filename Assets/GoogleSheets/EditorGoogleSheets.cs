using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace GoogleSheets.Editor
{
    public class EditorGoogleSheets : EditorWindow
    {
        private const string Title = "GoogleDocs Downloader";
        private static string _url = "";

        [MenuItem("Tools/GoogleSheets")]
        private static void OpenEditorWindow()
        {
            var win = GetWindow<EditorGoogleSheets>(true);
            var pos = win.position;
            pos.width = 600f;
            pos.height = 300f;
            win.position = pos;
            win.titleContent.text = Title;
        }

        private void OnGUI()
        {
            _url = EditorGUILayout.TextField("External Url path:", _url).Trim();

            if (GUILayout.Button("Test", GUILayout.Height(30f)))
            {
                ReadDictionary(_url);
                ReadArray(_url);
            }

            if (GUILayout.Button("TestById", GUILayout.Height(30f)))
            {
                ReadById();
            }
        }

        private static async void ReadById()
        {
            var data = await GoogleSheetsManager.ReadById<TestGoogleSheets>(GoogleSheetsId.Enemy);
            data.ForEach(x => Debug.Log(x.Damage));
        }

        private static async void ReadDictionary(string url)
        {
            var data = await GoogleSheetsManager.ReadDictionary<TestGoogleSheets>(url);
            data.Values.ToList().ForEach(x => Debug.Log(x.Damage));
            data.Keys.ToList().ForEach(Debug.Log);
        }

        private static async void ReadArray(string url)
        {
            var data = await GoogleSheetsManager.ReadArray<TestGoogleSheets>(url);
            data.ToList().ForEach(x => Debug.Log(x.Damage));
        }
    }

    [System.Serializable]
    public class TestGoogleSheets
    {
        [JsonProperty("Helth")] public int Helth;

        [JsonProperty("Damage")] public int Damage;

        [JsonProperty("Speed")] public float Speed;
    }
}