using sadSmile;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
[System.Serializable]
public class StreamKeyContent
{
    public string streamKey;
}

[System.Serializable]
public class StreamKeyResponse
{
    public int code;
    public string message;
    public StreamKeyContent content;
}
public class ChzzkStreamKey : MonoBehaviour
{
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/streams/key";

    private void Start()
    {
        GetStreamKey();
    }
    public void GetStreamKey()
    {
        StartCoroutine(RequestStreamKey());
    }

    private IEnumerator RequestStreamKey()
    {
        string accessToken =
            ChzzkAuth.instance.Key;

        using (UnityWebRequest request =
               UnityWebRequest.Get(Url))
        {
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + accessToken);

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                Debug.Log(request.downloadHandler.text);

                var response =
                    JsonUtility.FromJson<StreamKeyResponse>(
                        request.downloadHandler.text);

                Debug.Log(
                    $"스트림키 : {response.content.streamKey}");
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