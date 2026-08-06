using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// uGUI 텍스트용 수직 그라데이션 + 주기적 샤인 스윕(빛 훑기) 효과.
/// Outline/Shadow보다 먼저(컴포넌트 순서상 위에) 추가해야 외곽선 색이 유지됩니다.
/// </summary>
public class UIVerticalGradientShine : BaseMeshEffect
{
    public Color topColor = Color.white;
    public Color bottomColor = Color.gray;

    [Header("Shine Sweep")]
    public bool shineEnabled;
    public float shinePeriod = 4.2f;        // 샤인 반복 주기(초)
    public float shineSweepDuration = 0.9f; // 한 번 훑는 데 걸리는 시간(초)
    public float shineHalfWidth = 90f;      // 빛 띠 반폭(px)
    [Range(0f, 1f)] public float shineStrength = 0.85f;

    static readonly List<UIVertex> buffer = new List<UIVertex>();
    float shineTimer;

    void Update()
    {
        if (!shineEnabled || graphic == null) return;

        shineTimer += Time.unscaledDeltaTime;
        if (shineTimer > shinePeriod)
        {
            shineTimer -= shinePeriod;
        }

        // 스윕 중에만 매 프레임 갱신 (마지막 잔상 제거용 여유 포함)
        if (shineTimer <= shineSweepDuration + 0.1f)
        {
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        buffer.Clear();
        vh.GetUIVertexStream(buffer);
        if (buffer.Count == 0) return;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < buffer.Count; i++)
        {
            Vector3 p = buffer[i].position;
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }
        float height = Mathf.Max(0.001f, maxY - minY);

        bool shineActive = shineEnabled && shineTimer <= shineSweepDuration;
        float bandX = 0f;
        if (shineActive)
        {
            float t = Mathf.Clamp01(shineTimer / shineSweepDuration);
            bandX = Mathf.Lerp(minX - shineHalfWidth, maxX + shineHalfWidth, t);
        }

        for (int i = 0; i < buffer.Count; i++)
        {
            UIVertex v = buffer[i];
            float yT = (v.position.y - minY) / height;
            Color c = Color.Lerp(bottomColor, topColor, yT);

            if (shineActive)
            {
                float dist = Mathf.Abs(v.position.x - bandX) / shineHalfWidth;
                float glow = Mathf.Clamp01(1f - dist);
                glow = glow * glow * (3f - 2f * glow); // smoothstep
                c = Color.Lerp(c, Color.white, glow * shineStrength);
            }

            c.a = v.color.a / 255f;
            v.color = c;
            buffer[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(buffer);
    }
}
