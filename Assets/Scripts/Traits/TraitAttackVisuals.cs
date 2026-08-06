using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조합 주기 특수 공격 VFX·피해 적용 (15조합 전체).
/// </summary>
public static class TraitAttackVisuals
{
    static readonly List<Zombie> _zombieBuffer = new List<Zombie>(64);
    static readonly Color MageOuter = new Color(0.25f, 0.55f, 1f, 0.65f);
    static readonly Color MageInner = new Color(0.75f, 0.92f, 1f, 1f);
    static readonly Color WardenGreen = new Color(0.35f, 0.95f, 0.45f, 1f);
    static readonly Color GoblinGold = new Color(1f, 0.82f, 0.2f, 1f);
    static readonly Color FireOrange = new Color(1f, 0.45f, 0.1f, 0.85f);
    static readonly Color IceCyan = new Color(0.55f, 0.92f, 1f, 0.9f);
    static readonly Color LightGold = new Color(1f, 0.95f, 0.55f, 0.95f);
    static readonly Color DarkPurple = new Color(0.45f, 0.15f, 0.65f, 0.85f);
    static readonly Color KnightSilver = new Color(0.88f, 0.92f, 1f, 0.9f);
    static readonly Color DemonRed = new Color(0.95f, 0.2f, 0.25f, 0.9f);
    static readonly Color NatureGreen = new Color(0.3f, 0.9f, 0.4f, 0.9f);
    static readonly Color WindCyan = new Color(0.7f, 0.95f, 1f, 0.85f);
    static readonly Color LightningYellow = new Color(1f, 0.92f, 0.4f, 1f);

    const float ArrowFrameTime = 0.045f;

    public static void Execute(
        MonoBehaviour host,
        TraitPeriodicAttackDefs.AttackKind kind,
        Character source,
        TraitPeriodicAttackDefs.TierDef tier,
        int damage,
        float synergyDamageMul)
    {
        if (host == null || source == null || damage <= 0) return;
        damage = Mathf.Max(1, Mathf.RoundToInt(damage * synergyDamageMul));
        int sourceUnitNumber = source.unitNumber;
        Vector3 from = source.transform.position;

        switch (kind)
        {
            case TraitPeriodicAttackDefs.AttackKind.MageLaser:
                FireMageLaser(host, from, tier.targetCount, damage, sourceUnitNumber);
                break;
            case TraitPeriodicAttackDefs.AttackKind.WardenArrowRain:
                host.StartCoroutine(WardenArrowRain(from, tier.targetCount, damage, sourceUnitNumber));
                break;
            case TraitPeriodicAttackDefs.AttackKind.GoblinCoinBurst:
                GoblinHomingMissiles(host, from, tier.targetCount, damage, sourceUnitNumber);
                break;
            case TraitPeriodicAttackDefs.AttackKind.KnightLanceWave:
                host.StartCoroutine(KnightWave(from, damage, tier.damageScale >= 1.3f ? 1.35f : 1f, sourceUnitNumber));
                break;
            case TraitPeriodicAttackDefs.AttackKind.CalamityPulse:
                host.StartCoroutine(CalamityPulse(from, damage, tier.extraParam > 0f ? tier.extraParam : 2.2f, sourceUnitNumber));
                break;
            case TraitPeriodicAttackDefs.AttackKind.CalamityMark:
                {
                    bool chainOnDeath = tier.extraParam >= 0.24f;
                    int chainDamage = chainOnDeath ? Mathf.Max(1, Mathf.RoundToInt(damage * 3.5f)) : 0;
                    host.StartCoroutine(CalamityMarkAttack(
                        from,
                        tier.targetCount,
                        tier.extraParam > 0f ? tier.extraParam : 0.15f,
                        chainOnDeath,
                        chainDamage,
                        damage,
                        sourceUnitNumber));
                }
                break;
            case TraitPeriodicAttackDefs.AttackKind.DemonExecution:
                host.StartCoroutine(DemonExecution(from, tier.targetCount, damage, tier.extraParam > 0f ? tier.extraParam : 0.12f, sourceUnitNumber));
                break;
            case TraitPeriodicAttackDefs.AttackKind.RogueShuriken:
                host.StartCoroutine(RogueShuriken(from, tier.targetCount, damage, sourceUnitNumber));
                break;
            case TraitPeriodicAttackDefs.AttackKind.PenguinIceShard:
                host.StartCoroutine(PenguinIce(from, damage, tier.extraParam > 0f ? tier.extraParam : 1.6f, sourceUnitNumber));
                break;
            case TraitPeriodicAttackDefs.AttackKind.FireMeteorShower:
                host.StartCoroutine(FireMeteorShower(from, tier, damage, sourceUnitNumber));
                break;
            case TraitPeriodicAttackDefs.AttackKind.IceFreeze:
                host.StartCoroutine(IceFreezeAttack(from, tier.targetCount, damage, sourceUnitNumber));
                break;
            case TraitPeriodicAttackDefs.AttackKind.LightningChain:
                host.StartCoroutine(LightningChain(from, tier.targetCount, damage, sourceUnitNumber));
                break;
            case TraitPeriodicAttackDefs.AttackKind.NatureVine:
                host.StartCoroutine(NatureVine(from, tier.targetCount, damage, sourceUnitNumber));
                break;
            case TraitPeriodicAttackDefs.AttackKind.WindGust:
                host.StartCoroutine(WindGust(from, tier.targetCount, damage, sourceUnitNumber));
                break;
            case TraitPeriodicAttackDefs.AttackKind.LightBeam:
                LightBeam(from, damage, sourceUnitNumber);
                break;
            case TraitPeriodicAttackDefs.AttackKind.DarkVortex:
                host.StartCoroutine(DarkVortex(from, tier.targetCount, damage, sourceUnitNumber));
                break;
        }
    }

