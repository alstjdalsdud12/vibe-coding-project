// 씬 간 데이터를 전달하는 전역 상태 (씬이 바뀌어도 유지됨)
public static class GameState
{
    public static CharacterData CurrentCharacter;
    public static int  Gold         = 0;
    public static int  DungeonCount = 0;
    public static int  MonsterCount = 0;
    public static int  BossCount    = 0;
    public static bool StoryUpdated = false;
    public static int  Xp           = 0;
    public static int  Level        = 1;

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

    public static SkillData GetSkillForLevel(int level)
    {
        return level switch
        {
            5  => new SkillData { name = "치유의 손길", description = "HP 50 회복",              mpCost = 30, isPassive = false, atkMultiplier = 0f,   healAmount = 50 },
            10 => new SkillData { name = "강타",        description = "ATK×2.5 피해",           mpCost = 45, isPassive = false, atkMultiplier = 2.5f, healAmount = 0  },
            15 => new SkillData { name = "마나 폭발",   description = "ATK×4.0 강력한 피해",    mpCost = 70, isPassive = false, atkMultiplier = 4.0f, healAmount = 0  },
            _  => null,
        };
    }
}
