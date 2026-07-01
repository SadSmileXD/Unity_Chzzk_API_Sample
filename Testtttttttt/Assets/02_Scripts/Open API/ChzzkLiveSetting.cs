using sadSmile;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
[System.Serializable]
public class LiveCategory
{
    public string categoryType;
    public string categoryId;
    public string categoryValue;
    public string posterImageUrl;
}

[System.Serializable]
public class LiveSettingContent
{
    public string defaultLiveTitle;
    public LiveCategory category;
    public string[] tags;
}

[System.Serializable]
public class LiveSettingResponse
{
    public int code;
    public string message;
    public LiveSettingContent content;
}
public class ChzzkLiveSetting : MonoBehaviour
{
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/lives/setting";

    private void Start()
    {
        GetLiveSetting();
    }
    public void GetLiveSetting()
    {
        StartCoroutine(RequestLiveSetting());
    }

    private IEnumerator RequestLiveSetting()
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
                    JsonUtility.FromJson<LiveSettingResponse>(
                        request.downloadHandler.text);

                var setting = response.content;

                Debug.Log($"제목 : {setting.defaultLiveTitle}");

                if (setting.category != null)
                {
                    Debug.Log(
                        $"카테고리 : {setting.category.categoryValue}");
                }

                if (setting.tags != null)
                {
                    foreach (var tag in setting.tags)
                    {
                        Debug.Log($"태그 : {tag}");
                    }
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