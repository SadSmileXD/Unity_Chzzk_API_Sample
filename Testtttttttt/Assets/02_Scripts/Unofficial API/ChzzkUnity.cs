using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using WebSocketSharp; // WebSocketSharp 네임스페이스 사용

public class ChzzkUnity : MonoBehaviour
{
    #region Variables

    private const string WS_URL = "wss://kr-ss3.chat.naver.com/chat";
    private const string HEARTBEAT_REQUEST = "{\"ver\":\"2\",\"cmd\":0}";
    private const string HEARTBEAT_RESPONSE = "{\"ver\":\"2\",\"cmd\":10000}";

    string cid;
    string token;
    public string channel;

    // WebSocketSharp.WebSocket으로 명시적 타입 지정
    WebSocketSharp.WebSocket socket = null;

    float timer = 0f;
    bool running = false;

    #region Callbacks

    public UnityEvent<Profile, string> onMessage = new();
    public UnityEvent<Profile, string, DonationExtras> onDonation = new();
    public UnityEvent<Profile, SubscriptionExtras> onSubscription = new();
    public UnityEvent onClose = new();
    public UnityEvent onOpen = new();

    #endregion Callbacks

    #endregion Variables

    int closedCount = 0;
    bool reOpenTrying = false;
    //private string channelId;

    //public ChzzkUnity(string channelId)
    //{
    //    this.channelId = channelId;
    //}

    #region Unity Methods

    void Start()
    {
        onMessage.AddListener(DebugMessage);
        onDonation.AddListener(DebugDonation);
        onSubscription.AddListener(DebugSubscription);
    }

    private void Update()
    {
        if (closedCount > 0)
        {
            onClose?.Invoke();
            if (!reOpenTrying)
                StartCoroutine(TryReOpen());
            closedCount--;
        }
    }

    public IEnumerator TryReOpen()
    {
        reOpenTrying = true;
        yield return new WaitForSeconds(1);
        if (socket != null && !socket.IsAlive)
        {
            socket.Connect();
        }
        reOpenTrying = false;
    }

    void FixedUpdate()
    {
        if (running)
        {
            timer += Time.unscaledDeltaTime;
            if (timer > 15)
            {
                socket.Send(HEARTBEAT_REQUEST);
                timer = 0;
            }
        }
    }

    private void OnDestroy()
    {
        StopListening();
    }

    #endregion Unity Methods

    #region Debug Methods

    private void DebugMessage(Profile profile, string str) => Debug.Log($"| [Message] {profile.nickname} - {str}");
    private void DebugDonation(Profile profile, string str, DonationExtras donation) => Debug.Log(donation.isAnonymous ? $"| [Donation] 익명 - {str}" : $"| [Donation] {profile.nickname} - {str}");
    private void DebugSubscription(Profile profile, SubscriptionExtras subscription) => Debug.Log($"| [Subscription] {profile.nickname} - {subscription.month}");

    #endregion Debug Methods

    #region Public Methods

    public async UniTask<ChannelInfo> GetChannelInfo(string channelId)
    {
        var url = $"https://api.chzzk.naver.com/service/v1/channels/{channelId}";
        var request = UnityWebRequest.Get(url);
        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
            return JsonUtility.FromJson<ChannelInfo>(request.downloadHandler.text);
        return null;
    }

    public async UniTask<LiveStatus> GetLiveStatus(string channelId)
    {
        var url = $"https://api.chzzk.naver.com/polling/v3.1/channels/{channelId}/live-status";
        var request = UnityWebRequest.Get(url);
        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
            return JsonUtility.FromJson<LiveStatus>(request.downloadHandler.text);
        return null;
    }

    public async UniTask<AccessTokenResult> GetAccessToken(string cid)
    {
        var url = $"https://comm-api.game.naver.com/nng_main/v1/chats/access-token?channelId={cid}&chatType=STREAMING";
        var request = UnityWebRequest.Get(url);
        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
            return JsonUtility.FromJson<AccessTokenResult>(request.downloadHandler.text);
        return null;
    }

    public async UniTask Connect()
    {
        if (socket != null && socket.IsAlive)
        {
            socket.Close();
            socket = null;
        }

        LiveStatus liveStatus = await GetLiveStatus(channel);
        cid = liveStatus.content.chatChannelId;
        AccessTokenResult accessTokenResult = await GetAccessToken(cid);
        token = accessTokenResult.content.accessToken;

        // WebSocketSharp.WebSocket 인스턴스 생성 (SSL 설정은 라이브러리가 wss 프로토콜 자동 인식)
        socket = new WebSocketSharp.WebSocket(WS_URL);

        socket.OnMessage += ParseMessage;
        socket.OnClose += CloseConnect;
        socket.OnOpen += StartChat;

        socket.Connect();
    }

    public void Connect(string channelId)
    {
        channel = channelId;
        Connect().Forget();
    }

