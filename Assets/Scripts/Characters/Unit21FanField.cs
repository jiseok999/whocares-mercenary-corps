using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 21: 비행 투시체는 unit_020_attack 스프라이트(노란 틴트) 콜라이더·데미지 없이 이동, 도착 후 제자리 고정.
/// 이후 3초간 선풍(날개 3)만 Overlap으로 데미지 — Undead_Weapon_10.
/// </summary>
public class Unit21FanField : MonoBehaviour
{
    public const string BladeResourcePath1 = "Addons/Undead/0_Unit/0_Sprite/6_Weapons/0_Sword/Undead_Weapon_10";
    public const string BladeResourcePath2 = "Undead_Weapon_10";

    private const float BladeOrbitRadius = 0.4f;
    /// <summary>날개 시각/판정(Overlap 반경)에 동일 비율 적용.</summary>
    private const float BladeSizeScale = 2f;
    private const float HitRadius = 0.18f * BladeSizeScale;
    private const float DamagePerTargetInterval = 0.32f;
    private const float SpinDegreesPerSec = 300f;
    private const int BladeCount = 3;
    private const string FlightResourceName = "unit_020_attack";
    private const int FlightFrameCount = 8;
    private const float FlightFps = 12f;
    private const float FlightScaleMove = 1.3f;
    private const float FlightScaleLanded = 1.2f;
    private static readonly Color FlightTintYellow = new Color(1f, 0.92f, 0.15f, 1f);

    private enum Stage
    {
        Move,
        Spin,
    }

    private Stage _stage = Stage.Move;
    private Vector3 _destination;
    private float _moveSpeed;
    private float _spinEndTime;
    private int _damage;
    private int _sourceUnitNumber;
    private int _sortOrder;
    private float _spinZ;
    private float _spinDurationSec = 3f;
    private float _bladeRadiusMul = 1f;
    private GameObject _fanRoot;
    private Transform[] _blades;
    private SpriteRenderer _flight;
    private readonly Dictionary<Zombie, float> _nextZombieHitTime = new Dictionary<Zombie, float>();

    public void Init(Vector3 start, Vector3 destination, float moveSpeed, float spinDuration, int damage, int sortingBaseOrder, float bladeRadiusMul = 1f, int sourceUnitNumber = 0)
    {
        transform.position = start;
        _destination = new Vector3(destination.x, destination.y, 0f);
        _moveSpeed = Mathf.Max(0.1f, moveSpeed);
        _spinEndTime = 0f;
        _spinDurationSec = Mathf.Max(0.1f, spinDuration);
        _damage = damage;
        _sourceUnitNumber = sourceUnitNumber;
        _sortOrder = sortingBaseOrder;
        _bladeRadiusMul = Mathf.Max(0.5f, bladeRadiusMul);
        _stage = Stage.Move;
        _nextZombieHitTime.Clear();
        CreateFlightVisual();
    }

    void CreateFlightVisual()
    {
        var go = new GameObject("Flight");
        go.transform.SetParent(transform, false);
        _flight = go.AddComponent<SpriteRenderer>();
        Sprite[] frames = Resources.LoadAll<Sprite>(FlightResourceName);
        if (frames != null && frames.Length > 0)
        {
            System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            _flight.sprite = frames[0];
            _flight.color = FlightTintYellow;
            if (_flight.sprite != null)
            {
                Vector2 c = _flight.sprite.bounds.center;
                go.transform.localPosition = new Vector3(-c.x, -c.y, 0f);
            }
            go.transform.localScale = Vector3.one * FlightScaleMove;
            var anim = go.AddComponent<UnitSpriteAnimator>();
            anim.Initialize(_flight, FlightResourceName, FlightFps, FlightFrameCount);
            anim.SetAnimating(true);
        }
        else
        {
            _flight.sprite = MakeSolidSprite(FlightTintYellow);
            _flight.color = Color.white;
            go.transform.localScale = Vector3.one * 0.4f;
        }
        _flight.sortingOrder = _sortOrder + 1;
    }

