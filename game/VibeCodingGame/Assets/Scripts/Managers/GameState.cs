// 씬 간 데이터를 전달하는 전역 상태 (씬이 바뀌어도 유지됨)
public static class GameState
{
    public static CharacterData CurrentCharacter;
    public static string LoadedCharacterId;
    public static int  Gold         = 0;
    public static int  DungeonCount = 0;
    public static int  MonsterCount = 0;
    public static int  BossCount    = 0;
    public static bool StoryUpdated   = false;
    public static int  Xp             = 0;
    public static int  Level          = 1;
    public static bool[] MissionRewarded = new bool[4]; // 씬 이동해도 수령 여부 유지

    // 캐릭터 선택/생성 시 호출. 다른 캐릭터로 전환되면 이전 캐릭터의
    // 잔여 골드/XP/퀘스트 진행도가 섞여 들어가지 않도록 상태를 완전히 초기화한다.
    public static void LoadCharacter(CharacterData ch)
    {
        CurrentCharacter = ch;
        if (LoadedCharacterId == ch.id) return;

        LoadedCharacterId = ch.id;
        Gold         = ch.gold;
        Xp           = ch.xp;
        Level        = LevelFromXp(Xp);
        DungeonCount = ch.questProgress?.dungeonCount ?? 0;
        MonsterCount = ch.questProgress?.monsterCount ?? 0;
        BossCount    = ch.questProgress?.bossCount    ?? 0;
        StoryUpdated     = false;
        MissionRewarded  = new bool[4];
    }

    public static int TotalXpForLevel(int level)
    {
        int total = 0;
        for (int l = 1; l < level; l++) total += l * 100;
        return total;
    }

    public static int LevelFromXp(int totalXp)
    {
        int lv = 1;
        while (totalXp >= TotalXpForLevel(lv + 1)) lv++;
        return lv;
    }

    // 캐릭터 컨셉에 맞춰 AI가 생성한 levelSkills(Lv5/10/15)를 우선 사용.
    // 구버전 캐릭터 등 levelSkills가 없으면 범용 스킬로 대체.
    public static SkillData GetSkillForLevel(int level, CharacterData ch)
    {
        int idx = level switch { 5 => 0, 10 => 1, 15 => 2, _ => -1 };
        if (idx < 0) return null;

        var levelSkills = ch?.generated?.levelSkills;
        if (levelSkills != null && idx < levelSkills.Count && levelSkills[idx] != null
            && !string.IsNullOrEmpty(levelSkills[idx].name))
            return levelSkills[idx];

        return level switch
        {
            5  => new SkillData { name = "치유의 손길", description = "HP 50 회복",              mpCost = 30, isPassive = false, atkMultiplier = 0f,   healAmount = 50 },
            10 => new SkillData { name = "강타",        description = "ATK×2.5 피해",           mpCost = 45, isPassive = false, atkMultiplier = 2.5f, healAmount = 0  },
            15 => new SkillData { name = "마나 폭발",   description = "ATK×4.0 강력한 피해",    mpCost = 70, isPassive = false, atkMultiplier = 4.0f, healAmount = 0  },
            _  => null,
        };
    }
}
