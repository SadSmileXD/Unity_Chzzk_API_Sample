using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using static ChzzkUnity;
[RequireComponent(typeof(ChzzkUnity))]
public class ChzzkDonationReceiver : MonoBehaviour
{
    [Header("치지직 채널 설정")]
    [SerializeField] private string channelId = "여기에_채널ID를_입력하세요";

    [Header("후원 반응 이벤트 (인스펙터 연결)")]
    public ChzzkDonationEvent OnDonationReceived;

    private ChzzkUnity chzzk;

    // 웹소켓(멀티스레드)에서 들어온 명령을 메인 프레임 스레드로 가져오기 위한 큐
    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    private void Awake()
    {
        chzzk = GetComponent<ChzzkUnity>();
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(channelId))
        {
            Debug.LogError("[ChzzkReceiver] 채널 ID가 설정되지 않았습니다. 인스펙터를 확인하세요.");
            return;
        }

        // ChzzkUnity에 내장된 후원 이벤트 발생 시 실행할 함수 구독
        chzzk.onDonation.AddListener(ProcessDonationData);

        // 치지직 방송 소켓 서버 연결 시작
        chzzk.Connect(channelId);
        Debug.Log("[ChzzkReceiver] 치지직 서버 연결을 요청했습니다.");
    }

    private void Update()
    {
        // 백그라운드 스레드에서 쌓인 연출 처리를 유니티 메인스레드(프레임)에서 안전하게 순차 실행
        while (mainThreadActions.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }

    // ChzzkUnity 웹소켓 해석기에서 후원 신호를 감지했을 때 실행되는 메서드
    private void ProcessDonationData(Profile profile, string message, DonationExtras donation) // data. 제거
    {
        mainThreadActions.Enqueue(() =>
        {
            DonationData data = new DonationData
            {
                nickname = donation.isAnonymous ? "익명의 후원자" : (profile != null ? profile.nickname : "이름 없는 후원자"),
                amount = donation.payAmount,
                message = message ?? ""

            };
            Debug.Log($"[ChzzkReceiver] 후원 수신: {data.nickname}님이 {data.amount}원을 후원했습니다. 메시지: {data.message}");
            OnDonationReceived?.Invoke(data);
        });
    }

    private void OnDestroy()
    {
        if (chzzk != null)
        {
            chzzk.onDonation.RemoveListener(ProcessDonationData);
        }
    }
}