using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace sadSmile
{
    [Serializable]
    public class AuthRequest
    {
        public string grantType;
        public string clientId;
        public string clientSecret;
        public string code;
        public string state;
    }

    [Serializable]
    public class RefreshRequest
    {
        public string grantType;
        public string refreshToken;
        public string clientId;
        public string clientSecret;
    }

    [Serializable]
    public class TokenData
    {
        public string accessToken;
        public string refreshToken;
        public string tokenType;
        public int expiresIn;
        public string scope;
    }

    [Serializable]
    public class TokenResponse
    {
        public int code;
        public string message;
        public TokenData content;
    }

    public class ChzzkAuth : MonoBehaviour
    {
        public static ChzzkAuth instance;
        [Header("CHZZK")]
        [SerializeField] private string m_clientId;
        [SerializeField] private string m_clientSecret;
        public string clientId=>m_clientId;
        public string clientSecret=>m_clientSecret;

        private const string RedirectUri = "http://localhost:8080/callback";
        private const string TokenUrl = "https://openapi.chzzk.naver.com/auth/v1/token";

        private string savedState;
        private HttpListener listener;
        
        public   string Key;
        private async void Awake()
        {
            if(instance != null)
            {
                Destroy(this.gameObject);
                return;
            }
            instance = this;
             await InitializeAuth();
            DontDestroyOnLoad(this.gameObject);
            SceneManager.LoadScene("api");
        }

        public async Task<string> InitializeAuth()
        {
            string accessToken =
                PlayerPrefs.GetString("Chzzk_AccessTokenTest", "");

            if (string.IsNullOrEmpty(accessToken))
            {
                RunLoginFlow();
                return null;
            }

            if (await IsTokenValid(accessToken))
            {
                Debug.Log("[Auth] 저장된 토큰 사용");
                Key = accessToken;
                return accessToken;
            }

            Debug.Log("[Auth] 토큰 만료, 갱신 시도");
            return await RefreshAccessToken();
        }

        [ContextMenu("Login")]
        public async void RunLoginFlow()
        {
            await LoginAsync();
        }

        private async Task LoginAsync()
        {
            savedState = Guid.NewGuid().ToString("N");

            string loginUrl =
                $"https://chzzk.naver.com/account-interlock" +
                $"?clientId={m_clientId}" +
                $"&redirectUri={Uri.EscapeDataString(RedirectUri)}" +
                $"&responseType=code" +
                $"&state={savedState}";

            Application.OpenURL(loginUrl);

            var result = await WaitForCode();

            if (result.state != savedState)
            {
                Debug.LogError("[Auth] State 불일치");
                return;
            }
            Key = result.code;
         await RequestAccessToken(result.code, result.state);
        }

        private async Task<string> RequestAccessToken(
            string code,
            string state)
        {
            var request = new AuthRequest
            {
                grantType = "authorization_code",
                clientId = m_clientId,
                clientSecret = m_clientSecret,
                code = code,
                state = state
            };

            string json = JsonUtility.ToJson(request);
            Debug.Log("[Token Request]\n" + json);

            return await SendJsonPostRequest(TokenUrl, json);
        }

        private async Task<string> RefreshAccessToken()
        {
            string refreshToken =
                PlayerPrefs.GetString("Chzzk_RefreshTokenTest", "");

            if (string.IsNullOrEmpty(refreshToken))
            {
                RunLoginFlow();
                return null;
            }

            var request = new RefreshRequest
            {
                grantType = "refresh_token",
                refreshToken = refreshToken,
                clientId = m_clientId,
                clientSecret = m_clientSecret
            };

            string json = JsonUtility.ToJson(request);

            return await SendJsonPostRequest(TokenUrl, json);
        }

        private async Task<string> SendJsonPostRequest(
            string url,
            string json)
        {
            using (UnityWebRequest www =
                   new UnityWebRequest(url, "POST"))
            {
                byte[] body =
                    Encoding.UTF8.GetBytes(json);

                www.uploadHandler =
                    new UploadHandlerRaw(body);

                www.downloadHandler =
                    new DownloadHandlerBuffer();

                www.SetRequestHeader(
                    "Content-Type",
                    "application/json");

                var op = www.SendWebRequest();

                while (!op.isDone)
                    await Task.Yield();

                Debug.Log(
                    $"Response : {www.responseCode}\n" +
                    $"{www.downloadHandler.text}");

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(
                        $"[Auth] 실패 : {www.error}\n" +
                        $"{www.downloadHandler.text}");
                    return null;
                }

                TokenResponse response =
                    JsonUtility.FromJson<TokenResponse>(
                        www.downloadHandler.text);

                if (response?.content == null)
                {
                    Debug.LogError("[Auth] 응답 파싱 실패");
                    return null;
                }

                SaveTokens(
                    response.content.accessToken,
                    response.content.refreshToken);
                Key = response.content.accessToken;
                Debug.Log("[Auth] 로그인 성공");

                return response.content.accessToken;
            }
        }

        private async Task<bool> IsTokenValid(
            string accessToken)
        {
            using (UnityWebRequest www =
                   UnityWebRequest.Get(
                       "https://openapi.chzzk.naver.com/v1/user/channels"))
            {
                www.SetRequestHeader(
                    "Authorization",
                    "Bearer " + accessToken);

                var op = www.SendWebRequest();

                while (!op.isDone)
                    await Task.Yield();

                return www.responseCode == 200;
            }
        }

        private void SaveTokens(
            string accessToken,
            string refreshToken)
        {
            PlayerPrefs.SetString(
                "Chzzk_AccessTokenTest",
                accessToken);

            PlayerPrefs.SetString(
                "Chzzk_RefreshTokenTest",
                refreshToken);

            PlayerPrefs.Save();

            Debug.Log("[Auth] 토큰 저장 완료");
        }

        private async Task<(string code, string state)>
            WaitForCode()
        {
            listener = new HttpListener();

            listener.Prefixes.Add(
                "http://localhost:8080/");

            listener.Start();

            var context =
                await listener.GetContextAsync();

            string code =
                context.Request.QueryString["code"];

            string state =
                context.Request.QueryString["state"];

            byte[] buffer =
                Encoding.UTF8.GetBytes(
                    "<html><body><h1>로그인 성공! 창을 닫으세요.</h1></body></html>");

            context.Response.OutputStream.Write(
                buffer,
                0,
                buffer.Length);

            context.Response.Close();
            listener.Stop();

            return (code, state);
        }
    }
}