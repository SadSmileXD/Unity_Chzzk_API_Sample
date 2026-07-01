using sadSmile;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class UpdateLiveSettingRequest
{
    public string defaultLiveTitle;
    public string categoryType;
    public string categoryId;
    public string[] tags;
}

[Serializable]
public class ApiResponse
{
    public int code;
    public string message;
}

public class ChzzkLiveSettingUpdater : MonoBehaviour
{
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/lives/setting";

    /// <summary>
    /// 제목 변경
    /// </summary>
    public void ChangeTitle(string title)
    {
        var request = new UpdateLiveSettingRequest
        {
            defaultLiveTitle = title
        };

        StartCoroutine(RequestUpdate(request));
    }

    /// <summary>
    /// 카테고리 변경
    /// </summary>
    public void ChangeCategory(
        string categoryType,
        string categoryId)
    {
        var request = new UpdateLiveSettingRequest
        {
            categoryType = categoryType,
            categoryId = categoryId
        };

        StartCoroutine(RequestUpdate(request));
    }

    /// <summary>
    /// 태그 변경
    /// </summary>
    public void ChangeTags(params string[] tags)
    {
        var request = new UpdateLiveSettingRequest
        {
            tags = tags
        };

        StartCoroutine(RequestUpdate(request));
    }

    /// <summary>
    /// 모든 설정 변경
    /// </summary>
    public void ChangeAll(
        string title,
        string categoryType,
        string categoryId,
        params string[] tags)
    {
        var request = new UpdateLiveSettingRequest
        {
            defaultLiveTitle = title,
            categoryType = categoryType,
            categoryId = categoryId,
            tags = tags
        };

        StartCoroutine(RequestUpdate(request));
    }

    /// <summary>
    /// 카테고리 제거
    /// </summary>
    public void RemoveCategory()
    {
        var request = new UpdateLiveSettingRequest
        {
            categoryId = ""
        };

        StartCoroutine(RequestUpdate(request));
    }

    /// <summary>
    /// 태그 제거
    /// </summary>
    public void RemoveTags()
    {
        var request = new UpdateLiveSettingRequest
        {
            tags = Array.Empty<string>()
        };

        StartCoroutine(RequestUpdate(request));
    }

    private IEnumerator RequestUpdate(
        UpdateLiveSettingRequest data)
    {
        string json = JsonUtility.ToJson(data);

        Debug.Log($"Request JSON\n{json}");

        using (UnityWebRequest request =
               new UnityWebRequest(Url, "PATCH"))
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
                Debug.Log(
                    $"변경 성공\n{request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError(
                    $"변경 실패 : {request.responseCode}\n" +
                    request.downloadHandler.text);
            }
        }
    }
}