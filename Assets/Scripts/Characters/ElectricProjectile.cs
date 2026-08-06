using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전기 투사체: 목표에 맞으면 추가로 가까운 적에게 전기 효과와 데미지
/// </summary>
public class ElectricProjectile : MonoBehaviour
{
    public float speed = 3f;
    public int damage = 2;
    public int sourceUnitNumber;
    public int chainDamage = 2;
    public float chainRange = 5f;
    public int maxChainCount = 1;
    public float chainDamageMultiplier = 1f;
    
    private Zombie target;
    private Vector2 lastDirection = Vector2.right;
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames = System.Array.Empty<Sprite>();
    private float frameTimer = 0f;
    private int frameIndex = 0;
    private bool introFinished = false;
    private const float FrameTime = 0.06f;
    
    void Update()
    {
        // 상점이 열려있으면 투사체 정지
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        UpdateAnimation();
        
        // 타겟이 있으면 타겟을 향해 이동, 없으면 마지막 방향으로 이동
        if (target != null)
        {
            Vector2 direction = (target.transform.position - transform.position).normalized;
            lastDirection = direction;
        }

        // 이동 방향에 맞게 회전
        if (lastDirection.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(lastDirection.y, lastDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        
        transform.position += (Vector3)(lastDirection * speed * Time.deltaTime);
        
        // 화면 밖으로 나가면 제거
        DestroyIfOutOfScreen();
        
        // 타겟에 도달했는지 확인 (히트 판정 여유)
        if (target != null && Vector2.Distance(transform.position, target.transform.position) < 0.5f)
        {
            HitTarget();
        }
        else if (target == null)
        {
            // 타겟이 없어도 다른 좀비에 맞으면 데미지
            CheckHitAnyZombie();
        }
    }
    
    /// <summary>
    /// 타겟 설정
    /// </summary>
    public void SetTarget(Zombie targetZombie)
    {
        target = targetZombie;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        frames = Resources.LoadAll<Sprite>("unit_003_attack");
        if (frames == null || frames.Length == 0)
        {
            frames = Resources.LoadAll<Sprite>("unit_003");
        }
        if (frames == null || frames.Length == 0)
        {
            frames = System.Array.Empty<Sprite>();
        }
        else
        {
            System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
        }

        Unit1ProjectileAfterimage.Attach(gameObject);
    }

    void UpdateAnimation()
    {
        if (spriteRenderer == null || frames.Length == 0)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        if (frameTimer < FrameTime)
        {
            return;
        }

        frameTimer -= FrameTime;
        int maxIntroIndex = Mathf.Min(7, frames.Length - 1);
        int loopStartIndex = Mathf.Min(4, frames.Length - 1);

        if (!introFinished)
        {
            spriteRenderer.sprite = frames[frameIndex];
            frameIndex++;
            if (frameIndex > maxIntroIndex)
            {
                introFinished = true;
                frameIndex = loopStartIndex;
            }
            return;
        }

        spriteRenderer.sprite = frames[frameIndex];
        frameIndex++;
        if (frameIndex > maxIntroIndex)
        {
            frameIndex = loopStartIndex;
        }
    }
    
    /// <summary>
    /// 타겟에 명중
    /// </summary>
    void HitTarget()
    {
        if (target != null)
        {
            target.TakeDamage(damage, sourceUnitNumber);
            AttackHitEffectFire.Spawn(target.transform.position, Vector3.one * 2f);

            ApplyChainDamage(target);
        }
        
        Destroy(gameObject);
    }
    
    void CheckHitAnyZombie()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        float hitRadius = 0.5f;
        
        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            float distance = Vector2.Distance(transform.position, zombie.transform.position);
            if (distance < hitRadius)
            {
                zombie.TakeDamage(damage, sourceUnitNumber);
                AttackHitEffectFire.Spawn(zombie.transform.position, Vector3.one * 2f);
                // 전기 연쇄는 타겟이 있을 때만 처리
                Destroy(gameObject);
                return;
            }
        }
    }
    
