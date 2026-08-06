using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 12(저스티스 백작): 가장 가까운 1명에게 창 투척 → 명중 시 연쇄 번개.
/// </summary>
public class Unit12JudgmentLance : MonoBehaviour
{
    const string LanceResource = "Addons/Ver300/0_Unit/0_Sprite/8_Weapons/1_Spear/New_Weapon_13";
    const float LanceRotationOffsetDeg = 90f;
    const float FlightSpeed = 22f;
    const float HitRadius = 0.45f;
    const float MaxLifetime = 2.5f;
    const float ChainRadius = 2.6f;
    const float EffectLifetime = 0.14f;

    public int sourceUnitNumber;

    static readonly Color LightningColor = new Color(1f, 0.92f, 0.45f, 1f);
    static readonly Color LanceFlashColor = new Color(0.88f, 0.92f, 1f, 0.75f);

    Vector2 _from;
    Zombie _target;
    int _primaryDamage;
    int _chainCount;
    float _chainDamageRatio;
    float _stunDuration;
    float _spawnTime;
    Transform _lance;
    SpriteRenderer _lanceRenderer;
    bool _hitApplied;
    MonoBehaviour _effectHost;

    public void Launch(Vector2 from, Zombie target, int primaryDamage, int chainCount, float chainDamageRatio, float stunDuration, MonoBehaviour effectHost = null)
    {
        _from = from;
        _target = target;
        _primaryDamage = Mathf.Max(1, primaryDamage);
        _chainCount = Mathf.Max(0, chainCount);
        _chainDamageRatio = Mathf.Clamp01(chainDamageRatio);
        _stunDuration = Mathf.Max(0f, stunDuration);
        _spawnTime = Time.time;
        _hitApplied = false;
        _effectHost = effectHost != null ? effectHost : this;
        SpawnLaunchRing(from);
        CreateLanceVisual(from);
    }

    void CreateLanceVisual(Vector2 from)
    {
        Sprite sp = LoadLanceSprite();
        if (sp == null) return;

        GameObject go = new GameObject("JudgmentLance");
        go.transform.position = new Vector3(from.x, from.y, 0f);
        go.transform.localScale = Vector3.one * 1.1f;
        _lanceRenderer = go.AddComponent<SpriteRenderer>();
        _lanceRenderer.sprite = sp;
        _lanceRenderer.color = Color.white;
        _lanceRenderer.sortingOrder = 50;
        _lance = go.transform;
        UpdateLanceRotation();
    }

    static Sprite LoadLanceSprite()
    {
        Sprite sp = Resources.Load<Sprite>(LanceResource);
        if (sp != null) return sp;

        Sprite[] folder = Resources.LoadAll<Sprite>("Addons/Ver300/0_Unit/0_Sprite/8_Weapons/1_Spear");
        if (folder == null) return null;
        for (int i = 0; i < folder.Length; i++)
        {
            if (folder[i] != null && folder[i].name == "New_Weapon_13") return folder[i];
        }
        return folder.Length > 0 ? folder[0] : null;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (Time.time - _spawnTime > MaxLifetime)
        {
            CleanupLanceVisual();
            Destroy(gameObject);
            return;
        }

        if (_hitApplied || _lance == null) return;

        if (_target == null || _target.IsExcludedFromCombat)
        {
            CleanupLanceVisual();
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = _target.transform.position;
        Vector3 current = _lance.position;
        Vector3 step = targetPos - current;
        float dist = step.magnitude;

        if (dist <= HitRadius)
        {
            ApplyJudgmentHit(_target.transform.position);
            return;
        }

        step = step.normalized * (FlightSpeed * Time.deltaTime);
        _lance.position = step.magnitude > dist ? targetPos : current + step;
        _lance.Rotate(0f, 0f, -720f * Time.deltaTime);
        UpdateLanceRotation();
    }

    void UpdateLanceRotation()
    {
        if (_lance == null || _target == null) return;
        Vector2 d = (Vector2)(_target.transform.position - _lance.position);
        if (d.sqrMagnitude < 0.0001f) return;
        float z = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + LanceRotationOffsetDeg + 180f;
        _lance.rotation = Quaternion.Euler(0f, 0f, z);
    }

    void ApplyJudgmentHit(Vector3 hitPoint)
    {
        _hitApplied = true;
        if (_target != null && !_target.IsExcludedFromCombat)
        {
            _target.TakeDamage(_primaryDamage, sourceUnitNumber);
            _target.ApplyStun(_stunDuration);
        }

        SpawnBriefHitFlash(hitPoint);
        SpawnLightningRing(hitPoint);
        ApplyLightningChain(_target, hitPoint);
        CleanupLanceVisual();
        Destroy(gameObject);
    }

    void ApplyLightningChain(Zombie origin, Vector3 fromPos)
    {
        if (origin == null || _chainCount <= 0) return;

        int chainDamage = Mathf.Max(1, Mathf.RoundToInt(_primaryDamage * _chainDamageRatio));
        HashSet<Zombie> hit = new HashSet<Zombie> { origin };
        Vector3 pos = fromPos;

        for (int round = 0; round < _chainCount; round++)
        {
            Zombie next = FindChainTarget(pos, hit);
            if (next == null) break;

            hit.Add(next);
            SpawnLightningBolt(pos, next.transform.position);
            next.TakeDamage(chainDamage, sourceUnitNumber);
            SpawnLightningRing(next.transform.position);
            pos = next.transform.position;
        }
    }

    static Zombie FindChainTarget(Vector3 from, HashSet<Zombie> exclude)
    {
        Zombie best = null;
        float bestDist = ChainRadius;
        Zombie[] all = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Zombie z = all[i];
            if (z == null || z.IsExcludedFromCombat || exclude.Contains(z)) continue;
            float d = Vector2.Distance(from, z.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = z;
            }
        }
        return best;
    }

