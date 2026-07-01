using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
[Serializable]
public class SubscriberInfo
{
    public string channelId;
    public string channelName;
    public int month;
    public int tierNo;
    public string createdDate;
}

[Serializable]
public class SubscriberResponse
{
    public SubscriberInfo[] data;
}
public class ChzzkSubscriber : MonoBehaviour
{
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/channels/subscribers";

    private void Start()
    {
        FetchSubscribers();
    }
    public void FetchSubscribers(
        int page = 0,
        int size = 30,
        string sort = "RECENT")
    {
        StartCoroutine(
            RequestSubscribers(page, size, sort));
    }

    private IEnumerator RequestSubscribers(
        int page,
        int size,
        string sort)
    {
        string accessToken =
            PlayerPrefs.GetString(
                "Chzzk_AccessTokenTest",
                "");

        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogError("AccessToken이 없습니다.");
            yield break;
        }

        string url =
            $"{Url}?page={page}" +
            $"&size={size}" +
            $"&sort={sort}";

        using (UnityWebRequest request =
               UnityWebRequest.Get(url))
        {
            request.SetRequestHeader(
                "Authorization",
                $"Bearer {accessToken}");

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                Debug.Log(request.downloadHandler.text);

                var response =
                    JsonUtility.FromJson<SubscriberResponse>(
                        request.downloadHandler.text);

                if (response?.data == null ||
                    response.data.Length == 0)
                {
                    Debug.Log("구독자가 없습니다.");
                    yield break;
                }

                foreach (var subscriber in response.data)
                {
                    string tier =
                        subscriber.tierNo switch
                        {
                            1 => "티어1",
                            2 => "티어2",
                            _ => $"알 수 없음({subscriber.tierNo})"
                        };

                    Debug.Log(
                        $"이름 : {subscriber.channelName}\n" +
                        $"채널ID : {subscriber.channelId}\n" +
                        $"구독 개월 : {subscriber.month}개월\n" +
                        $"구독 상품 : {tier}\n" +
                        $"구독 시작일 : {subscriber.createdDate}");
                }
            }
            else
            {
                Debug.LogError(
                    $"실패 : {request.responseCode}\n" +
                    request.downloadHandler.text);
            }
        }
    }
}