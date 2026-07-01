using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using static ChzzkUnity;

// 후원 정보를 안전하게 담아 이벤트를 보낼 데이터 구조체
[System.Serializable]
public class DonationData
{
    public string nickname;  // 후원자 이름
    public int amount;       // 후원 금액 (치즈 개수)
    public string message;   // 후원 메시지
}

// 유니티 인스펙터 창에서 매핑할 수 있도록 노출시키는 커스텀 이벤트
[System.Serializable]
public class ChzzkDonationEvent : UnityEvent<DonationData> { }

