using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특수 스킬 선택 UI 관리
/// </summary>
public class SkillSelectionManager : MonoBehaviour
{
    private Text silverText;
    private Text selectedSlotText;
    private Text skillInfoText;
    
    private Button unlockButton;
    private Button upgradeButton;
    private Button equipButton;
    
    private List<Button> skillButtons = new List<Button>();
    private List<Text> skillButtonTexts = new List<Text>();
    private Button[] slotButtons = new Button[3];
    private Text[] slotTexts = new Text[3];
    
    private int selectedSlotIndex = 0;
    private int selectedSkillId = 1;
    
    public void BuildUI()
    {
        // 타이틀
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "특수 스킬";
        titleText.font = UIFontProvider.Get();
        titleText.fontSize = 36;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.UpperCenter;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1);
        titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -70);
        titleRect.sizeDelta = new Vector2(300, 60);
        
        // 실버 텍스트
        GameObject silverObj = new GameObject("SilverText");
        silverObj.transform.SetParent(transform, false);
        silverText = silverObj.AddComponent<Text>();
        silverText.text = "실버 0";
        silverText.font = UIFontProvider.Get();
        silverText.fontSize = 24;
        silverText.color = Color.white;
        silverText.alignment = TextAnchor.UpperRight;
        
        RectTransform silverRect = silverObj.GetComponent<RectTransform>();
        silverRect.anchorMin = new Vector2(1, 1);
        silverRect.anchorMax = new Vector2(1, 1);
        silverRect.pivot = new Vector2(1, 1);
        silverRect.anchoredPosition = new Vector2(-20, -20);
        silverRect.sizeDelta = new Vector2(200, 40);
        
        // 선택 슬롯 텍스트
        GameObject selectedObj = new GameObject("SelectedSlotText");
        selectedObj.transform.SetParent(transform, false);
        selectedSlotText = selectedObj.AddComponent<Text>();
        selectedSlotText.text = "선택 슬롯: 1";
        selectedSlotText.font = UIFontProvider.Get();
        selectedSlotText.fontSize = 22;
        selectedSlotText.color = Color.white;
        selectedSlotText.alignment = TextAnchor.UpperLeft;
        
        RectTransform selectedRect = selectedObj.GetComponent<RectTransform>();
        selectedRect.anchorMin = new Vector2(0, 1);
        selectedRect.anchorMax = new Vector2(0, 1);
        selectedRect.pivot = new Vector2(0, 1);
        selectedRect.anchoredPosition = new Vector2(20, -80);
        selectedRect.sizeDelta = new Vector2(250, 40);
        
        // 슬롯 버튼 3개
        for (int i = 0; i < 3; i++)
        {
            GameObject slotObj = new GameObject($"Slot_{i}");
            slotObj.transform.SetParent(transform, false);
            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 1);
            slotRect.anchorMax = new Vector2(0.5f, 1);
            slotRect.pivot = new Vector2(0.5f, 1);
            slotRect.sizeDelta = new Vector2(160, 60);
            slotRect.anchoredPosition = new Vector2(-180 + i * 180, -120);
            
            Image slotImage = slotObj.AddComponent<Image>();
            slotImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            Button slotButton = slotObj.AddComponent<Button>();
            int idx = i;
            slotButton.onClick.AddListener(() => { selectedSlotIndex = idx; RefreshUI(); });
            slotButtons[i] = slotButton;
            SimpleUIPackTheme.ApplySmallButton(slotButton);
            
            GameObject slotTextObj = new GameObject("Text");
            slotTextObj.transform.SetParent(slotObj.transform, false);
            Text slotText = slotTextObj.AddComponent<Text>();
            slotText.text = "빈칸";
            slotText.font = UIFontProvider.Get();
            slotText.fontSize = 20;
            slotText.color = Color.white;
            slotText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform slotTextRect = slotTextObj.GetComponent<RectTransform>();
            slotTextRect.anchorMin = Vector2.zero;
            slotTextRect.anchorMax = Vector2.one;
            slotTextRect.offsetMin = Vector2.zero;
            slotTextRect.offsetMax = Vector2.zero;
            slotTexts[i] = slotText;
        }
        
        // 스킬 리스트
        float listStartY = -200f;
        List<SkillData.SkillInfo> skills = SkillData.GetAll();
        for (int i = 0; i < skills.Count; i++)
        {
            SkillData.SkillInfo info = skills[i];
            GameObject skillBtnObj = new GameObject($"Skill_{info.id}");
            skillBtnObj.transform.SetParent(transform, false);
            
            RectTransform skillRect = skillBtnObj.AddComponent<RectTransform>();
            skillRect.anchorMin = new Vector2(0, 1);
            skillRect.anchorMax = new Vector2(0, 1);
            skillRect.pivot = new Vector2(0, 1);
            skillRect.sizeDelta = new Vector2(320, 70);
            skillRect.anchoredPosition = new Vector2(20, listStartY - i * 80);
            
            Image skillImage = skillBtnObj.AddComponent<Image>();
            skillImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            
            Button skillButton = skillBtnObj.AddComponent<Button>();
            skillButtons.Add(skillButton);
            skillButton.onClick.AddListener(() => { selectedSkillId = info.id; RefreshUI(); });
            SimpleUIPackTheme.ApplyPrimaryButton(skillButton);
            
            GameObject skillTextObj = new GameObject("Text");
            skillTextObj.transform.SetParent(skillBtnObj.transform, false);
            Text skillText = skillTextObj.AddComponent<Text>();
            skillText.text = info.name;
            skillText.font = UIFontProvider.Get();
            skillText.fontSize = 20;
            skillText.color = Color.white;
            skillText.alignment = TextAnchor.MiddleLeft;
            
            RectTransform skillTextRect = skillTextObj.GetComponent<RectTransform>();
            skillTextRect.anchorMin = Vector2.zero;
            skillTextRect.anchorMax = Vector2.one;
            skillTextRect.offsetMin = new Vector2(10, 0);
            skillTextRect.offsetMax = new Vector2(-10, 0);
            skillButtonTexts.Add(skillText);
        }
        
        // 스킬 정보 패널
        GameObject infoObj = new GameObject("SkillInfo");
        infoObj.transform.SetParent(transform, false);
        Image infoImage = infoObj.AddComponent<Image>();
        infoImage.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        SimpleUIPackTheme.ApplyPopupBackground(infoImage);
        
        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(1, 1);
        infoRect.anchorMax = new Vector2(1, 1);
        infoRect.pivot = new Vector2(1, 1);
        infoRect.anchoredPosition = new Vector2(-20, -200);
        infoRect.sizeDelta = new Vector2(360, 260);
        
        GameObject infoTextObj = new GameObject("InfoText");
        infoTextObj.transform.SetParent(infoObj.transform, false);
        skillInfoText = infoTextObj.AddComponent<Text>();
        skillInfoText.text = "";
        skillInfoText.font = UIFontProvider.Get();
        skillInfoText.fontSize = 20;
        skillInfoText.color = Color.white;
        skillInfoText.alignment = TextAnchor.UpperLeft;
        
        RectTransform infoTextRect = infoTextObj.GetComponent<RectTransform>();
        infoTextRect.anchorMin = Vector2.zero;
        infoTextRect.anchorMax = Vector2.one;
        infoTextRect.offsetMin = new Vector2(10, 10);
        infoTextRect.offsetMax = new Vector2(-10, -10);
        
        // 버튼들
        equipButton = CreateActionButton(transform, "장착", new Vector2(200, -500));
        unlockButton = CreateActionButton(transform, "해제(1실버)", new Vector2(0, -500));
        upgradeButton = CreateActionButton(transform, "업그레이드(1실버)", new Vector2(-200, -500));
        
        equipButton.onClick.AddListener(OnEquipClicked);
        unlockButton.onClick.AddListener(OnUnlockClicked);
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        
        Button closeButton = CreateActionButton(transform, "닫기", new Vector2(0, -560));
        closeButton.onClick.AddListener(Close);
        
        RefreshUI();
        Close();
    }
    
    Button CreateActionButton(Transform parent, string text, Vector2 position)
    {
        GameObject buttonObj = new GameObject(text);
        buttonObj.transform.SetParent(parent, false);
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(200, 50);
        rect.anchoredPosition = position;
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.6f, 0.9f, 1f);
        
        Button button = buttonObj.AddComponent<Button>();
        SimpleUIPackTheme.ApplyPrimaryButton(button);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text textComp = textObj.AddComponent<Text>();
        textComp.text = text;
        textComp.font = UIFontProvider.Get();
        textComp.fontSize = 20;
        textComp.color = Color.white;
        textComp.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return button;
    }
    
    void OnEquipClicked()
    {
        if (!SkillProgressData.IsUnlocked(selectedSkillId)) return;
        SkillSelectionData.SelectedSkills[selectedSlotIndex] = selectedSkillId;
        RefreshUI();
    }
    
    void OnUnlockClicked()
    {
        if (SkillProgressData.TryUnlock(selectedSkillId, GameManager.Instance))
        {
            RefreshUI();
        }
    }
    
    void OnUpgradeClicked()
    {
        if (SkillProgressData.TryUpgrade(selectedSkillId, GameManager.Instance))
        {
            RefreshUI();
        }
    }
    
    public void Open()
    {
        gameObject.SetActive(true);
        RefreshUI();
    }
    
    public void Close()
    {
        gameObject.SetActive(false);
    }
    
    void RefreshUI()
    {
        if (GameManager.Instance != null)
        {
            silverText.text = $"실버 {GameManager.Instance.silverCoins}";
        }
        
        selectedSlotText.text = $"선택 슬롯: {selectedSlotIndex + 1}";
        
        for (int i = 0; i < 3; i++)
        {
            int id = SkillSelectionData.SelectedSkills[i];
            if (id > 0)
            {
                var info = SkillData.GetById(id);
                slotTexts[i].text = info != null ? info.name : $"스킬 {id}";
            }
            else
            {
                slotTexts[i].text = "빈칸";
            }
            
            Image img = slotButtons[i].GetComponent<Image>();
            img.color = (i == selectedSlotIndex) ? new Color(0.3f, 0.6f, 1f, 1f) : new Color(0.2f, 0.2f, 0.2f, 1f);
        }
        
        List<SkillData.SkillInfo> skills = SkillData.GetAll();
        for (int i = 0; i < skills.Count; i++)
        {
            var info = skills[i];
            bool unlocked = SkillProgressData.IsUnlocked(info.id);
            int level = SkillProgressData.GetUpgradeLevel(info.id);
            skillButtonTexts[i].text = $"{info.name} {(unlocked ? $"(해제됨 Lv{level})" : "(잠김)")}";
            
            Image img = skillButtons[i].GetComponent<Image>();
            img.color = (info.id == selectedSkillId) ? new Color(0.2f, 0.6f, 1f, 1f) : new Color(0.15f, 0.15f, 0.15f, 1f);
        }
        
        SkillData.SkillInfo selectedInfo = SkillData.GetById(selectedSkillId);
        if (selectedInfo != null)
        {
            bool unlocked = SkillProgressData.IsUnlocked(selectedInfo.id);
            int level = SkillProgressData.GetUpgradeLevel(selectedInfo.id);
            skillInfoText.text =
                $"{selectedInfo.name}\n\n{selectedInfo.description}\n\n" +
                $"상태: {(unlocked ? "해제됨" : "잠김")}\n" +
                $"업그레이드: Lv{level}";
        }
        
        unlockButton.interactable = !SkillProgressData.IsUnlocked(selectedSkillId);
        upgradeButton.interactable = SkillProgressData.IsUnlocked(selectedSkillId);
        equipButton.interactable = SkillProgressData.IsUnlocked(selectedSkillId);
    }
}


