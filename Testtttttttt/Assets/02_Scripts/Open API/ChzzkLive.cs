using sadSmile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
[System.Serializable]
public class LiveInfo
{
    public long liveId;
    public string liveTitle;
    public string liveThumbnailImageUrl;
    public int concurrentUserCount;
    public string openDate;
    public bool adult;
    public string[] tags;

    public string categoryType;
    public string liveCategory;
    public string liveCategoryValue;

    public string channelId;
    public string channelName;
    public string channelImageUrl;
}

[System.Serializable]
public class LivePage
{
    public string next;
}

[System.Serializable]
public class LiveContent
{
    public LiveInfo[] data;
    public LivePage page;
}

[System.Serializable]
public class LiveResponse
{
    public int code;
    public string message;
    public LiveContent content;
}
public class ChzzkLive : MonoBehaviour
{
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/lives";

    private string clientId =>
        ChzzkAuth.instance.clientId;

    private string clientSecret =>
        ChzzkAuth.instance.clientSecret;

    private void Start()
    {
        StartCoroutine(GetLives());
    }

    public IEnumerator GetLives(
        int size = 20,
        string next = "")
    {
        string url =
            $"{Url}?size={size}";

        if (!string.IsNullOrEmpty(next))
            url += $"&next={next}";

        using (UnityWebRequest request =
               UnityWebRequest.Get(url))
        {
            request.SetRequestHeader(
                "Client-Id",
                clientId);

            request.SetRequestHeader(
                "Client-Secret",
                clientSecret);

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                Debug.Log(
                    request.downloadHandler.text);

                var response =
                    JsonUtility.FromJson<LiveResponse>(
                        request.downloadHandler.text);

                foreach (var live in response.content.data)
                {
                    Debug.Log(
                        $"방송 제목 : {live.liveTitle}\n" +
                        $"채널명 : {live.channelName}\n" +
                        $"시청자 : {live.concurrentUserCount}\n" +
                        $"카테고리 : {live.liveCategoryValue}\n" +
                        $"채널ID : {live.channelId}\n");
                }

                Debug.Log(
                    $"다음 페이지 토큰 : " +
                    response.content.page.next);
            }
            else
            {
                Debug.LogError(
                    request.downloadHandler.text);
            }
        }
    }
}