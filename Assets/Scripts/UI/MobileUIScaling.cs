using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모바일 대응 공통 Canvas Scaler 설정.
/// 기존 코드가 CanvasScaler를 기본값(ConstantPixelSize)으로 붙이던 부분을
/// 전부 이 헬퍼로 교체해서, PC 해상도 기준으로 만든 UI가 폰 화면에서도
/// 비율을 유지하며 스케일되도록 통일한다.
///
/// PC 개발 해상도(1920x1080)를 기준 해상도로 두고, 폭/높이 중간값으로 매칭해서
/// 가로가 넓은 PC 화면과 세로가 상대적으로 좁은(가로모드 폰) 화면 모두에서
/// 텍스트/버튼이 너무 작아지거나 잘리지 않게 한다.
/// </summary>
public static class MobileUIScaling
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    public const float MatchWidthOrHeight = 0.5f;

    /// <summary>
    /// CanvasScaler를 화면 크기에 맞춰 스케일되도록 구성한다.
    /// 반환값을 그대로 체이닝해서 쓸 수 있다: Configure(canvasObj.AddComponent&lt;CanvasScaler&gt;())
    /// </summary>
    public static CanvasScaler Configure(CanvasScaler scaler)
    {
        if (scaler == null) return scaler;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = MatchWidthOrHeight;
        return scaler;
    }
}
