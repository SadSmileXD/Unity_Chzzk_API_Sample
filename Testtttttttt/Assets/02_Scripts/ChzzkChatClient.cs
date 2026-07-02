using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SocketIOClient;
using System.Collections.Generic;

namespace sadSmile
{
    public class ChzzkOfficialClient : MonoBehaviour
    {
        public string channelId; // 인스펙터에 입력
        private SocketIOUnity socket;
        private string sessionKey;

        async void Start()
        {
            // ChzzkAuth에서 저장된 토큰 불러오기
            string token = PlayerPrefs.GetString("Chzzk_AccessTokenTest", "");

            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[Chat] 토큰이 없습니다. 먼저 로그인을 완료해주세요.");
                return;
            }

            await InitializeSession(token);
        }

        private async Task InitializeSession(string accessToken)
        {
            using (var client = new HttpClient())
            {
                // 공식 API 호출을 위한 헤더 설정
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                // 공식 API 엔드포인트 호출
                var response = await client.GetAsync("https://openapi.chzzk.naver.com/open/v1/sessions/auth");
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[Auth] API 호출 실패: {response.StatusCode}\n응답: {responseContent}");
                    return;
                }

                // 성공 시 JSON 파싱
                var json = JObject.Parse(responseContent);
                string url = json["content"]["url"]?.ToString();

                Debug.Log($"[Chat] 세션 URL 획득 성공: {url}");
                ConnectSocket(url);
            }
        }

        private void ConnectSocket(string url)
        {
            // Socket.IO 연결 설정
            var uri = new System.Uri(url);
            socket = new SocketIOUnity(uri, new SocketIOOptions
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });

            socket.OnConnected += (sender, e) => Debug.Log("[Chat] 소켓 연결 성공!");

            // 공식 문서에 명시된 시스템 이벤트 처리
            socket.On("SYSTEM", (response) => {
                var data = JObject.Parse(response.GetValue<string>());
                string type = data["type"]?.ToString();

                if (type == "connected")
                {
                    sessionKey = data["data"]["sessionKey"]?.ToString();
                    Debug.Log($"[Chat] 세션 키 획득: {sessionKey}");
                    SubscribeEvents();
                }
            });

            // 채팅 및 후원 메시지 처리
            socket.On("CHAT", (res) => Debug.Log($"[채팅 수신]: {res.GetValue<string>()}"));
            socket.On("DONATION", (res) => Debug.Log($"[후원 수신]: {res.GetValue<string>()}"));

            socket.Connect();
        }

        private async void SubscribeEvents()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + PlayerPrefs.GetString("Chzzk_AccessTokenTest"));

                // 구독 요청 (세션 키 + 채널 ID)
                var payload = new Dictionary<string, string> {
                    { "sessionKey", sessionKey },
                    { "channelId", channelId }
                };

                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");

                await client.PostAsync("https://api.chzzk.naver.com/open/v1/sessions/events/subscribe/chat", content);
                await client.PostAsync("https://api.chzzk.naver.com/open/v1/sessions/events/subscribe/donation", content);

                Debug.Log("[Chat] 채팅 및 후원 구독 요청 완료");
            }
        }
    }
}