    // ── 마법사 ──
    static void FireMageLaser(MonoBehaviour host, Vector3 from, int targetCount, int damage, int sourceUnitNumber)
    {
        SpawnLaunchFlash(from, new Color(0.4f, 0.65f, 1f, 0.55f), 0.9f);
        Zombie[] targets = PickNearest(from, targetCount);
        for (int i = 0; i < targets.Length; i++)
        {
            Zombie t = targets[i];
            if (t == null) continue;
            GameObject lo = new GameObject("TraitMageLaser");
            Unit20TargetedLaser laser = lo.AddComponent<Unit20TargetedLaser>();
            laser.duration = 0.85f;
            laser.damageInterval = 0.85f;
            laser.beamWidth = 0.24f;
            laser.outerColor = MageOuter;
            laser.innerColor = MageInner;
            laser.sourceUnitNumber = sourceUnitNumber;
            laser.Launch(from + Vector3.up * 0.35f, t.transform.position, t, damage);
        }
    }

    // ── 파수꾼 ──
    static IEnumerator WardenArrowRain(Vector3 from, int count, int damage, int sourceUnitNumber)
    {
        SpawnLaunchFlash(from, WardenGreen, 0.75f);
        Zombie[] targets = PickNearest(from, count);
        for (int i = 0; i < targets.Length; i++)
        {
            Zombie t = targets[i];
            if (t == null) continue;
            yield return SpawnWeaponDrop(t.transform.position, damage, WardenGreen, "unit_018_attack", 1.8f, true, false, sourceUnitNumber);
            yield return new WaitForSeconds(0.1f);
        }
        SpawnRing(from, WardenGreen, 0.2f, 1.4f);
    }

    // ── 도깨비 ──
    static void GoblinHomingMissiles(MonoBehaviour host, Vector3 from, int targetCount, int damage, int sourceUnitNumber)
    {
        MonoBehaviour runner = host != null ? host : TraitPeriodicAttackRunner.Instance;
        if (runner != null) runner.StartCoroutine(GoblinHomingMissilesRoutine(from, targetCount, damage, sourceUnitNumber));
    }

    static IEnumerator GoblinHomingMissilesRoutine(Vector3 from, int targetCount, int damage, int sourceUnitNumber)
    {
        SpawnRing(from, GoblinGold, 0.18f, 1.05f);
        CreateLineBurst("GoblinLaunchFlash", from, from + Vector3.up * 0.55f,
            new Color(1f, 0.9f, 0.35f, 0.65f), 0.16f, 0.18f);
        yield return FireGoblinHomingVolley(from, targetCount, damage, sourceUnitNumber);
    }

