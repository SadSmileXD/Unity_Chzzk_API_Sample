using sadSmile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEngine;
using UnityEngine.Networking;
[Serializable]
public class CategoryInfo
{
    public string categoryType;
    public string categoryId;
    public string categoryValue;
    public string posterImageUrl;
}

[Serializable]
public class CategoryContent
{
    public CategoryInfo[] data;
}

[Serializable]
public class CategorySearchResponse
{
    public int code;
    public string message;
    public CategoryContent content;
}
public class ChzzkCategory : MonoBehaviour
{
    private const string Url =
        "https://openapi.chzzk.naver.com/open/v1/categories/search";

      private string clientId=> ChzzkAuth.instance.clientId;
      private string clientSecret=> ChzzkAuth.instance.clientSecret;
    public string Category;

    private void Start()
    {
        SearchCategory(Category);
    }
    public void SearchCategory(
        string query,
        int size = 20)
    {
        StartCoroutine(
            RequestCategory(query, size));
    }

    private IEnumerator RequestCategory(
        string query,
        int size)
    {
        string url =
            $"{Url}?query={UnityWebRequest.EscapeURL(query)}&size={size}";

        using (UnityWebRequest request =
               UnityWebRequest.Get(url))
        {
            request.SetRequestHeader(
                "Client-Id",
                clientId);

            request.SetRequestHeader(
                "Client-Secret",
                clientSecret);

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                Debug.Log(request.downloadHandler.text);

                var response =
                    JsonUtility.FromJson<CategorySearchResponse>(
                        request.downloadHandler.text);

                if (response?.content?.data == null ||
      response.content.data.Length == 0)
                {
                    Debug.Log("검색 결과가 없습니다.");
                    yield break;
                }

                foreach (var category in response.content.data)
                {
                    if (category.categoryValue != Category)
                        continue;
                    Debug.Log(
                        $"카테고리 : {category.categoryValue}\n" +
                        $"ID : {category.categoryId}\n" +
                        $"종류 : {category.categoryType}\n" +
                        $"이미지 : {category.posterImageUrl}");
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