using sadSmile;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ChatSettingInfo
{
    public string chatAvailableCondition;
    public string chatAvailableGroup;
    public int minFollowerMinute;
    public bool allowSubscriberInFollowerMode;
    public int chatSlowModeSec;
    public bool chatEmojiMode;
}

[Serializable]
public class ChatSettingResponse
{
    public int code;
    public string message;
    public ChatSettingInfo content;
}

public class ChzzkChatSetting : MonoBehaviour
{
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/chats/settings";

    public void GetChatSetting()
    {
        StartCoroutine(RequestChatSetting());
    }

    private IEnumerator RequestChatSetting()
    {
        using (UnityWebRequest request =
               UnityWebRequest.Get(Url))
        {
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + ChzzkAuth.instance.Key);

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                Debug.Log(request.downloadHandler.text);

                var response =
                    JsonUtility.FromJson<ChatSettingResponse>(
                        request.downloadHandler.text);

                if (response?.content == null)
                {
                    Debug.LogError("응답 파싱 실패");
                    yield break;
                }

                ChatSettingInfo setting =
                    response.content;

                Debug.Log(
                    $"본인인증 : {setting.chatAvailableCondition}");

                Debug.Log(
                    $"채팅 참여 범위 : {setting.chatAvailableGroup}");

                Debug.Log(
                    $"최소 팔로우 시간 : {setting.minFollowerMinute}분");

                Debug.Log(
                    $"구독자 예외 허용 : {setting.allowSubscriberInFollowerMode}");

                Debug.Log(
                    $"슬로우 모드 : {setting.chatSlowModeSec}초");

                Debug.Log(
                    $"이모티콘 모드 : {setting.chatEmojiMode}");
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