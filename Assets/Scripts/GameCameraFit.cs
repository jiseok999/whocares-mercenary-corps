using UnityEngine;

/// <summary>
/// 고정 해상도(레퍼런스 종횡비) 기준 카메라/월드 레이아웃 유틸리티.
///
/// 문제: 좀비 스폰 위치·보드 경계 같은 게임플레이 좌표 계산이 실시간
/// Screen.width/Screen.height 종횡비를 기준으로 "딱 한 번만" 계산되던 코드가
/// 있었다. 안드로이드는 앱이 세로로 실행됐다가 Player Settings의 가로 고정
/// 옵션에 맞춰 강제로 회전하는 짧은 전환 구간이 있는데, 하필 이 타이밍에
/// Screen.width/Screen.height를 읽으면 세로 기준의 잘못된 종횡비가 계산된다.
/// 좀비 스폰 X좌표(ZombieSpawner.spawnX)처럼 그 값을 캐싱해서 계속 쓰는
/// 코드는 이 한 번의 오차가 게임 내내 유지되어, 좀비가 카메라 시야 밖 아주
/// 먼 곳에서 스폰되어 "적이 전혀 안 보이는" 버그로 이어졌다.
///
/// 해결책: 게임플레이 좌표 계산은 실시간 화면 크기 대신 항상 고정된 레퍼런스
/// 해상도(1920x1080, 16:9)를 기준으로 하고, 카메라는 ConfigureLetterbox()로
/// 실제 화면에 16:9 프레임을 레터박스/필러박스로 맞춘다. 그러면 기기 화면
/// 비율이 무엇이든 "보이는 게임 영역"이 항상 동일한 16:9 고정 해상도가 되고,
/// 좌표 계산과 실제로 보이는 화면이 항상 일치한다.
/// </summary>
public static class GameCameraFit
{
    public const float ReferenceWidth = 1920f;
    public const float ReferenceHeight = 1080f;
    public const float ReferenceAspect = ReferenceWidth / ReferenceHeight;

    static int lastScreenWidth = -1;
    static int lastScreenHeight = -1;

    /// <summary>
    /// 카메라 Viewport Rect를 조정해서, 실제 화면 종횡비가 16:9와 달라도
    /// 항상 16:9 프레임만 보이도록 레터박스(위아래 여백)/필러박스(좌우 여백) 처리한다.
    /// 화면 크기가 바뀌지 않았으면 아무 작업도 하지 않으므로 매 프레임 호출해도 가볍다.
    /// </summary>
    public static void ConfigureLetterbox(Camera cam)
    {
        if (cam == null || Screen.width <= 0 || Screen.height <= 0) return;
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float windowAspect = Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / ReferenceAspect;

        Rect rect = cam.rect;
        if (scaleHeight < 1f)
        {
            // 화면이 레퍼런스보다 세로로 길다 → 위아래를 레터박스로 채운다.
            rect.width = 1f;
            rect.height = scaleHeight;
            rect.x = 0f;
            rect.y = (1f - scaleHeight) / 2f;
        }
        else
        {
            // 화면이 레퍼런스보다 가로로 길다(대부분의 폰) → 좌우를 필러박스로 채운다.
            float scaleWidth = 1f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1f;
            rect.x = (1f - scaleWidth) / 2f;
            rect.y = 0f;
        }
        cam.rect = rect;
    }

    /// <summary>
    /// 실시간 화면 종횡비 대신 고정 레퍼런스 종횡비를 반환한다.
    /// 좀비 스폰 위치, 보드 경계 등 "화면에 실제로 보이는 16:9 프레임" 기준으로
    /// 계산해야 하는 곳에서는 이 값을 Screen.width/(float)Screen.height 대신 쓴다.
    /// </summary>
    public static float Aspect => ReferenceAspect;
}
