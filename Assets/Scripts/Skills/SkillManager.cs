using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 특수 스킬 관리
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    public Button[] skillButtons;
    public Image[] skillImages;
    public Text[] skillTexts;
    public bool[] skillEnabled;
    public int[] slotSkillIds;
    Image[] skillArmedFrames;

    SkillRangePreview rangePreview;
    readonly HashSet<int> usedSkillIdsThisRound = new HashSet<int>();

    int armedIndex = -1;
    readonly Color armedColor = new Color(0.96f, 0.62f, 0.18f, 1f);

    public bool IsSkillArmed => armedIndex >= 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        GameObject previewObj = new GameObject("SkillRangePreview");
        previewObj.transform.SetParent(transform, false);
        rangePreview = previewObj.AddComponent<SkillRangePreview>();
        rangePreview.Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (armedIndex >= 0)
        {
            UpdateRangePreview();

            if (WasFieldClick(out Vector3 worldPos))
            {
                if (CanUseSkill(armedIndex))
                {
                    ActivateSkill(armedIndex, worldPos);
                }

                SetArmed(-1);
            }
        }
        else
        {
            rangePreview?.Hide();
        }
    }

    public bool IsOnCooldown(int skillId)
    {
        return skillId > 0 && usedSkillIdsThisRound.Contains(skillId);
    }

    public void ResetRoundCooldowns()
    {
        usedSkillIdsThisRound.Clear();
        SetArmed(-1);
        RefreshSkillEnabledFromCooldown();
    }

    public void RegisterSkills(Button[] buttons, Image[] images, Text[] texts, int[] skillIds)
    {
        RegisterSkills(buttons, images, texts, skillIds, null);
    }

    public void RegisterSkills(Button[] buttons, Image[] images, Text[] texts, int[] skillIds, Image[] armedFrames)
    {
        skillButtons = buttons ?? System.Array.Empty<Button>();
        skillImages = images ?? System.Array.Empty<Image>();
        skillTexts = texts;
        slotSkillIds = skillIds ?? System.Array.Empty<int>();
        skillArmedFrames = armedFrames;
        skillEnabled = new bool[skillButtons.Length];
        armedIndex = -1;
        rangePreview?.Hide();

        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] == null) continue;

            skillButtons[i].onClick.RemoveAllListeners();
            int idx = i;
            skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(idx));

            int skillId = i < slotSkillIds.Length ? slotSkillIds[i] : 0;
            skillEnabled[i] = skillId > 0 && !IsOnCooldown(skillId);

            if (skillTexts != null && i < skillTexts.Length && skillTexts[i] != null)
            {
                if (skillId > 0)
                {
                    var info = SkillData.GetById(skillId);
                    skillTexts[i].text = info != null ? info.name : $"스킬 {skillId}";
                }
                else
                {
                    skillTexts[i].text = "빈칸";
                }
            }

            if (skillImages != null && i < skillImages.Length && skillImages[i] != null)
            {
                skillImages[i].color = Color.white;
            }

            if (skillArmedFrames != null && i < skillArmedFrames.Length && skillArmedFrames[i] != null)
            {
                skillArmedFrames[i].enabled = false;
            }
        }
    }

    public void SyncArmedVisuals()
    {
        if (skillImages == null) return;
        for (int i = 0; i < skillImages.Length; i++)
        {
            bool enabled = skillEnabled != null && i < skillEnabled.Length && skillEnabled[i];
            if (!enabled) continue;

            if (skillArmedFrames != null && i < skillArmedFrames.Length && skillArmedFrames[i] != null)
            {
                skillArmedFrames[i].enabled = i == armedIndex;
            }
            else if (skillImages[i] != null)
            {
                skillImages[i].color = i == armedIndex ? armedColor : Color.white;
            }
        }
    }

    void OnSkillButtonClicked(int index)
    {
        if (!CanUseSkill(index))
        {
            return;
        }

        if (armedIndex == index)
        {
            SetArmed(-1);
        }
        else
        {
            SetArmed(index);
        }
    }

    void SetArmed(int index)
    {
        if (index >= 0 && !CanUseSkill(index))
        {
            index = -1;
        }

        armedIndex = index;

        if (armedIndex >= 0 && rangePreview != null && slotSkillIds != null && armedIndex < slotSkillIds.Length)
        {
            int skillId = slotSkillIds[armedIndex];
            rangePreview.Show(skillId);
            rangePreview.SetWorldPosition(GetPointerWorldPosition());
        }
        else
        {
            rangePreview?.Hide();
        }

        SyncArmedVisuals();
    }

    void UpdateRangePreview()
    {
        if (rangePreview == null || slotSkillIds == null || armedIndex < 0 || armedIndex >= slotSkillIds.Length)
        {
            return;
        }

        int skillId = slotSkillIds[armedIndex];
        if (skillId <= 0)
        {
            rangePreview.Hide();
            return;
        }

        rangePreview.SetWorldPosition(GetPointerWorldPosition());
    }

    Vector3 GetPointerWorldPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return ScreenToWorld(Mouse.current.position.ReadValue());
        }
