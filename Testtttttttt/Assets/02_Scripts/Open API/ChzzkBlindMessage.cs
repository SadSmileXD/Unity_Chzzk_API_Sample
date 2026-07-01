using sadSmile;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ChzzkBlindMessage : MonoBehaviour
{
    [System.Serializable]
    public class BlindMessageRequest
    {//요청 클래스
        public string chatChannelId;
        public long messageTime;
        public string senderChannelId;
    }

    [System.Serializable]
    public class CommonResponse
    {//응답 클래스
        public int code;
        public string message;
    }
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/chats/blind-message";

    public void BlindMessage(
        string chatChannelId,
        long messageTime,
        string senderChannelId)
    {
        StartCoroutine(
            RequestBlindMessage(
                chatChannelId,
                messageTime,
                senderChannelId));
    }

    private IEnumerator RequestBlindMessage(
        string chatChannelId,
        long messageTime,
        string senderChannelId)
    {
        var data = new BlindMessageRequest
        {
            chatChannelId = chatChannelId,
            messageTime = messageTime,
            senderChannelId = senderChannelId
        };

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request =
               new UnityWebRequest(Url, "POST"))
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
                Debug.Log("메시지 숨기기 성공");
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