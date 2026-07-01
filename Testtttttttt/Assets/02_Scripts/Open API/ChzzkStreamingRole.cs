using System.Collections;
using UnityEngine;
using System;
using UnityEngine.Networking;
using sadSmile;
[Serializable]
public class StreamingRole
{
    public string managerChannelId;
    public string managerChannelName;
    public string userRole;
    public string createdDate;
}

[Serializable]
public class StreamingRoleResponse
{
    public StreamingRole[] data;
}
public class ChzzkStreamingRole : MonoBehaviour
{
    //API 사용 등록한 본인 채널만 조회 가능
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/channels/streaming-roles";

    private void Start()
    {
        FetchStreamingRoles();


    }
    public void FetchStreamingRoles()
    {
        StartCoroutine(RequestStreamingRoles());
    }

    private IEnumerator RequestStreamingRoles()
    {
        string accessToken = ChzzkAuth.instance.Key;
            

        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogError("AccessToken이 없습니다.");
            yield break;
        }

        using (UnityWebRequest request =
               UnityWebRequest.Get(Url))
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
                    JsonUtility.FromJson<StreamingRoleResponse>(
                        request.downloadHandler.text);

                if (response?.data == null)
                {
                    Debug.Log("관리자가 없습니다.");
                    yield break;
                }

                foreach (var manager in response.data)
                {
                    Debug.Log(
                        $"관리자 : {manager.managerChannelName}\n" +
                        $"채널ID : {manager.managerChannelId}\n" +
                        $"권한 : {manager.userRole}\n" +
                        $"등록일 : {manager.createdDate}");
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