    void ApplyChainDamage(Zombie origin)
    {
        if (origin == null || maxChainCount <= 0) return;

        int chainHitDamage = Mathf.Max(1, Mathf.RoundToInt(chainDamage * chainDamageMultiplier));
        var chained = new HashSet<Zombie> { origin };
        Zombie current = origin;

        for (int i = 0; i < maxChainCount; i++)
        {
            Zombie chainTarget = FindNearestOtherZombie(current, chained);
            if (chainTarget == null) break;

            CreateElectricEffect(current.transform.position, chainTarget.transform.position);
            chainTarget.TakeDamage(chainHitDamage, sourceUnitNumber);
            AttackHitEffectFire.Spawn(chainTarget.transform.position, Vector3.one * 2f);
            chained.Add(chainTarget);
            current = chainTarget;
        }
    }

    Zombie FindNearestOtherZombie(Zombie excluded, HashSet<Zombie> alsoExcluded = null)
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        Zombie nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie == excluded || zombie.IsExcludedFromCombat) continue;
            if (alsoExcluded != null && alsoExcluded.Contains(zombie)) continue;
            
            float distance = Vector2.Distance(excluded.transform.position, zombie.transform.position);
            if (distance < nearestDistance && distance <= chainRange)
            {
                nearestDistance = distance;
                nearest = zombie;
            }
        }
        
        return nearest;
    }
    
    static readonly Color BoltStartColor = new Color(0.35f, 0.65f, 1f, 1f);
    static readonly Color BoltEndColor = new Color(0.6f, 0.85f, 1f, 0.3f);

    /// <summary>
    /// 연쇄 전달 이펙트 — 푸른색 지그재그 번개 볼트 (유닛 12 연쇄 번개와 동일 스타일)
    /// </summary>
    void CreateElectricEffect(Vector3 from, Vector3 to)
    {
        GameObject effectObj = new GameObject("ElectricChainBolt");
        LineRenderer line = effectObj.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.sortingOrder = 7;
        line.startWidth = 0.12f;
        line.endWidth = 0.04f;
        line.startColor = BoltStartColor;
        line.endColor = BoltEndColor;

        int segments = 5;
        line.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Lerp(from, to, t);
            if (i > 0 && i < segments)
            {
                p += new Vector3(Random.Range(-0.22f, 0.22f), Random.Range(-0.18f, 0.18f), 0f);
            }
            line.SetPosition(i, p);
        }

        effectObj.AddComponent<LightningBoltFade>().Init(line, 0.18f);
    }

    /// <summary>번개 볼트를 서서히 사라지게 한 뒤 제거합니다 (투사체 파괴와 무관하게 동작).</summary>
    class LightningBoltFade : MonoBehaviour
    {
        LineRenderer _line;
        float _lifetime;
        float _elapsed;
        Color _startColor;
        Color _endColor;

        public void Init(LineRenderer line, float lifetime)
        {
            _line = line;
            _lifetime = Mathf.Max(0.05f, lifetime);
            _startColor = line.startColor;
            _endColor = line.endColor;
            Destroy(gameObject, _lifetime + 0.05f);
        }

        void Update()
        {
            if (_line == null) return;
            _elapsed += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(_elapsed / _lifetime);
            _line.startColor = new Color(_startColor.r, _startColor.g, _startColor.b, _startColor.a * a);
            _line.endColor = new Color(_endColor.r, _endColor.g, _endColor.b, _endColor.a * a);
        }
    }
    
    void DestroyIfOutOfScreen()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        float camHalfWidth = cam.orthographicSize * (Screen.width / (float)Screen.height);
        float camHalfHeight = cam.orthographicSize;
        float left = cam.transform.position.x - camHalfWidth;
        float right = cam.transform.position.x + camHalfWidth;
        float bottom = cam.transform.position.y - camHalfHeight;
        float top = cam.transform.position.y + camHalfHeight;
        
        if (transform.position.x < left - 1f || transform.position.x > right + 1f ||
            transform.position.y < bottom - 1f || transform.position.y > top + 1f)
        {
            Destroy(gameObject);
        }
    }
}


