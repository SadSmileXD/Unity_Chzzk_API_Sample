using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
 

// --- 데이터 모델 ---
[Serializable] public class ChzzkResponse { public int code; public string message; public TokenData content; }
[Serializable] public class TokenData { public string accessToken; public string refreshToken; public string tokenType; public int expiresIn; public string scope; }
[Serializable] public class TokenRequest { public string grantType; public string clientId; public string clientSecret; public string code; public string state; public string refreshToken; }

// --- 라이브 목록 응답 모델 ---
[Serializable] public class LiveListResponse { public LiveDataContent content; }
[Serializable] public class LiveDataContent { public PageInfo page; public LiveData[] data; }
[Serializable] public class PageInfo { public string next; }
[Serializable] public class LiveData { public string channelId; public string channelName; public string liveTitle; }

public class ChzzkAuthManager : MonoBehaviour
{
    [Header("CHZZK Settings")]
    [SerializeField] private string clientId;
    [SerializeField] private string clientSecret;
    public string targetChannelId; // 인스펙터에 방송인 채널 ID 입력

    private const string RedirectUri = "http://localhost:8080/callback";
    private const string TokenUrl = "https://openapi.chzzk.naver.com/auth/v1/token";
    [SerializeField] private AudioSource m_as;

   
    [SerializeField] private Button submitButton;       // 확인 버튼
    [SerializeField] private Button sitebtn;
    private void Awake()
    {
        sitebtn.onClick.AddListener(Gochzzk);
    }
    void Start()
    {
        submitButton.onClick.AddListener(OnSubmitCode);
        //string savedRefresh = PlayerPrefs.GetString("Chzzk_RefreshToken", "");
        //if (!string.IsNullOrEmpty(savedRefresh))
        //{
        //    StartCoroutine(RefreshAccessToken(savedRefresh));
        //}
      
    }
    private void OnSubmitCode()
    {
     
        var text = PlayerPrefs.GetString("Chzzk_RefreshToken", "");
        if (text == "")
        {
            Login();
            submitButton.gameObject.SetActive(false);
            return;
        }
        string savedRefresh = PlayerPrefs.GetString("Chzzk_RefreshToken", "");
        if (!string.IsNullOrEmpty(savedRefresh))
        {
            StartCoroutine(RefreshAccessToken(savedRefresh));
        }
        string code = text;
        if (!string.IsNullOrEmpty(code))
        {
            StartCoroutine(GetToken(code, ""));
        }
        submitButton.gameObject.SetActive(false);
    }
    [ContextMenu("Login")]
    public async void Login()
    {
        var server = new OAuthServer();
        string state = Guid.NewGuid().ToString("N");
        string loginUrl = $"https://chzzk.naver.com/account-interlock?clientId={clientId}&redirectUri={Uri.EscapeDataString(RedirectUri)}&responseType=code&state={state}";
        Application.OpenURL(loginUrl);
        var result = await server.WaitForCode();
        StartCoroutine(GetToken(result.code, result.state));
    }

    private IEnumerator GetToken(string code, string state)
    {
        string json = JsonUtility.ToJson(new TokenRequest { grantType = "authorization_code", clientId = clientId, clientSecret = clientSecret, code = code, state = state });
        yield return RequestToken(json);
    }

    private IEnumerator RefreshAccessToken(string refreshToken)
    {
        string json = JsonUtility.ToJson(new TokenRequest { grantType = "refresh_token", refreshToken = refreshToken, clientId = clientId, clientSecret = clientSecret });
        yield return RequestToken(json);
    }

    private IEnumerator RequestToken(string jsonData)
    {
        using (UnityWebRequest request = new UnityWebRequest(TokenUrl, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<ChzzkResponse>(request.downloadHandler.text);
                PlayerPrefs.SetString("Chzzk_RefreshToken", response.content.refreshToken);
                Debug.Log("[System] 모니터링 시작!");
                StartCoroutine(AutoCheckLiveRoutine());
            }
        }
    }

    private IEnumerator AutoCheckLiveRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(FetchAndCheckStreamer(targetChannelId));
            yield return new WaitForSeconds(30f); // 1분 주기
        }
    }

    private IEnumerator FetchAndCheckStreamer(string targetChannelId)
    {
        string nextToken = "";
        bool found = false;

        // 3페이지(60개)까지 조회
        for (int i = 0; i < 50; i++)
        {
            string url = "https://openapi.chzzk.naver.com/open/v1/lives?size=20" + (string.IsNullOrEmpty(nextToken) ? "" : $"&next={nextToken}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Client-Id", clientId);
                request.SetRequestHeader("Client-Secret", clientSecret);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    LiveListResponse response = JsonUtility.FromJson<LiveListResponse>(request.downloadHandler.text);
                    if (response.content?.data != null)
                    {
                        foreach (var live in response.content.data)
                        {
                            Debug.Log(
            $"채널명 : {live.channelName}\n" +
            $"채널ID : {live.channelId}\n" +
            $"방송제목 : {live.liveTitle}"
        );

                            if (live.channelId == targetChannelId)
                            {
                                Debug.Log($"[방송 중 확인!] {live.channelName}님이 방송 중입니다: {live.liveTitle}");
                                m_as.Play();
                                found = true;
                                sitebtn.gameObject.SetActive(true);
                                break;
                            }
                        }
                    }
                    if (found) break;
                    nextToken = response.content?.page?.next;
                    Debug.Log(request.downloadHandler.text);
                    Debug.Log($"next = {response.content?.page?.next}");
                    if (string.IsNullOrEmpty(nextToken)) break;
                }
            }
        }
        if (!found) Debug.Log("현재 상위 60개 방송 목록에 없습니다.");
    }
    private void Gochzzk()
    {
        Application.OpenURL("https://chzzk.naver.com/live/21e1510cc1cf976ed33fa35d48837495");
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
    public async Task<(string code, string state)> WaitForCode()
    {
        listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        var context = await listener.GetContextAsync();
        string code = context.Request.QueryString["code"];
        string state = context.Request.QueryString["state"];
        byte[] buffer = Encoding.UTF8.GetBytes("<html><body><h1>로그인 성공! 창을 닫으세요.</h1></body></html>");
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.Close();
        listener.Stop();
        return (code, state);
    }
}