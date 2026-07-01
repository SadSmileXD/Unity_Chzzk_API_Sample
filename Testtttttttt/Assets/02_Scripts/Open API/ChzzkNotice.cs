
using sadSmile;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class NoticeResponse
{
    public int code;
    public string message;
}
[System.Serializable]
public class NoticeRequest
{
    public string message;
    public string messageId;
}

public class ChzzkNotice : MonoBehaviour
{
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/chats/notice";

    /// <summary>
    /// 새 공지 등록
    /// </summary>
    /// 
    /*Sample
     * notice.RegisterNotice(
    "오늘 방송은 메이플 보스 공략입니다!");
     */
    public void RegisterNotice(string message)
    {
        StartCoroutine(RequestNotice(
            new NoticeRequest
            {
                message = message
            }));
    }

    /// <summary>
    /// 기존 메시지를 공지로 등록
    /// </summary>
    public void RegisterNoticeByMessageId(
        string messageId)
    {
        StartCoroutine(RequestNotice(
            new NoticeRequest
            {
                messageId = messageId
            }));
    }

    private IEnumerator RequestNotice(
        NoticeRequest body)
    {
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
                Debug.Log("공지 등록 성공");
                Debug.Log(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError(
                    $"공지 등록 실패 : {request.responseCode}\n" +
                    request.downloadHandler.text);
            }
        }
    }
}