    void SpawnLaunchRing(Vector2 at)
    {
        GameObject ring = CreateRing("Unit12LaunchRing", at, LanceFlashColor, 4);
        RunEffect(ExpandFadeRing(ring, ring.GetComponent<SpriteRenderer>(), 0.85f, EffectLifetime));
    }

    void SpawnLightningRing(Vector3 at)
    {
        GameObject ring = CreateRing("Unit12ChainRing", at, new Color(LightningColor.r, LightningColor.g, LightningColor.b, 0.85f), 5);
        RunEffect(ExpandFadeRing(ring, ring.GetComponent<SpriteRenderer>(), 0.75f, EffectLifetime));
    }

    void SpawnLightningBolt(Vector3 from, Vector3 to)
    {
        GameObject go = new GameObject("Unit12ChainBolt");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 7;
        lr.startWidth = 0.11f;
        lr.endWidth = 0.035f;
        lr.startColor = LightningColor;
        lr.endColor = new Color(1f, 1f, 0.7f, 0.25f);

        int segments = 5;
        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Lerp(from, to, t);
            if (i > 0 && i < segments)
            {
                p += new Vector3(Random.Range(-0.22f, 0.22f), Random.Range(-0.18f, 0.18f), 0f);
            }
            lr.SetPosition(i, p);
        }

        RunEffect(FadeDestroyLine(go, lr, EffectLifetime));
    }

    void SpawnBriefHitFlash(Vector3 at)
    {
        GameObject flash = CreateRing("Unit12HitFlash", at, LanceFlashColor, 6);
        RunEffect(ExpandFadeRing(flash, flash.GetComponent<SpriteRenderer>(), 0.55f, EffectLifetime * 0.85f));
    }

    static GameObject CreateRing(string name, Vector3 at, Color color, int sortingOrder)
    {
        GameObject ring = new GameObject(name);
        ring.transform.position = at;
        ring.transform.localScale = Vector3.one * 0.12f;
        SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
        sr.sprite = MakeRingSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return ring;
    }

    void RunEffect(IEnumerator routine)
    {
        if (_effectHost != null)
        {
            _effectHost.StartCoroutine(routine);
        }
    }

    static IEnumerator ExpandFadeRing(GameObject obj, SpriteRenderer sr, float endScale, float duration)
    {
        if (obj == null || sr == null) yield break;

        Object.Destroy(obj, duration + 0.02f);
        Vector3 start = obj.transform.localScale;
        Vector3 end = Vector3.one * endScale;
        Color baseColor = sr.color;
        float t = 0f;
        while (t < duration && obj != null && sr != null)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            obj.transform.localScale = Vector3.Lerp(start, end, p);
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - p));
            yield return null;
        }
        if (obj != null) Object.Destroy(obj);
    }

    static IEnumerator FadeDestroyLine(GameObject go, LineRenderer lr, float lifetime)
    {
        if (go == null || lr == null) yield break;

        Object.Destroy(go, lifetime + 0.02f);
        float t = 0f;
        Color sc = lr.startColor;
        Color ec = lr.endColor;
        while (t < lifetime && go != null && lr != null)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / lifetime);
            lr.startColor = new Color(sc.r, sc.g, sc.b, sc.a * a);
            lr.endColor = new Color(ec.r, ec.g, ec.b, ec.a * a);
            yield return null;
        }
        if (go != null) Object.Destroy(go);
    }

    void CleanupLanceVisual()
    {
        if (_lance != null)
        {
            Destroy(_lance.gameObject);
            _lance = null;
            _lanceRenderer = null;
        }
    }

    static Sprite _ringSprite;

    static Sprite MakeRingSprite()
    {
        if (_ringSprite != null) return _ringSprite;
        const int size = 48;
        Texture2D tex = new Texture2D(size, size);
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / (size * 0.5f);
                tex.SetPixel(x, y, (d > 0.55f && d < 0.95f) ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        _ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _ringSprite;
    }

    public static void ClearAll()
    {
        Unit12JudgmentLance[] arr = FindObjectsByType<Unit12JudgmentLance>(FindObjectsSortMode.None);
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] != null) Destroy(arr[i].gameObject);
        }

        DestroyObjectsNamed("JudgmentLance");
        DestroyObjectsNamed("Unit12LaunchRing");
        DestroyObjectsNamed("Unit12ChainRing");
        DestroyObjectsNamed("Unit12ChainBolt");
        DestroyObjectsNamed("Unit12HitFlash");
        Unit12PathDragController.ClearAll();
    }

    static void DestroyObjectsNamed(string objectName)
    {
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject obj = all[i];
            if (obj != null && obj.name == objectName)
            {
                Destroy(obj);
            }
        }
    }
}
