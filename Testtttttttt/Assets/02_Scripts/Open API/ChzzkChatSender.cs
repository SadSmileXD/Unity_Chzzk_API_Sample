using sadSmile;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
[System.Serializable]
public class SendChatRequest
{
    public string message;
}
[System.Serializable]
public class SendChatContent
{
    public string messageId;
}

[System.Serializable]
public class SendChatResponse
{
    public int code;
    public string message;
    public SendChatContent content;
}
public class ChzzkChatSender : MonoBehaviour
{
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/chats/send";
    /*
     샘플
    GetComponent<ChzzkChatSender>()
    .SendChat("안녕하세요!");

GetComponent<ChzzkChatSender>()
    .SendChat("메이플 보스 갑니다!");
     */
    public void SendChat(string message)
    {
        StartCoroutine(RequestSendChat(message));
    }

    private IEnumerator RequestSendChat(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogError("메시지가 비어있습니다.");
            yield break;
        }

        if (message.Length > 100)
        {
            Debug.LogError("메시지는 최대 100자입니다.");
            yield break;
        }

        var body = new SendChatRequest
        {
            message = message
        };

        string json =
            JsonUtility.ToJson(body);

        using (UnityWebRequest request =
               new UnityWebRequest(Url, "POST"))
        {
            byte[] data =
                Encoding.UTF8.GetBytes(json);

            request.uploadHandler =
                new UploadHandlerRaw(data);

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
                Debug.Log(request.downloadHandler.text);

                var response =
                    JsonUtility.FromJson<SendChatResponse>(
                        request.downloadHandler.text);

                Debug.Log(
                    $"채팅 전송 성공\n" +
                    $"MessageId : {response.content.messageId}");
            }
            else
            {
                Debug.LogError(
                    $"전송 실패 : {request.responseCode}\n" +
                    request.downloadHandler.text);
            }
        }
    }
}