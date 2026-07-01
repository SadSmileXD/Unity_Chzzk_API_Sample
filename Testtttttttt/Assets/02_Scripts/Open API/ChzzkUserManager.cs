using sadSmile;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class UserInfoResponse { public int code; public string message; public UserData content; }
[Serializable]
public class UserData { public string channelId; public string channelName; }

public class ChzzkUserManager : MonoBehaviour
{
    private const string UserInfoUrl = "https://openapi.chzzk.naver.com/open/v1/users/me";

    // 조회된 유저 정보를 저장할 변수
    public string MyChannelId { get; private set; }
    public string MyChannelName { get; private set; }
    private void Start()
    {
        FetchUserInfo(ChzzkAuth.instance.Key);
    }
    public void FetchUserInfo(string accessToken)
    {
        StartCoroutine(RequestUserInfo(accessToken));
    }
   
    private IEnumerator RequestUserInfo(string accessToken)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(UserInfoUrl))
        {
            //헤더
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.SetRequestHeader("Content-Type", "application/json");
            /*
             Header란?
            서버에게 추가 정보를 알려주는 부분입니다.
            예를 들어 택배라고 생각하면:

            택배 내용 : 유저 정보 조회
            주소 : https://openapi.chzzk.naver.com/v1/user/channels
            추가 메모 :
            나는 누구인가?
            어떤 형식으로 보낼 것인가?

            이 추가 메모가 Header입니다.
            
            
            request.SetRequestHeader(
            "Authorization",
            "Bearer " + accessToken);
            
             실제로는:

              Authorization: Bearer abc123

            왜 필요함?

            치지직 서버 입장:
            
            누가 내 정보를 요청했지?
            
            를 알아야 합니다.
            
            토큰이 없으면:
            
            누군지 모르겠는데?
            
            하고 거절합니다.

            Bearer는 뭐임?

규칙입니다.

Authorization: Bearer 토큰값

형태로 보내야 한다는 뜻입니다.

            Authorization: Bearer eyJhbGc...
            왜 문자열을 더하냐?
"Bearer " + accessToken

예:

accessToken = "abc123";

그러면:

"Bearer abc123"

가 됩니다.

2.
request.SetRequestHeader(
    "Content-Type",
    "application/json");

이건:

Content-Type: application/json

입니다.

의미
내가 보내는 데이터는 JSON 형식입니다.
            쉽게 말하면

Authorization
↓
"내가 누구인지" 알려주는 신분증

Content-Type
↓
"내가 보내는 데이터 형식"을 알려주는 설명서
             */
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<UserInfoResponse>(request.downloadHandler.text);
                if (response?.content != null)
                {
                    MyChannelId = response.content.channelId;
                    MyChannelName = response.content.channelName;
                    Debug.Log($"[유저 정보 로드 완료] {MyChannelName} ({MyChannelId})");
                }
            }
            else
            {
                Debug.LogError($"[유저 정보 조회 실패] {request.error}");
            }
        }
    }
}