using System.Collections.Generic;

/// <summary>
/// 특수 스킬 데이터
/// </summary>
public static class SkillData
{
    public class SkillInfo
    {
        public int id;
        public string name;
        public string description;
        public bool unlockedByDefault;
        
        public SkillInfo(int id, string name, string description, bool unlockedByDefault)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.unlockedByDefault = unlockedByDefault;
        }
    }
    
    private static List<SkillInfo> skills = new List<SkillInfo>
    {
        new SkillInfo(1, "빙결", "전체 적 3초 기절 (얼음 조합 2개 이상)", true),
        new SkillInfo(2, "화염 지옥", "전체 적 2 데미지 (불 조합 2개 이상)", false),
        new SkillInfo(3, "블랙홀", "범위 내 적 1초 끌어당김 (어둠 조합 2개 이상)", false),
        new SkillInfo(4, "번개", "범위 내 3 데미지 (번개 조합 2개 이상)", false)
    };
    
    public static List<SkillInfo> GetAll()
    {
        return skills;
    }
    
    public static SkillInfo GetById(int id)
    {
        foreach (var s in skills)
        {
            if (s.id == id) return s;
        }
        return null;
    }
}


