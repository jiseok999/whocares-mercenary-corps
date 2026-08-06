using UnityEngine;

/// <summary>
/// 조합 도적단 표창 — 단일 대상 추적 후 명중.
/// </summary>
public class TraitShurikenProjectile : MonoBehaviour
{
    public Zombie target;
    public int damage = 1;
    public int sourceUnitNumber;
    public float speed = 15f;
    public float spinSpeed = 900f;
    public float hitRadius = 0.5f;
    public float maxLifetime = 3f;

    float spawnTime;

    public static TraitShurikenProjectile Spawn(Vector3 from, Zombie targetZombie, int damageValue, Color tint, int sourceUnitNumber = 0)
    {
        if (targetZombie == null) return null;

        GameObject go = new GameObject("TraitShuriken");
        go.transform.position = from;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = LoadShurikenSprite();
        sr.color = tint;
        sr.sortingOrder = 6;
        visual.transform.localScale = Vector3.one * 0.55f;

        if (sr.sprite != null)
        {
            Vector2 c = sr.sprite.bounds.center;
            visual.transform.localPosition = new Vector3(-c.x, -c.y, 0f);
        }

        TraitShurikenProjectile proj = go.AddComponent<TraitShurikenProjectile>();
        proj.target = targetZombie;
        proj.damage = damageValue;
        proj.sourceUnitNumber = sourceUnitNumber;
        proj.spawnTime = Time.time;
        Unit1ProjectileAfterimage.Attach(visual);
        return proj;
    }

    static Sprite _shurikenSprite;

    static Sprite LoadShurikenSprite()
    {
        if (_shurikenSprite != null) return _shurikenSprite;
        _shurikenSprite = Resources.Load<Sprite>("Addons/Elf/0_Unit/0_Sprite/6_Weapons/3_Shield/Elf_Weapon_16");
        return _shurikenSprite;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (Time.time - spawnTime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null || target.IsExcludedFromCombat)
        {
            Destroy(gameObject);
            return;
        }

        transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        Vector3 to = target.transform.position - transform.position;
        float dist = to.magnitude;
        if (dist <= hitRadius)
        {
            target.TakeDamage(damage, sourceUnitNumber);
            AttackHitEffectFire.Spawn(target.transform.position, Vector3.one * 1.1f, new Color(0.8f, 0.82f, 0.9f, 1f));
            Destroy(gameObject);
            return;
        }

        transform.position += to.normalized * (speed * Time.deltaTime);
    }
}