#endif
        return ScreenToWorld(Input.mousePosition);
    }

    bool CanUseSkill(int index)
    {
        if (skillEnabled == null || index < 0 || index >= skillEnabled.Length) return false;
        if (slotSkillIds == null || index >= slotSkillIds.Length) return false;

        int skillId = slotSkillIds[index];
        return skillId > 0 && !IsOnCooldown(skillId);
    }

    void RefreshSkillEnabledFromCooldown()
    {
        if (skillEnabled == null || slotSkillIds == null) return;

        for (int i = 0; i < skillEnabled.Length; i++)
        {
            int skillId = i < slotSkillIds.Length ? slotSkillIds[i] : 0;
            skillEnabled[i] = skillId > 0 && !IsOnCooldown(skillId);
        }

        SyncArmedVisuals();
    }

    bool WasFieldClick(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI()) return false;
            Vector3 screenPos = Mouse.current.position.ReadValue();
            worldPos = ScreenToWorld(screenPos);
            return true;
        }
#endif
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI()) return false;
            Vector3 screenPos = Input.mousePosition;
            worldPos = ScreenToWorld(screenPos);
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            if (IsPointerOverUI()) return false;
            Vector2 screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            worldPos = ScreenToWorld(screenPos);
            return true;
        }
#endif
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                if (IsPointerOverUI(touch.fingerId)) return false;
                Vector2 screenPos = touch.position;
                worldPos = ScreenToWorld(screenPos);
                return true;
            }
        }

        return false;
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    bool IsPointerOverUI(int pointerId)
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(pointerId);
    }

    Vector3 ScreenToWorld(Vector3 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;
        screenPos.z = -cam.transform.position.z;
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;
        return worldPos;
    }

    void ActivateSkill(int index, Vector3 worldPos)
    {
        if (slotSkillIds == null || index >= slotSkillIds.Length) return;
        int skillId = slotSkillIds[index];
        if (skillId <= 0 || IsOnCooldown(skillId)) return;

        usedSkillIdsThisRound.Add(skillId);
        RefreshSkillEnabledFromCooldown();

        SkillEffectVisuals.Play(skillId, worldPos, this);

        if (skillId == 1)
        {
            FreezeAllZombies(3f);
        }
        else if (skillId == 2)
        {
            FireHell(skillId);
        }
        else if (skillId == 3)
        {
            BlackHole(worldPos, 2f, 1f);
        }
        else if (skillId == 4)
        {
            LightningStrike(worldPos, 2f, skillId);
        }
    }

    void FreezeAllZombies(float duration)
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        foreach (Zombie zombie in zombies)
        {
            if (zombie != null && !zombie.IsExcludedFromCombat)
            {
                zombie.ApplyStun(duration);
            }
        }
    }

    void FireHell(int skillId)
    {
        int sourceUnitNumber = UnitDamageAttribution.ForTraitSkill(skillId);
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        foreach (Zombie zombie in zombies)
        {
            if (zombie != null && !zombie.IsExcludedFromCombat)
            {
                zombie.TakeDamage(2, sourceUnitNumber);
            }
        }
    }

    void LightningStrike(Vector3 center, float radius, int skillId)
    {
        int sourceUnitNumber = UnitDamageAttribution.ForTraitSkill(skillId);
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            float dist = Vector2.Distance(center, zombie.transform.position);
            if (dist <= radius)
            {
                zombie.TakeDamage(3, sourceUnitNumber);
            }
        }
    }

    void BlackHole(Vector3 center, float radius, float duration)
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || zombie.IsExcludedFromCombat) continue;
            float dist = Vector2.Distance(center, zombie.transform.position);
            if (dist <= radius)
            {
                StartCoroutine(PullZombie(zombie, center, duration));
            }
        }
    }

    IEnumerator PullZombie(Zombie zombie, Vector3 center, float duration)
    {
        float startTime = Time.time;
        Vector3 startPos = zombie.transform.position;
        while (Time.time - startTime < duration && zombie != null)
        {
            float t = (Time.time - startTime) / duration;
            zombie.transform.position = Vector3.Lerp(startPos, center, t);
            yield return null;
        }
    }
}
