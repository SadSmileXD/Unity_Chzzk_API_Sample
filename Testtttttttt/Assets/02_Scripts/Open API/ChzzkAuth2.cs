 
using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

#region Auth Models
namespace testt
{


[Serializable]
public class ChzzkResponse
{
    public int code;
    public string message;
    public TokenData content;
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
public class TokenRequest
{
    public string grantType;
    public string clientId;
    public string clientSecret;
    public string code;
    public string state;
    public string refreshToken;
}

#endregion

public class ChzzkAuth2 : MonoBehaviour
{
    [Header("CHZZK")]
    [SerializeField] private string clientId;
    [SerializeField] private string clientSecret;

    [Header("방송인 채널 ID")]
    [SerializeField] private string targetChannelId;

    [SerializeField] private AudioSource m_as;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button sitebtn;

    private const string RedirectUri = "http://localhost:8080/callback";
    private const string TokenUrl = "https://openapi.chzzk.naver.com/auth/v1/token";

    private bool isLive = false;

    private void Awake()
    {
        sitebtn.onClick.AddListener(Gochzzk);
        sitebtn.gameObject.SetActive(false);
    }

    private void Start()
    {
        submitButton.onClick.AddListener(OnSubmitCode);
    }

    private void OnSubmitCode()
    {
        string refreshToken =
            PlayerPrefs.GetString("Chzzk_RefreshToken", "");

        if (string.IsNullOrEmpty(refreshToken))
        {
            Login();
            return;
        }

        StartCoroutine(RefreshAccessToken(refreshToken));
    }

    [ContextMenu("Login")]
    public async void Login()
    {
        var server = new OAuthServer();

        string state = Guid.NewGuid().ToString("N");

        string loginUrl =
            $"https://chzzk.naver.com/account-interlock?" +
            $"clientId={clientId}" +
            $"&redirectUri={Uri.EscapeDataString(RedirectUri)}" +
            $"&responseType=code" +
            $"&state={state}";

        Application.OpenURL(loginUrl);

        var result = await server.WaitForCode();

        StartCoroutine(GetToken(result.code, result.state));
    }

    private IEnumerator GetToken(string code, string state)
    {
        string json =
            JsonUtility.ToJson(new TokenRequest
            {
                grantType = "authorization_code",
                clientId = clientId,
                clientSecret = clientSecret,
                code = code,
                state = state
            });

        yield return RequestToken(json);
    }

    private IEnumerator RefreshAccessToken(string refreshToken)
    {
        string json =
            JsonUtility.ToJson(new TokenRequest
            {
                grantType = "refresh_token",
                refreshToken = refreshToken,
                clientId = clientId,
                clientSecret = clientSecret
            });

        yield return RequestToken(json);
    }

    private IEnumerator RequestToken(string jsonData)
    {
        using (UnityWebRequest request =
               new UnityWebRequest(TokenUrl, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(jsonData);

            request.uploadHandler =
                new UploadHandlerRaw(body);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json");

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                var response =
                    JsonUtility.FromJson<ChzzkResponse>(
                        request.downloadHandler.text);

                PlayerPrefs.SetString(
                    "Chzzk_RefreshToken",
                    response.content.refreshToken);

                Debug.Log("[System] 모니터링 시작");

                StartCoroutine(AutoCheckLiveRoutine());
            }
            else
            {
                Debug.LogError(request.error);
            }
        }
    }

    private IEnumerator AutoCheckLiveRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(
                CheckStreamerLive(targetChannelId));

            yield return new WaitForSeconds(30f);
        }
    }

    private IEnumerator CheckStreamerLive(string channelId)
    {
        string url =
            $"https://api.chzzk.naver.com/service/v3/channels/{channelId}/live-detail";

        using (UnityWebRequest request =
               UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"라이브 조회 실패 : {request.error}");

                yield break;
            }

            string json = request.downloadHandler.text;

            bool nowLive =
                json.Contains("\"status\":\"OPEN\"");

            if (nowLive)
            {
                if (!isLive)
                {
                    Debug.Log("방송 시작!");

                    if (!m_as.isPlaying)
                        m_as.Play();

                    sitebtn.gameObject.SetActive(true);
                }

                isLive = true;
            }
            else
            {
                if (isLive)
                {
                    Debug.Log("방송 종료");
                    sitebtn.gameObject.SetActive(false);
                }

                isLive = false;
            }
        }
    }

    private void Gochzzk()
    {
        Application.OpenURL(
            $"https://chzzk.naver.com/live/{targetChannelId}");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

public class OAuthServer
{
    private HttpListener listener;

    public async Task<(string code, string state)>
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

        context.Response.OutputStream
            .Write(buffer, 0, buffer.Length);

        context.Response.Close();

        listener.Stop();

        return (code, state);
    }
}
}