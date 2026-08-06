using UnityEngine;

/// <summary>
/// Casual Game Sounds U6 기반 게임 SFX (Resources/GameSfx).
/// </summary>
public static class GameSfxPlayer
{
    const string SfxResourceRoot = "GameSfx/";
    const float UnitCardVolume = 0.72f;
    const float EnemyHitVolume = 0.38f;
    const float AllyAttackVolume = 0.42f;
    const float EnemyHitMinInterval = 0.045f;

    static AudioSource source;
    static AudioClip unitCardReveal;
    static AudioClip enemyHitSmall;
    static AudioClip allyProjectileSmall;
    static AudioClip allyMeleeSmall;
    static float lastEnemyHitTime = -999f;
    static bool clipsLoaded;

    public static void PlayUnitCardReveal()
    {
        EnsureLoaded();
        PlayOneShot(unitCardReveal, UnitCardVolume, ignorePause: true);
    }

    public static void PlayEnemyHitSmall()
    {
        if (Time.time - lastEnemyHitTime < EnemyHitMinInterval) return;
        lastEnemyHitTime = Time.time;
        EnsureLoaded();
        PlayOneShot(enemyHitSmall, EnemyHitVolume);
    }

    public static void PlayAllyProjectileAttack()
    {
        EnsureLoaded();
        PlayOneShot(allyProjectileSmall, AllyAttackVolume);
    }

    public static void PlayAllyNonProjectileAttack()
    {
        EnsureLoaded();
        PlayOneShot(allyMeleeSmall, AllyAttackVolume);
    }

    static void EnsureLoaded()
    {
        if (clipsLoaded && source != null) return;

        if (source == null)
        {
            GameObject host = new GameObject("GameSfxPlayer");
            Object.DontDestroyOnLoad(host);
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        unitCardReveal = LoadClip("DM-CGS-18");
        enemyHitSmall = LoadClip("DM-CGS-39");
        allyProjectileSmall = LoadClip("DM-CGS-46");
        allyMeleeSmall = LoadClip("DM-CGS-07");
        clipsLoaded = true;
    }

    static AudioClip LoadClip(string fileName)
    {
        AudioClip clip = Resources.Load<AudioClip>(SfxResourceRoot + fileName);
        if (clip == null)
        {
            Debug.LogWarning($"GameSfxPlayer: 클립을 찾지 못했습니다 — Resources/{SfxResourceRoot}{fileName}");
        }
        return clip;
    }

    static void PlayOneShot(AudioClip clip, float volume, bool ignorePause = false)
    {
        if (clip == null || source == null) return;
        if (!ignorePause && IsGameplayPaused()) return;

        float master = AudioListener.volume;
        source.pitch = Random.Range(0.96f, 1.04f);
        source.PlayOneShot(clip, Mathf.Clamp01(volume * master));
    }

    static bool IsGameplayPaused()
    {
        return GameManager.Instance != null && GameManager.Instance.IsCombatPaused();
    }
}