    public void StopListening()
    {
        if (socket != null)
        {
            socket.Close();
            socket = null;
        }
    }

    #endregion Public Methods

    #region Socket Event Handlers

    private void ParseMessage(object sender, MessageEventArgs e)
    {
        //Debug.Log($"RAW : {e.Data}");
        try
        {
            IDictionary<string, object> data = JsonConvert.DeserializeObject<IDictionary<string, object>>(e.Data);
            switch ((long)data["cmd"])
            { 
              
                case 0:
                    socket.Send(HEARTBEAT_RESPONSE);
                    timer = 0;
                    break;
                case 93101:
                    JArray body = (JArray)data["bdy"];
                    foreach (JToken jToken in body)
                    {
                        JObject bodyObject = (JObject)jToken;
                        string profileText = bodyObject["profile"]?.ToString();
                        if (profileText != null)
                        {
                            var profile = JsonUtility.FromJson<Profile>(profileText.Replace("\\", ""));
                            onMessage?.Invoke(profile, bodyObject["msg"]?.ToString().Trim());
                        }
                    }
                    break;
                case 93102:
                    {
                        Debug.Log("후원도네이션 메시지 수신");

                        JArray donationBody = (JArray)data["bdy"];

                        foreach (JToken token in donationBody)
                        {
                            JObject bodyObject = (JObject)token;

                            // profile
                            Profile profile = null;
                            string profileText = bodyObject["profile"]?.ToString();

                            if (!string.IsNullOrEmpty(profileText))
                            {
                                profileText = profileText.Replace("\\", "");

                                if (profileText.StartsWith("\""))
                                    profileText = profileText.Trim('"');

                                profile = JsonUtility.FromJson<Profile>(profileText);
                            }

                            // extras
                            DonationExtras extras = null;
                            string extrasText = bodyObject["extras"]?.ToString();

                            if (!string.IsNullOrEmpty(extrasText))
                            {
                                extras = JsonConvert.DeserializeObject<DonationExtras>(extrasText);
                            }

                            string message = bodyObject["msg"]?.ToString();

                            Debug.Log(
                                $"후원자 : {extras?.nickname}\n" +
                                $"금액 : {extras?.payAmount}\n" +
                                $"메시지 : {message}"
                            );

                            onDonation?.Invoke(profile, message, extras);
                        }
                    }
                    break;
                case 10100:
                    onOpen?.Invoke();
                    break;
            }
        }
        catch (Exception er) { Debug.LogError(er.ToString()); }
    }

    private void CloseConnect(object sender, CloseEventArgs e)
    {
        Debug.LogError("연결이 해제되었습니다");
        closedCount += 1;
    }

    private void StartChat(object sender, EventArgs e)
    {
        var message = $"{{\"ver\":\"2\",\"cmd\":100,\"svcid\":\"game\",\"cid\":\"{cid}\",\"bdy\":{{\"uid\":null,\"devType\":2001,\"accTkn\":\"{token}\",\"auth\":\"READ\"}},\"tid\":1}}";
        timer = 0;
        running = true;
        socket.Send(message);
    }

    #endregion Socket Event Handlers

    #region Sub-classes
    // ... (기존 클래스들 유지)
    [Serializable] 
    public class LiveStatus 
    {
        public int code; 
        public Content content;
        [Serializable]
        public class Content
        {
            public string chatChannelId;
        }
    }
    [Serializable] public class AccessTokenResult { public int code; public Content content; [Serializable] public class Content { public string accessToken; } }
    [Serializable] public class Profile { public string nickname; }
    [Serializable] public class SubscriptionExtras { public int month; }
    [Serializable]
    public class DonationExtras
    {
        public string donationId;
        public string donationType;
        public bool isAnonymous;
        public string payType;
        public int payAmount;
        public string nickname;
        public int continuousDonationDays;
        public string chatType;
        public string streamingChannelId;
    }
    [Serializable] public class ChannelInfo { public int code; public Content content; [Serializable] public class Content { public string channelId; } }
    #endregion Sub-classes
}


namespace  data
{


#region 네이버 치지직 데이터 파싱용 구조체 모음
[System.Serializable]
public class LiveStatus
{
    public int code;
    public string message;
    public LiveStatusContent content;
}

[Serializable]
public class LiveStatusContent
{
    public string liveTitle;
    public string status;
    public string chatChannelId;
}

[Serializable]
public class ChannelInfo
{
    public int code;
    public string message;
}

[System.Serializable]
public class AccessTokenResult
{
    public int code;
    public string message;
    public AccessTokenContent content;
}

[System.Serializable]
public class AccessTokenContent
{
    public string accessToken;
}

[System.Serializable]
public class Profile
{
    public string nickname;
    public string profileImageUrl;
}

    [Serializable]
    public class DonationExtras
    {
        public bool isAnonymous;
        public int payAmount; // 필드 추가
    }

    [System.Serializable]
public class SubscriptionExtras
{
    public int month;
}
    #endregion
}