    void StartSpinning()
    {
        if (_flight != null)
        {
            bool useAttackSprite = _flight.GetComponent<UnitSpriteAnimator>() != null;
            _flight.transform.localScale = Vector3.one * (useAttackSprite ? FlightScaleLanded : 0.36f);
        }
        _fanRoot = new GameObject("Fan");
        _fanRoot.transform.SetParent(transform, false);
        _fanRoot.transform.localPosition = Vector3.zero;
        _spinZ = 0f;
        Sprite sp = LoadBladeSprite();
        _blades = new Transform[BladeCount];
        for (int i = 0; i < BladeCount; i++)
        {
            var blade = new GameObject("Blade_" + i);
            blade.transform.SetParent(_fanRoot.transform, false);
            float a = i * (360f / BladeCount);
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, a);
            var sr = blade.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            if (sp != null)
            {
                Vector2 c = sp.bounds.center;
                blade.transform.localPosition = Quaternion.Euler(0f, 0f, a) * (new Vector3(-c.x, -c.y, 0f) + (Vector3)(Vector2.right * BladeOrbitRadius * _bladeRadiusMul));
            }
            else
            {
                blade.transform.localPosition = Quaternion.Euler(0f, 0f, a) * (Vector3.right * BladeOrbitRadius * _bladeRadiusMul);
            }
            sr.color = Color.white;
            sr.sortingOrder = _sortOrder + 2;
            if (sp != null)
            {
                float h = sp.bounds.size.y;
                if (h > 0.01f)
                {
                    float sc = 0.35f / h * BladeSizeScale;
                    blade.transform.localScale = Vector3.one * sc;
                }
            }
            _blades[i] = blade.transform;
        }
        _fanRoot.transform.localRotation = Quaternion.identity;
    }

    static Sprite LoadBladeSprite()
    {
        Sprite[] a1 = Resources.LoadAll<Sprite>(BladeResourcePath1);
        if (a1 != null && a1.Length > 0)
        {
            return a1[0];
        }
        Sprite[] a2 = Resources.LoadAll<Sprite>(BladeResourcePath2);
        if (a2 != null && a2.Length > 0)
        {
            return a2[0];
        }
        return null;
    }

    static Sprite MakeSolidSprite(Color color)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, color);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (_stage == Stage.Move) RunMove();
        else RunSpin();
    }

    void RunMove()
    {
        Vector3 p = transform.position;
        transform.position = Vector3.MoveTowards(p, _destination, _moveSpeed * Time.deltaTime);
        if (Vector2.Distance((Vector2)transform.position, (Vector2)_destination) < 0.05f)
        {
            transform.position = _destination;
            _stage = Stage.Spin;
            _spinEndTime = Time.time + _spinDurationSec;
            StartSpinning();
        }
    }

    void RunSpin()
    {
        if (Time.time >= _spinEndTime)
        {
            Destroy(gameObject);
            return;
        }
        if (_fanRoot == null) return;
        _fanRoot.transform.localRotation = Quaternion.Euler(0f, 0f, _spinZ);
        _spinZ += SpinDegreesPerSec * Time.deltaTime;
        ApplyBladeDamage();
    }

    void ApplyBladeDamage()
    {
        if (_stage != Stage.Spin) return;
        if (_damage <= 0 || _blades == null) return;
        float t = Time.time;
        for (int i = 0; i < _blades.Length; i++)
        {
            if (_blades[i] == null) continue;
            Vector2 tip = _blades[i].position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(tip, HitRadius * _bladeRadiusMul);
            for (int h = 0; h < hits.Length; h++)
            {
                var z = hits[h] != null ? hits[h].GetComponentInParent<Zombie>() : null;
                if (z == null || z.IsExcludedFromCombat) continue;
                if (_nextZombieHitTime.TryGetValue(z, out float next) && t < next) continue;
                z.TakeDamage(_damage, _sourceUnitNumber);
                _nextZombieHitTime[z] = t + DamagePerTargetInterval;
            }
        }
    }
}
