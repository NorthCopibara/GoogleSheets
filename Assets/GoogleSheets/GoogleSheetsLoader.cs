using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace GoogleSheets.Loader
{
    public static class GoogleSheetsLoader
    {
        private static readonly List<string> _csvBuffer = new List<string>(32);
        private static readonly Regex CsvMultilineFixRegex = new Regex("\"([^\"]|\"\"|\\n)*\"");
        private static readonly Regex CsvParseRegex = new Regex("(?<=^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)");

        private static string ConvertToDictJson(string data)
        {
            var sb = new StringBuilder(data.Length * 2);
            var list = CsvToDict(data);
            if (list.Count < 2)
            {
                throw new Exception(
                    "Invalid header data: first line should contains field names, second line - pair of wrapping chars.");
            }

            sb.Append("{");
            var it = list.GetEnumerator();

            // header.
            it.MoveNext();
            var headerKey = it.Current.Key;
            var headerValue = it.Current.Value;

            // wrappers.
            it.MoveNext();
            var wrapperKey = it.Current.Key;
            var wrapperValue = it.Current.Value;

            if (wrapperKey != "\"\"")
            {
                throw new Exception($"Invalid wrapper data for \"{headerKey}\" field: it should be wrapped with \"\".");
            }

            for (var i = 0; i < wrapperValue.Length; i++)
            {
                if (!(
                    wrapperValue[i] == string.Empty ||
                    wrapperValue[i] == "[]" ||
                    wrapperValue[i] == "{}" ||
                    string.Compare(wrapperValue[i], "IGNORE", true) == 0 ||
                    wrapperValue[i] == "\"\""))
                {
                    throw new Exception(string.Format("Invalid wrapper data for \"{0}\" field.", headerValue[i]));
                }
            }

            var needObjectsComma = false;
            string itemValue;
            string wrapChars;
            while (it.MoveNext())
            {
                sb.AppendFormat("{0}\"{1}\":{{", needObjectsComma ? "," : string.Empty, it.Current.Key);
                var needFieldsComma = false;
                for (var i = 0; i < headerValue.Length; i++)
                {
                    wrapChars = wrapperValue[i];
                    if (string.Compare(wrapChars, "IGNORE", true) == 0)
                    {
                        continue;
                    }

                    itemValue = wrapChars.Length > 0
                        ? string.Format("{0}{1}{2}", wrapChars[0], it.Current.Value[i], wrapChars[1])
                        : it.Current.Value[i];
                    sb.AppendFormat("{0}\"{1}\":{2}", needFieldsComma ? "," : string.Empty, headerValue[i], itemValue);
                    needFieldsComma = true;
                }

                sb.Append("}");
                needObjectsComma = true;
            }

            sb.Append("}");
            return sb.ToString();
        }

        private static string ConvertToArrayJson(string data)
        {
            var sb = new StringBuilder(data.Length * 2);
            var list = CsvToArray(data);
            if (list.Count < 2)
            {
                throw new Exception(
                    "Invalid header data: first line should contains field names, second line - pair of wrapping chars.");
            }

            sb.Append("[");
            var it = list.GetEnumerator();

            // header.
            it.MoveNext();
            var headerValue = it.Current;

            // wrappers.
            it.MoveNext();
            var wrapperValue = it.Current;
            for (var i = 0; i < wrapperValue.Length; i++)
            {
                if (!(
                    wrapperValue[i] == string.Empty ||
                    wrapperValue[i] == "[]" ||
                    wrapperValue[i] == "{}" ||
                    string.Compare(wrapperValue[i], "IGNORE", true) == 0 ||
                    wrapperValue[i] == "\"\""))
                {
                    throw new Exception($"Invalid wrapper data for \"{headerValue[i]}\" field.");
                }
            }

            var needObjectsComma = false;
            while (it.MoveNext())
            {
                sb.AppendFormat("{0}{{", needObjectsComma ? "," : string.Empty);
                var needFieldsComma = false;
                for (var i = 0; i < headerValue.Length; i++)
                {
                    var wrapChars = wrapperValue[i];
                    if (string.Compare(wrapChars, "IGNORE", true) == 0)
                    {
                        continue;
                    }

                    var itemValue = wrapChars.Length > 0
                        ? $"{wrapChars[0]}{it.Current[i]}{wrapChars[1]}"
                        : it.Current[i];
                    sb.AppendFormat("{0}\"{1}\":{2}", needFieldsComma ? "," : string.Empty, headerValue[i], itemValue);
                    needFieldsComma = true;
                }

                sb.Append("}");
                needObjectsComma = true;
            }

            sb.Append("]");
            return sb.ToString();
        }

        private static void ParseCsvLine(string data)
        {
            _csvBuffer.Clear();
            foreach (Match m in CsvParseRegex.Matches(data))
            {
                var part = m.Value.Trim();
                if (part.Length > 0)
                {
                    if (part[0] == '"' && part[part.Length - 1] == '"')
                    {
                        part = part.Substring(1, part.Length - 2);
                    }

                    part = part.Replace("\"\"", "\"");
                }

                _csvBuffer.Add(part);
            }
        }

        private static Dictionary<string, string[]> CsvToDict(string data)
        {
            var list = new Dictionary<string, string[]>();
            var headerLen = -1;
            using (var reader = new StringReader(data))
            {
                while (reader.Peek() != -1)
                {
                    ParseCsvLine(reader.ReadLine());
                    if (_csvBuffer.Count <= 0 || string.IsNullOrEmpty(_csvBuffer[0])) continue;
                    if (headerLen == -1)
                    {
                        headerLen = _csvBuffer.Count;
                    }

                    if (_csvBuffer.Count != headerLen)
                    {
                        Debug.LogWarning("Invalid csv line, skipping.");
                        continue;
                    }

                    var key = _csvBuffer[0];
                    _csvBuffer.RemoveAt(0);
                    list[key] = _csvBuffer.ToArray();
                }
            }

            return list;
        }

        private static List<string[]> CsvToArray(string data)
        {
            var list = new List<string[]>();
            var headerLen = -1;
            using (var reader = new StringReader(data))
            {
                while (reader.Peek() != -1)
                {
                    ParseCsvLine(reader.ReadLine());
                    if (_csvBuffer.Count <= 0) continue;
                    if (headerLen == -1)
                    {
                        headerLen = _csvBuffer.Count;
                    }

                    if (_csvBuffer.Count != headerLen)
                    {
                        Debug.LogWarning("Invalid csv line, skipping.");
                        continue;
                    }

                    list.Add(_csvBuffer.ToArray());
                }
            }

            return list;
        }

        public static async Task<string> Process(string url, JsonMode jsonMode)
        {
            try
            {
                if (url == null)
                {
                    throw new NullReferenceException("URL is null");
                }

                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                var www = new WebClient {Encoding = Encoding.UTF8};
                string data;
                if (string.IsNullOrEmpty(url)) return null;

                try
                {
                    var newUrl = url.Replace("?", string.Empty).Replace("/edit", "/export?format=csv&");
                    data = await www.DownloadStringTaskAsync(newUrl);
                }
                catch (Exception urlEx)
                {
                    throw new Exception($"\"{url}\": {urlEx.Message}");
                }

                // fix for multiline string.
                data = CsvMultilineFixRegex.Replace(data, m => m.Value.Replace("\n", "\\n"));
                // json generation.
                switch (jsonMode)
                {
                    case JsonMode.Array:
                        data = ConvertToArrayJson(data);
                        break;
                    case JsonMode.Dictionary:
                        data = ConvertToDictJson(data);
                        break;
                    case JsonMode.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return data;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = null;
            }
        }
    }

    public enum JsonMode
    {
        None = 0,
        Array = 1,
        Dictionary = 2
    }
}