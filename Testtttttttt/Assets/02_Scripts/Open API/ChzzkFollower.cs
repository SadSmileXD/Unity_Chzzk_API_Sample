using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Networking;
[Serializable]
public class FollowerInfo
{
    public string channelId;
    public string channelName;
    public string createdDate;
}

[Serializable]
public class FollowerResponse
{
    public FollowerInfo[] data;
}
public class ChzzkFollower : MonoBehaviour
{
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/channels/followers";

    private void Start()
    {
        FetchFollowers();
    }
    public void FetchFollowers(int page = 0,int size = 30)
    {
        StartCoroutine(RequestFollowers(page, size));
    }

    private IEnumerator RequestFollowers(
        int page,
        int size)
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
            $"{Url}?page={page}&size={size}";

        using (UnityWebRequest request =
               UnityWebRequest.Get(url))
        {
            request.SetRequestHeader(
                "Authorization",
                $"Bearer {accessToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("ChzzkFollower");
                Debug.Log(request.downloadHandler.text);

                var response =
                    JsonUtility.FromJson<FollowerResponse>(
                        request.downloadHandler.text);

                if (response?.data == null ||
                    response.data.Length == 0)
                {
                    Debug.Log("팔로워가 없습니다.");
                    yield break;
                }

                foreach (var follower in response.data)
                {
                    Debug.Log(
                        $"이름 : {follower.channelName}\n" +
                        $"채널ID : {follower.channelId}\n" +
                        $"팔로우 날짜 : {follower.createdDate}");
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