    static IEnumerator FireGoblinHomingVolley(Vector3 from, int targetCount, int damage, int sourceUnitNumber)
    {
        int count = Mathf.Clamp(targetCount, 1, 3);
        Zombie[] targets = PickNearest(from, count);
        if (targets.Length == 0) yield break;

        int homingDamage = Mathf.Max(1, Mathf.RoundToInt(damage * 0.55f));
        Vector3 launch = from + Vector3.up * 0.25f;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;
            Vector3 offset = new Vector3((i - 1) * 0.08f, 0f, 0f);
            TraitHomingProjectile.Spawn(launch + offset, targets[i], homingDamage, GoblinGold, 0.26f, sourceUnitNumber);
            yield return new WaitForSeconds(0.07f);
        }
    }

    // ── 기사단 ──
    static IEnumerator KnightWave(Vector3 from, int damage, float widthMul, int sourceUnitNumber)
    {
        SpawnLaunchFlash(from, KnightSilver, 0.85f);
        Zombie primary = PickNearest(from, 1).Length > 0 ? PickNearest(from, 1)[0] : null;
        Vector3 dir = primary != null ? (primary.transform.position - from).normalized : Vector3.right;
        if (dir.sqrMagnitude < 0.01f) dir = Vector3.right;

        float length = 4.5f * widthMul;
        Vector3 end = from + dir * length;
        float halfW = 0.18f * widthMul;
        for (int lane = -1; lane <= 1; lane++)
        {
            Vector3 offset = new Vector3(-dir.y, dir.x, 0f) * lane * halfW;
            CreateLineBurst("KnightWave", from + offset, end + offset, KnightSilver, 0.18f * widthMul, 0.4f);
        }

        float hitHalfW = 0.55f * widthMul;
        Zombie.CopyLivingZombiesTo(_zombieBuffer);
        for (int i = 0; i < _zombieBuffer.Count; i++)
        {
            Zombie z = _zombieBuffer[i];
            if (z == null) continue;
            if (DistanceToSegment(from, end, z.transform.position) <= hitHalfW + 0.35f)
            {
                z.TakeDamage(damage, sourceUnitNumber);
            }
        }
        SpawnRing(end, KnightSilver, 0.15f, 1.1f * widthMul);
        yield return new WaitForSeconds(0.15f);
    }

    // ── 재앙 ──
    static IEnumerator CalamityMarkAttack(
        Vector3 from,
        int count,
        float damageBonus,
        bool chainOnDeath,
        int chainDamage,
        int applyHitDamage,
        int sourceUnitNumber)
    {
        SpawnLaunchFlash(from, new Color(0.55f, 0.12f, 0.45f, 0.55f), 0.85f);
        Zombie[] targets = PickNearest(from, count);
        const float markDuration = 4f;
        for (int i = 0; i < targets.Length; i++)
        {
            Zombie z = targets[i];
            if (z == null) continue;
            Vector3 at = z.transform.position;
            SpawnRing(at, DarkPurple, 0.18f, 1.35f);
            GameObject markBurst = SpawnRing(at, new Color(0.95f, 0.25f, 0.55f, 0.65f), 0.22f, 0.75f);
            if (markBurst != null && TraitPeriodicAttackRunner.Instance != null)
            {
                TraitPeriodicAttackRunner.Instance.StartCoroutine(AnimateScaleFadeRoutine(markBurst, 0.28f, 1.4f));
            }
            z.ApplyCalamityMark(markDuration, damageBonus, chainOnDeath, chainDamage, sourceUnitNumber);
            if (applyHitDamage > 0)
            {
                z.TakeDamage(applyHitDamage, sourceUnitNumber);
            }
            yield return new WaitForSeconds(0.08f);
        }
    }

    public static void ExplodeCalamityMarkChain(Vector3 center, int damage, int sourceUnitNumber)
    {
        const float radius = 1.65f;
        GameObject ring = SpawnRing(center, new Color(0.75f, 0.18f, 0.45f, 0.75f), 0.25f, radius * 1.1f);
        if (ring != null && TraitPeriodicAttackRunner.Instance != null)
        {
            TraitPeriodicAttackRunner.Instance.StartCoroutine(AnimateScaleFadeRoutine(ring, 0.32f, radius * 1.8f));
        }
        AttackHitEffectFire.Spawn(center, Vector3.one * 1.15f, new Color(0.65f, 0.15f, 0.55f, 1f));
        DamageInRadius(center, radius, damage, sourceUnitNumber);
    }

    static IEnumerator CalamityPulse(Vector3 center, int damage, float radius, int sourceUnitNumber)
    {
        GameObject core = SpawnRing(center, new Color(0.25f, 0.05f, 0.35f, 0.7f), 0.15f, radius * 0.8f);
        GameObject ring = SpawnRing(center, DarkPurple, 0.2f, radius * 2f);
        var runner = TraitPeriodicAttackRunner.Instance;
        if (runner != null && core != null) runner.StartCoroutine(SpinObject(core.transform, 420f, 0.45f));
        yield return AnimateScaleFadeRoutine(ring, 0.45f, radius * 2.2f);
        DamageInRadius(center, radius, damage, sourceUnitNumber);
        AttackHitEffectFire.Spawn(center, Vector3.one * (1.2f + radius * 0.2f), new Color(0.5f, 0.15f, 0.65f, 1f));
    }

    // ── 악마 ──
    static IEnumerator DemonExecution(Vector3 from, int count, int damage, float threshold, int sourceUnitNumber)
    {
        SpawnLaunchFlash(from, DemonRed, 0.7f);
        SpawnRing(from, new Color(0.9f, 0.15f, 0.2f, 0.45f), 0.25f, 2f);
        Zombie[] targets = PickNearest(from, count);
        for (int i = 0; i < targets.Length; i++)
        {
            Zombie z = targets[i];
            if (z == null) continue;
            Vector3 pos = z.transform.position;
            CreateLineBurst("DemonSlashA", pos + new Vector3(-0.4f, -0.4f, 0f), pos + new Vector3(0.4f, 0.4f, 0f), DemonRed, 0.14f, 0.22f);
            CreateLineBurst("DemonSlashB", pos + new Vector3(-0.4f, 0.4f, 0f), pos + new Vector3(0.4f, -0.4f, 0f), DemonRed, 0.14f, 0.22f);
            yield return new WaitForSeconds(0.05f);

            if (z.IsBossLike()) { z.TakeDamage(damage, sourceUnitNumber); continue; }
            if (z.maxHealth > 0 && z.health <= z.maxHealth * threshold)
            {
                AttackHitEffectFire.Spawn(pos, Vector3.one * 2f, DemonRed);
                z.TakeDamage(z.health, sourceUnitNumber);
            }
            else
            {
                z.TakeDamage(damage, sourceUnitNumber);
            }
        }
    }

    // ── 도적단 ──
    static IEnumerator RogueShuriken(Vector3 from, int count, int damage, int sourceUnitNumber)
    {
        SpawnLaunchFlash(from, new Color(0.75f, 0.75f, 0.82f, 0.5f), 0.65f);
        int shots = Mathf.Max(2, count);
        HashSet<Zombie> aimed = new HashSet<Zombie>();
        for (int i = 0; i < shots; i++)
        {
            Zombie t = PickNearestUnique(from, aimed);
            if (t == null) yield break;
            aimed.Add(t);
            TraitShurikenProjectile.Spawn(from + Vector3.up * 0.15f, t, damage, new Color(0.85f, 0.85f, 0.92f, 1f), sourceUnitNumber);
            yield return new WaitForSeconds(0.1f);
        }
    }

    // ── 펭귄 ──
    static IEnumerator PenguinIce(Vector3 from, int damage, float radius, int sourceUnitNumber)
    {
        Zombie target = PickNearest(from, 1).Length > 0 ? PickNearest(from, 1)[0] : null;
        if (target == null) yield break;
        Vector3 at = target.transform.position;
        yield return SpawnWeaponDrop(at, damage, IceCyan, "unit_018_attack", 1.6f, false, true, sourceUnitNumber);
        SpawnRing(from, IceCyan, 0.15f, radius * 1.6f);
        DamageInRadius(at, radius * 0.65f, Mathf.Max(1, damage / 2), sourceUnitNumber, z => z.ApplySlow(1.4f));
    }

    // ── 불 (화염 낙하) ──
    const float FireMeteorVfxScale = 1.65f;

    static IEnumerator FireMeteorShower(Vector3 from, TraitPeriodicAttackDefs.TierDef tier, int damage, int sourceUnitNumber)
    {
        int meteorCount = Mathf.Max(2, tier.targetCount);
        float impactRadius = tier.extraParam > 0f ? tier.extraParam : 0.85f;
        bool leaveEmbers = meteorCount >= 4;

        SpawnLaunchFlash(from, FireOrange, 1.15f * FireMeteorVfxScale);
        Zombie primary = PickNearest(from, 1).Length > 0 ? PickNearest(from, 1)[0] : null;
        if (primary == null) yield break;

        Vector3 cluster = primary.transform.position;
        SpawnRing(cluster, new Color(1f, 0.35f, 0.08f, 0.55f), 0.12f * FireMeteorVfxScale, 1.35f * FireMeteorVfxScale);

        for (int i = 0; i < meteorCount; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-1.15f, 1.15f),
                Random.Range(-0.55f, 0.55f),
                0f);
            Vector3 land = cluster + offset;
            yield return SpawnFireMeteor(land, damage, impactRadius, leaveEmbers, sourceUnitNumber);
            yield return new WaitForSeconds(0.11f);
        }
    }

    static IEnumerator SpawnFireMeteor(Vector3 land, int damage, float radius, bool leaveEmbers, int sourceUnitNumber)
    {
        Vector3 start = land + Vector3.up * (2.9f * FireMeteorVfxScale);
        Sprite[] frames = LoadSortedSprites("unit_002_attack");

        GameObject obj = new GameObject("TraitFireMeteor");
        obj.transform.position = start;
        obj.transform.localScale = Vector3.one * (1.15f * FireMeteorVfxScale);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 6;
        if (frames != null && frames.Length > 0)
        {
            renderer.sprite = frames[0];
            renderer.color = new Color(1f, 0.82f, 0.45f, 1f);
        }
        else
        {
            renderer.sprite = MakeDiscSprite(FireOrange);
            renderer.color = FireOrange;
        }
        Unit1ProjectileAfterimage.Attach(obj);

        SpawnRing(land, new Color(1f, 0.28f, 0.05f, 0.42f), 0.06f * FireMeteorVfxScale, radius * 1.5f * FireMeteorVfxScale);

        float fallTime = 0.3f;
        float t = 0f;
        int frameIndex = 0;
        float frameTimer = 0f;
        while (t < fallTime)
        {
            t += Time.deltaTime;
            float eased = Mathf.Pow(Mathf.Clamp01(t / fallTime), 1.35f);
            obj.transform.position = Vector3.Lerp(start, land, eased);

            if (frames != null && frames.Length > 0)
            {
                frameTimer += Time.deltaTime;
                if (frameTimer >= 0.05f)
                {
                    frameTimer = 0f;
                    frameIndex = (frameIndex + 1) % frames.Length;
                    renderer.sprite = frames[frameIndex];
                }
            }
            yield return null;
        }

        Object.Destroy(obj);
        ImpactFireMeteor(land, damage, radius, leaveEmbers, sourceUnitNumber);
    }

    static void ImpactFireMeteor(Vector3 land, int damage, float radius, bool leaveEmbers, int sourceUnitNumber)
    {
        float vfx = FireMeteorVfxScale;
        AttackHitEffectFire.Spawn(land, Vector3.one * ((1.35f + radius * 0.35f) * vfx), FireOrange);
        SpawnRing(land, FireOrange, 0.14f * vfx, radius * 1.9f * vfx);
        CreateLineBurst(
            "TraitMeteorBurst",
            land + Vector3.up * (0.55f * vfx),
            land + Vector3.down * (0.25f * vfx),
            new Color(1f, 0.55f, 0.12f, 0.9f),
            0.24f * vfx,
            0.22f);

        int burnPerTick = Mathf.Max(1, damage / 3);
        DamageInRadius(land, radius, damage, sourceUnitNumber, z => z.ApplyBurn(burnPerTick, 2.5f));

        if (leaveEmbers)
        {
            TraitFireEmberPatch.Spawn(land, 1f, radius * 0.82f, Mathf.Max(1, damage / 4), vfx);
        }
    }

    // ── 얼음 ──
    static IEnumerator IceFreezeAttack(Vector3 from, int count, int damage, int sourceUnitNumber)
    {
        SpawnLaunchFlash(from, IceCyan, 0.8f);
        Zombie[] targets = PickNearest(from, count);
        for (int i = 0; i < targets.Length; i++)
        {
            Zombie z = targets[i];
            if (z == null) continue;
            z.TakeDamage(damage, sourceUnitNumber);
            z.ApplySlow(1.8f);
            if (Random.value < 0.35f) z.ApplyUnit11Freeze(0.8f);
            AttackHitEffectFire.Spawn(z.transform.position, Vector3.one * 1.4f, IceCyan);
            SpawnRing(z.transform.position, IceCyan, 0.12f, 1.1f);
            yield return new WaitForSeconds(0.08f);
        }
    }

    // ── 번개 ──
    static IEnumerator LightningChain(Vector3 from, int chainRounds, int damage, int sourceUnitNumber)
    {
        SpawnLaunchFlash(from, LightningYellow, 0.85f);
        Zombie origin = PickNearest(from, 1).Length > 0 ? PickNearest(from, 1)[0] : null;
        if (origin == null) yield break;

        origin.TakeDamage(damage, sourceUnitNumber);
        SpawnLightningBolt(from + Vector3.up * 0.3f, origin.transform.position);
        SpawnRing(origin.transform.position, LightningYellow, 0.15f, 1.2f);

        Vector3 pos = origin.transform.position;
        HashSet<Zombie> hit = new HashSet<Zombie> { origin };
        for (int r = 0; r < chainRounds; r++)
        {
            Zombie next = PickNearestExcept(pos, hit, 2.6f);
            if (next == null) break;
            hit.Add(next);
            SpawnLightningBolt(pos, next.transform.position);
            next.TakeDamage(damage, sourceUnitNumber);
            SpawnRing(next.transform.position, LightningYellow, 0.1f, 0.9f);
            pos = next.transform.position;
            yield return new WaitForSeconds(0.06f);
        }
    }

    // ── 자연 ──
    static IEnumerator NatureVine(Vector3 from, int count, int damage, int sourceUnitNumber)
    {
        SpawnLaunchFlash(from, NatureGreen, 0.7f);
        Zombie[] targets = PickNearest(from, count);
        for (int i = 0; i < targets.Length; i++)
        {
            Zombie z = targets[i];
            if (z == null) continue;
            CreateLineBurst("TraitVine", from, z.transform.position, NatureGreen, 0.16f, 0.3f);
            z.TakeDamage(damage, sourceUnitNumber);
            z.ApplyStun(0.45f);
            SpawnRing(z.transform.position, NatureGreen, 0.1f, 0.85f);
            yield return new WaitForSeconds(0.1f);
        }
    }

    // ── 바람 ──
    static IEnumerator WindGust(Vector3 from, int count, int damage, int sourceUnitNumber)
    {
        SpawnLaunchFlash(from, WindCyan, 0.9f);
        int shots = Mathf.Max(2, count);
        for (int i = 0; i < shots; i++)
        {
            float yOff = (i - (shots - 1) * 0.5f) * 0.35f;
            Vector3 start = from + new Vector3(0f, yOff, 0f);
            SpawnWindProjectile(start, damage, sourceUnitNumber);
            yield return new WaitForSeconds(0.07f);
        }
    }

    // ── 빛 ──
    static void LightBeam(Vector3 from, int damage, int sourceUnitNumber)
    {
        Zombie target = PickNearest(from, 1).Length > 0 ? PickNearest(from, 1)[0] : null;
        if (target == null) return;
        SpawnLaunchFlash(from, LightGold, 0.95f);
        GameObject beamObj = new GameObject("TraitLightBeam");
        Unit25LaserBeam beam = beamObj.AddComponent<Unit25LaserBeam>();
        beam.duration = 0.9f;
        beam.damageInterval = 0.9f;
        beam.damage = damage;
        beam.sourceUnitNumber = sourceUnitNumber;
        beam.beamWidth = 0.45f;
        beam.outerColor = new Color(1f, 0.92f, 0.45f, 0.55f);
        beam.innerColor = LightGold;
        beam.Launch(from + Vector3.up * 0.2f, target.transform.position);
        SpawnRing(from, LightGold, 0.12f, 1.3f);
    }

    // ── 어둠 ──
    static IEnumerator DarkVortex(Vector3 center, int count, int damage, int sourceUnitNumber)
    {
        SpawnLaunchFlash(center, DarkPurple, 1.1f);
        GameObject disc = SpawnRing(center, DarkPurple, 0.35f, 2.4f);
        var runner = TraitPeriodicAttackRunner.Instance;
        if (runner != null && disc != null) runner.StartCoroutine(SpinObject(disc.transform, -280f, 0.55f));

        float duration = 0.55f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            PullAndDamage(center, 1.8f, damage, 0.35f * Time.deltaTime, sourceUnitNumber);
            yield return null;
        }
        if (disc != null) Object.Destroy(disc);

        Zombie[] extra = PickNearest(center, count);
        for (int i = 0; i < extra.Length; i++)
        {
            if (extra[i] != null)
            {
                extra[i].TakeDamage(Mathf.Max(1, damage / 2), sourceUnitNumber);
                AttackHitEffectFire.Spawn(extra[i].transform.position, Vector3.one * 1.1f, DarkPurple);
            }
        }
    }

    // ── 공용 VFX ──

    static void SpawnLaunchFlash(Vector3 at, Color color, float ringSize)
    {
        SpawnRing(at, color, 0.1f, ringSize);
    }

    static void SpawnLightningBolt(Vector3 from, Vector3 to)
    {
        GameObject go = new GameObject("TraitLightning");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 7;
        lr.startWidth = 0.12f;
        lr.endWidth = 0.04f;
        lr.startColor = LightningYellow;
        lr.endColor = new Color(1f, 1f, 0.7f, 0.3f);

        int segments = 5;
        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Lerp(from, to, t);
            if (i > 0 && i < segments)
            {
                p += new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.2f, 0.2f), 0f);
            }
            lr.SetPosition(i, p);
        }
        var host = TraitPeriodicAttackRunner.Instance;
        if (host != null) host.StartCoroutine(FadeDestroyLine(go, lr, 0.14f));
    }

    static void SpawnWindProjectile(Vector3 from, int damage, int sourceUnitNumber)
    {
        GameObject projObj = new GameObject("TraitWindProjectile");
        projObj.transform.position = from;
        SpriteRenderer sr = projObj.AddComponent<SpriteRenderer>();
        sr.sprite = MakeDiscSprite(WindCyan);
        sr.color = new Color(0.85f, 1f, 1f, 0.75f);
        sr.sortingOrder = 5;
        projObj.transform.localScale = Vector3.one * 0.4f;
        StraightProjectile proj = projObj.AddComponent<StraightProjectile>();
        proj.direction = Vector2.right;
        proj.speed = 16f;
        proj.damage = damage;
        proj.sourceUnitNumber = sourceUnitNumber;
        Unit1ProjectileAfterimage.Attach(projObj);
    }

    static IEnumerator SpawnWeaponDrop(
        Vector3 at, int damage, Color tint, string resourcePath, float scale,
        bool applyStun = false, bool iceTint = false, int sourceUnitNumber = 0)
    {
        Sprite[] frames = LoadSortedSprites(resourcePath);
        if (frames == null || frames.Length == 0)
        {
            yield return SpawnSimpleDrop(at, damage, tint, scale, sourceUnitNumber);
            yield break;
        }

        GameObject obj = new GameObject("TraitWeaponDrop");
        obj.transform.position = at + Vector3.up * 2f;
        obj.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 5;
        renderer.sprite = frames[0];
        renderer.color = iceTint ? tint : Color.Lerp(Color.white, tint, 0.35f);

        Vector3 land = at;
        float fallTime = 0.2f;
        float t = 0f;
        int animIndex = 0;
        float animTimer = 0f;
        int lastIndex = Mathf.Min(7, frames.Length - 1);

        while (t < fallTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fallTime);
            obj.transform.position = Vector3.Lerp(at + Vector3.up * 2f, land, p);
            animTimer += Time.deltaTime;
            if (animTimer >= ArrowFrameTime)
            {
                animTimer = 0f;
                animIndex = Mathf.Min(lastIndex, animIndex + 1);
                renderer.sprite = frames[animIndex];
            }
            yield return null;
        }

        SpawnRing(land, tint, 0.12f, 0.95f);
        Zombie nearest = PickNearest(land, 1).Length > 0 ? PickNearest(land, 1)[0] : null;
        if (nearest != null && Vector2.Distance(nearest.transform.position, land) < 1f)
        {
            nearest.TakeDamage(damage, sourceUnitNumber);
            if (applyStun) nearest.ApplyStun(0.5f);
            AttackHitEffectFire.Spawn(land, Vector3.one * 1.2f, tint);
        }
        Object.Destroy(obj);
    }

    static IEnumerator SpawnSimpleDrop(Vector3 at, int damage, Color tint, float scale, int sourceUnitNumber = 0)
    {
        GameObject arrowObj = new GameObject("TraitDrop");
        arrowObj.transform.position = at + Vector3.up * 1.8f;
        arrowObj.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = arrowObj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 5;
        renderer.sprite = MakeDiscSprite(tint);
        Vector3 land = at;
        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.deltaTime;
            arrowObj.transform.position = Vector3.Lerp(at + Vector3.up * 1.8f, land, t / 0.22f);
            yield return null;
        }
        SpawnRing(land, tint, 0.12f, 0.9f);
        Zombie nearest = PickNearest(land, 1).Length > 0 ? PickNearest(land, 1)[0] : null;
        if (nearest != null && Vector2.Distance(nearest.transform.position, land) < 0.8f)
            nearest.TakeDamage(damage, sourceUnitNumber);
        Object.Destroy(arrowObj);
    }

    static IEnumerator SpinObject(Transform target, float degPerSec, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && target != null)
        {
            elapsed += Time.deltaTime;
            target.Rotate(0f, 0f, degPerSec * Time.deltaTime);
            yield return null;
        }
    }

    static Sprite[] LoadSortedSprites(string path)
    {
        Sprite[] frames = Resources.LoadAll<Sprite>(path);
        if (frames == null || frames.Length == 0) return null;
        System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
        return frames;
    }

    static Zombie PickNearestUnique(Vector3 from, HashSet<Zombie> exclude)
    {
        Zombie.CopyLivingZombiesTo(_zombieBuffer);
        _zombieBuffer.Sort((a, b) =>
        {
            float da = Vector2.SqrMagnitude((Vector2)(a.transform.position - from));
            float db = Vector2.SqrMagnitude((Vector2)(b.transform.position - from));
            return da.CompareTo(db);
        });
        for (int i = 0; i < _zombieBuffer.Count; i++)
        {
            if (!_zombieBuffer[i].IsExcludedFromCombat && !exclude.Contains(_zombieBuffer[i]))
                return _zombieBuffer[i];
        }
        return null;
    }

    static Zombie PickNearestExcept(Vector3 from, HashSet<Zombie> exclude, float maxDist)
    {
        Zombie best = null;
        float bestDist = maxDist;
        Zombie.CopyLivingZombiesTo(_zombieBuffer);
        for (int i = 0; i < _zombieBuffer.Count; i++)
        {
            Zombie z = _zombieBuffer[i];
            if (z == null || exclude.Contains(z)) continue;
            float d = Vector2.Distance(from, z.transform.position);
            if (d < bestDist) { bestDist = d; best = z; }
        }
        return best;
    }

    static void PullAndDamage(Vector3 center, float radius, int damage, float pullStrength, int sourceUnitNumber)
    {
        Zombie.CopyLivingZombiesTo(_zombieBuffer);
        for (int i = 0; i < _zombieBuffer.Count; i++)
        {
            Zombie z = _zombieBuffer[i];
            if (z == null) continue;
            Vector3 pos = z.transform.position;
            if (Vector2.Distance(center, pos) > radius) continue;
            z.transform.position = pos + (center - pos).normalized * pullStrength;
            if (Random.value < 0.08f) z.TakeDamage(damage, sourceUnitNumber);
        }
    }

    static void DamageInRadius(Vector3 center, float radius, int damage, int sourceUnitNumber, System.Action<Zombie> extra = null)
    {
        Zombie.CopyLivingZombiesTo(_zombieBuffer);
        for (int i = 0; i < _zombieBuffer.Count; i++)
        {
            Zombie z = _zombieBuffer[i];
            if (z == null) continue;
            if (Vector2.Distance(center, z.transform.position) <= radius)
            {
                z.TakeDamage(damage, sourceUnitNumber);
                extra?.Invoke(z);
            }
        }
    }

    static Zombie[] PickNearest(Vector3 from, int count)
    {
        count = Mathf.Max(1, count);
        Zombie.CopyLivingZombiesTo(_zombieBuffer);
        _zombieBuffer.Sort((a, b) =>
        {
            float da = Vector2.SqrMagnitude((Vector2)(a.transform.position - from));
            float db = Vector2.SqrMagnitude((Vector2)(b.transform.position - from));
            return da.CompareTo(db);
        });
        int n = Mathf.Min(count, _zombieBuffer.Count);
        Zombie[] result = new Zombie[n];
        for (int i = 0; i < n; i++) result[i] = _zombieBuffer[i];
        return result;
    }

    static GameObject SpawnRing(Vector3 center, Color color, float startScale, float endScale)
    {
        GameObject ring = new GameObject("TraitRing");
        ring.transform.position = center;
        SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
        sr.sprite = MakeRingSprite(color);
        sr.sortingOrder = 4;
        ring.transform.localScale = Vector3.one * startScale;
        AnimateScaleFade(ring, 0.4f, endScale);
        return ring;
    }

    static void AnimateScaleFade(GameObject obj, float duration, float endScale)
    {
        if (obj == null) return;
        var host = TraitPeriodicAttackRunner.Instance;
        if (host != null) host.StartCoroutine(AnimateScaleFadeRoutine(obj, duration, endScale));
    }

    static IEnumerator AnimateScaleFadeRoutine(GameObject obj, float duration, float endScale)
    {
        SpriteRenderer sr = obj != null ? obj.GetComponent<SpriteRenderer>() : null;
        Vector3 start = obj.transform.localScale;
        Vector3 end = Vector3.one * endScale;
        Color c = sr != null ? sr.color : Color.white;
        float t = 0f;
        while (t < duration && obj != null)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            obj.transform.localScale = Vector3.Lerp(start, end, p);
            if (sr != null) sr.color = new Color(c.r, c.g, c.b, c.a * (1f - p));
            yield return null;
        }
        if (obj != null) Object.Destroy(obj);
    }

    static void CreateLineBurst(string name, Vector3 a, Vector3 b, Color color, float width, float lifetime)
    {
        GameObject go = new GameObject(name);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startWidth = width;
        lr.endWidth = width * 0.6f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = new Color(color.r, color.g, color.b, color.a * 0.2f);
        lr.sortingOrder = 6;
        var host = TraitPeriodicAttackRunner.Instance;
        if (host != null) host.StartCoroutine(FadeDestroyLine(go, lr, lifetime));
    }

    static IEnumerator FadeDestroyLine(GameObject go, LineRenderer lr, float lifetime)
    {
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

    static float DistanceToSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        float len = ab.sqrMagnitude;
        if (len < 0.0001f) return Vector2.Distance(a, p);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len);
        return Vector2.Distance(a + ab * t, p);
    }

    static Sprite _disc;
    static Sprite _ring;

    static Sprite MakeDiscSprite(Color color)
    {
        if (_disc == null)
        {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / (size * 0.5f);
                    tex.SetPixel(x, y, d <= 1f ? Color.white : Color.clear);
                }
            tex.Apply();
            _disc = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
        return _disc;
    }

    static Sprite MakeRingSprite(Color color)
    {
        if (_ring == null)
        {
            const int size = 48;
            Texture2D tex = new Texture2D(size, size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / (size * 0.5f);
                    tex.SetPixel(x, y, (d > 0.55f && d < 0.95f) ? Color.white : Color.clear);
                }
            tex.Apply();
            _ring = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
        return _ring;
    }
}
