using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MCFight.BalanceLab
{
    /// <summary>
    /// DeepSeek / OpenAI 兼容 API 客户端。
    /// 使用 UnityWebRequest 发送 chat completions 请求。
    /// </summary>
    public class LLMClient
    {
        private string _apiKey;
        private string _apiUrl;

        public LLMClient(string apiKey, string apiUrl)
        {
            _apiKey = apiKey;
            _apiUrl = apiUrl.TrimEnd('/');
        }

        /// <summary>
        /// 发送 chat completion 请求，返回 LLM 文本。
        /// 必须在协程中调用。
        /// </summary>
        public IEnumerator SendChat(string systemPrompt, string userPrompt, Action<string> onSuccess, Action<string> onError)
        {
            string url = $"{_apiUrl}/v1/chat/completions";

            // 构建请求体（手动拼 JSON 以避免序列化问题）
            string requestBody = BuildRequestBody(systemPrompt, userPrompt);
            Debug.Log($"[LLM] Sending request to {url}, prompt length={userPrompt.Length}");

            using var req = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
            req.timeout = 60;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string responseJson = req.downloadHandler.text;
                Debug.Log($"[LLM] Response received, length={responseJson.Length}");
                string content = ParseResponseContent(responseJson);
                if (!string.IsNullOrEmpty(content))
                    onSuccess?.Invoke(content);
                else
                    onError?.Invoke($"Failed to parse response: {responseJson.Substring(0, Math.Min(200, responseJson.Length))}");
            }
            else
            {
                string err = req.error ?? "Unknown error";
                string body = req.downloadHandler?.text ?? "";
                Debug.LogError($"[LLM] Request failed: {err}\n{body}");
                onError?.Invoke($"{err}: {body}");
            }
        }

        string BuildRequestBody(string systemPrompt, string userPrompt)
        {
            // Escape JSON strings
            string sys = JsonEscape(systemPrompt);
            string usr = JsonEscape(userPrompt);

            return $@"{{
  ""model"": ""deepseek-chat"",
  ""messages"": [
    {{""role"": ""system"", ""content"": ""{sys}""}},
    {{""role"": ""user"", ""content"": ""{usr}""}}
  ],
  ""temperature"": 0.3,
  ""max_tokens"": 4096,
  ""response_format"": {{""type"": ""json_object""}}
}}";
        }

        string ParseResponseContent(string json)
        {
            // 手动提取 choices[0].message.content
            // 不依赖 JsonUtility 的限制
            string marker = "\"content\":\"";
            int idx = json.IndexOf(marker);
            if (idx < 0)
            {
                // 尝试单引号或其他格式
                marker = "\"content\": \"";
                idx = json.IndexOf(marker);
            }
            if (idx < 0) return null;

            idx += marker.Length;
            var sb = new StringBuilder();
            while (idx < json.Length)
            {
                char c = json[idx];
                if (c == '"' && idx > 0 && json[idx - 1] != '\\')
                    break;
                if (c == '\\' && idx + 1 < json.Length)
                {
                    char next = json[idx + 1];
                    if (next == 'n') sb.Append('\n');
                    else if (next == 't') sb.Append('\t');
                    else if (next == 'r') sb.Append('\r');
                    else if (next == '"') sb.Append('"');
                    else if (next == '\\') sb.Append('\\');
                    else if (next == '/') sb.Append('/');
                    else sb.Append(next);
                    idx += 2;
                    continue;
                }
                sb.Append(c);
                idx++;
            }
            return sb.ToString();
        }

        static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t");
        }
    }
}
