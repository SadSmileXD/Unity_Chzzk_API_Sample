using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

// 데이터 모델은 네임스페이스 밖이나 안에 자유롭게 배치하되, 
// MonoBehaviour 상속 클래스는 반드시 파일 맨 끝까지 닫혀야 합니다.
[Serializable]
public class ChannelInfo
{
    public string channelId;
    public string channelName;
    public string channelImageUrl;
    public int followerCount;
    public bool verifiedMark;
}

[Serializable]
public class ChannelResponse
{
    // 치지직 API는 응답이 { "content": { "data": [...] } } 형태입니다.
    public Content content;
}

[Serializable]
public class Content { public ChannelInfo[] data; }

namespace sadSmile
{
    public class chzzkChannel : MonoBehaviour
    {
        private const string Url = "https://openapi.chzzk.naver.com/open/v1/channels";

        private string clientId=> ChzzkAuth.instance.clientId;
        private string clientSecret=> ChzzkAuth.instance.clientSecret; // 인스펙터에서 직접 입력하거나 AuthManager에서 가져오세요
       
        [Header("조회할 채널 ID")]
        [SerializeField] private string channelId;

        private void Start()
        {
            if (!string.IsNullOrEmpty(channelId))
            {
                FetchChannel(channelId);
            }
        }

        public void FetchChannel(string id)
        {
            StartCoroutine(RequestChannel(id));
        }

        private IEnumerator RequestChannel(string id)
        {
            string url = $"{Url}?channelIds={UnityWebRequest.EscapeURL(id)}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Client-Id", clientId);
                request.SetRequestHeader("Client-Secret", clientSecret);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("응답 원본: " + request.downloadHandler.text);

                    // 1. content 객체를 거쳐서 파싱
                    var response = JsonUtility.FromJson<ChannelResponse>(request.downloadHandler.text);

                    if (response?.content?.data == null)
                    {
                        Debug.LogWarning("조회된 채널이 없습니다.");
                        yield break;
                    }

                    foreach (var channel in response.content.data)
                    {
                        Debug.Log($"채널명: {channel.channelName} / 팔로워: {channel.followerCount}");
                    }
                }
                else
                {
                    Debug.LogError($"요청 실패 : {request.responseCode}\n{request.downloadHandler.text}");
                }
            }
        }
    }
}

//private IEnumerator RequestrInfo(string accessToken)
//{
//    using (UnityWebRequest request = UnityWebRequest.Get(Url))
//    {
//        request.SetRequestHeader("Authorization", "Bearer " + accessToken);
//        request.SetRequestHeader("Content-Type", "application/json");
//        yield return request.SendWebRequest();
//        if (request.result == UnityWebRequest.Result.Success)
//        {
//            var response = JsonUtility.FromJson<RequestchannelIds>(request.downloadHandler.text);
//        }
//    }
//}