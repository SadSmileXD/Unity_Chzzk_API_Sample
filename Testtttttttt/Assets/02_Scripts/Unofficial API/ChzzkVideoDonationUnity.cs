using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using WebSocketSharp;

public class ChzzkVideoDonationUnity : MonoBehaviour
{
    #region Variables

    WebSocketSharp.WebSocket socket = null;

    float timer = 0f;
    bool running = false;

    private const string HEARTBEAT_REQUEST = "2";
    private const string HEARTBEAT_RESPONSE = "3";

    public UnityEvent<Profile, VideoDonation> onVideoDonationArrive = new();
    public UnityEvent<DonationControl> onVideoDonationControl = new();
    public UnityEvent onClose = new();
    public UnityEvent onOpen = new();

    #endregion Variables

    int closedCount = 0;
    bool reOpenTrying = false;

    #region Unity Methods

    private void Update()
    {
        if (closedCount > 0)
        {
            onClose?.Invoke();
            if (!reOpenTrying)
                StartCoroutine(TryReOpen());
            closedCount--;
        }
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

    private void OnDestroy() => StopListening();

    #endregion Unity Methods

    #region Public Methods

    // 1. 첫 진입점: URL을 받아 처리 시작 (이름 변경으로 모호성 해결)
    public void StartDonation(string url)
    {
        ConnectFlowAsync(url).Forget();
    }

    private async UniTask ConnectFlowAsync(string url)
    {
        string wssId = GetMissionWSSId(url);
        string sessionUrl = await GetSessionURL(wssId);
        string wssUrl = MakeWssURL(sessionUrl);

        await ConnectWebSocket(wssUrl);
    }

    // 2. 실제 웹소켓 연결 담당
    public async UniTask ConnectWebSocket(string wssUrl)
    {
        if (socket != null)
        {
            socket.Close();
            socket = null;
        }

        socket = new WebSocketSharp.WebSocket(wssUrl);

        socket.OnMessage += ParseMessage;
        socket.OnClose += CloseConnect;
        socket.OnOpen += onSocketOpen;

        socket.Connect();
        await UniTask.CompletedTask;
    }

    public string GetMissionWSSId(string url) => url.Split("@")[1];

    public async UniTask<string> GetSessionURL(string missionWSSId)
    {
        var url = $"https://api.chzzk.naver.com/manage/v1/alerts/video@{missionWSSId}/session-url";
        var request = UnityWebRequest.Get(url);
        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            var sessionUrl = JsonUtility.FromJson<SessionUrl>(request.downloadHandler.text);
            return sessionUrl?.content?.sessionUrl;
        }
        return null;
    }

    public string MakeWssURL(string sessionUrl)
    {
        string auth = sessionUrl.Split("auth=")[1];
        string server = sessionUrl.Split(".nchat")[0].Substring(12);
        return $"wss://ssio{server}.nchat.naver.com/socket.io/?auth={auth}&EIO=3&transport=websocket";
    }

    void onSocketOpen(object sender, EventArgs e)
    {
        timer = 0;
        running = true;
        socket.Send(HEARTBEAT_REQUEST);
        onOpen?.Invoke();
    }

    public void StopListening()
    {
        if (socket == null) return;
        socket.Close();
        socket = null;
    }

    #endregion Public Methods

    #region Socket Event Handlers

    private void ParseMessage(object sender, MessageEventArgs e)
    {
        if (e.Data == HEARTBEAT_REQUEST) { timer = 0; socket.Send(HEARTBEAT_RESPONSE); return; }
        if (e.Data == HEARTBEAT_RESPONSE || e.Data == "40") return;

        VideoDonationList donations = JsonUtility.FromJson<VideoDonationList>(e.Data);
        if (donations?.videoDonation == null || donations.videoDonation.Count == 0) return;

        switch (donations.videoDonation[0])
        {
            case "donation":
                for (int i = 1; i < donations.videoDonation.Count; i++)
                {
                    VideoDonation donationObject = JsonUtility.FromJson<VideoDonation>(donations.videoDonation[i]);
                    Profile profile = JsonUtility.FromJson<Profile>(donationObject.profile);
                    onVideoDonationArrive.Invoke(profile, donationObject);
                }
                break;
            case "donationControl":
                for (int i = 1; i < donations.videoDonation.Count; i++)
                {
                    DonationControl controlObject = JsonUtility.FromJson<DonationControl>(donations.videoDonation[i]);
                    onVideoDonationControl.Invoke(controlObject);
                }
                break;
        }
    }

    private void CloseConnect(object sender, CloseEventArgs e)
    {
        closedCount += 1;
    }

    #endregion Socket Event Handlers

    #region Private Methods
    private IEnumerator TryReOpen()
    {
        reOpenTrying = true;
        yield return new WaitForSeconds(1);
        if (socket != null && !socket.IsAlive) socket.Connect();
        reOpenTrying = false;
    }
    #endregion

    #region Sub-classes
    [Serializable] public class SessionUrl { public string code; public Content content; [Serializable] public class Content { public string sessionUrl; } }
    [Serializable] public class DonationControl { public int startSecond; public int endSecond; public bool stopVideo; public bool titleExpose; public string donationId; public int payAmount; public bool isAnonymous; public bool useSpeech; }
    [Serializable] public class VideoDonationList { public List<string> videoDonation; }
    [Serializable] public class VideoDonation { public int startSecond; public int endSecond; public string videoType; public string videoId; public string playMode; public bool stopVideo; public bool titleExpose; public string donationId; public string profile; public int payAmount; public string donationText; public bool isAnonymous; public int tierNo; public bool useSpeech; }
    [Serializable] public class Profile { public string userIdHash; public string nickname; public string profileImageUrl; public string userRoleCode; public string badge; public string title; public bool verifiedMark; public List<ActivityBadge> activityBadges; [Serializable] public class ActivityBadge { public int badgeNo; public string badgeId; public string imageUrl; public bool activated; } public StreamingProperty streamingProperty; [Serializable] public class StreamingProperty { public Subscription subscription; [Serializable] public class Subscription { public int accumulativeMonth; public int tier; public Badge badge; [Serializable] public class Badge { public string imageUrl; } } public NicknameColor nicknameColor; [Serializable] public class NicknameColor { public string colorCode; } } }
    #endregion
}