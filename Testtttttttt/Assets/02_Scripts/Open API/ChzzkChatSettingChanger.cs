using sadSmile;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

//sample
/*
 * 전체 채팅 허용
 ChangeChatSetting(
    "NONE",
    "ALL",
    0,
    false,
    0,
    false);

팔로워 전용 + 10분 이상 + 슬로우 5초
ChangeChatSetting(
    "NONE",
    "FOLLOWER",
    10,
    true,
    5,
    false);
운영자만 채팅 가능
ChangeChatSetting(
    "NONE",
    "MANAGER",
    0,
    false,
    0,
    false);
 */
[System.Serializable]
public class ChatSettingRequest
{
    public string chatAvailableCondition;
    public string chatAvailableGroup;
    public int minFollowerMinute;
    public bool allowSubscriberInFollowerMode;
    public int chatSlowModeSec;
    public bool chatEmojiMode;
}
[System.Serializable]
public class CommonResponse
{
    public int code;
    public string message;
}
public class ChzzkChatSettingChanger : MonoBehaviour
{
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/chats/settings";

    public void ChangeChatSetting(
        string availableCondition,
        string availableGroup,
        int followerMinute,
        bool allowSubscriber,
        int slowModeSec,
        bool emojiMode)
    {
        StartCoroutine(
            RequestChangeChatSetting(
                availableCondition,
                availableGroup,
                followerMinute,
                allowSubscriber,
                slowModeSec,
                emojiMode));
    }

    private IEnumerator RequestChangeChatSetting(
        string availableCondition,
        string availableGroup,
        int followerMinute,
        bool allowSubscriber,
        int slowModeSec,
        bool emojiMode)
    {
        ChatSettingRequest data =
            new ChatSettingRequest
            {
                chatAvailableCondition = availableCondition,
                chatAvailableGroup = availableGroup,
                minFollowerMinute = followerMinute,
                allowSubscriberInFollowerMode = allowSubscriber,
                chatSlowModeSec = slowModeSec,
                chatEmojiMode = emojiMode
            };

        string json =
            JsonUtility.ToJson(data);

        using (UnityWebRequest request =
               new UnityWebRequest(Url, "PUT"))
        {
            byte[] body =
                Encoding.UTF8.GetBytes(json);

            request.uploadHandler =
                new UploadHandlerRaw(body);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Authorization",
                "Bearer " + ChzzkAuth.instance.Key);

            request.SetRequestHeader(
                "Content-Type",
                "application/json");

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                Debug.Log("채팅 설정 변경 성공");
                Debug.Log(request.downloadHandler.text);
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