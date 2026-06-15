using System;
using System.Collections.Generic;

[Serializable]
public class SkillData
{
    public string name;
    public string description;
    public int    mpCost;
    public bool   isPassive;
    public float  atkMultiplier;
    public int    healAmount;
}

[Serializable]
public class InventoryItem
{
    public string name;
    public int    qty;
    public string itemType;  // "material" | "consume"
    public int    sellPrice; // 판매가 (consume 은 -1)
}

[Serializable]
public class QuestProgress
{
    public int dungeonCount;
    public int monsterCount;
    public int bossCount;
}

[Serializable]
public class ExpeditionState
{
    public int  slotIdx;
    public long returnTime; // Unix ms timestamp
}

[Serializable]
public class AbilityData
{
    public string name;
    public string description;
}

[Serializable]
public class LocationData
{
    public string name;
    public string description;
}

[Serializable]
public class StatsData
{
    public int hp;
    public int atk;
    public int def;
    public int mp;
}

[Serializable]
public class SpumPartsData
{
    public string hair;
    public string hairColor;
    public string helmet;
    public string weapon;
    public string weaponType;
    public string shield;
    public string back;
}

[Serializable]
public class GeneratedData
{
    public string name;
    public int characterIndex;
    public SpumPartsData spumParts;
    public StatsData stats;
    public List<AbilityData> abilities;
    public string story;
    public List<LocationData> locations;
    public SkillData uniqueSkill;
}

[Serializable]
public class UserInputData
{
    public string appearance;
    public string weapon;
    public string concept;
    public string worldview;
}

[Serializable]
public class CharacterData
{
    public string id;
    public UserInputData userInput;
    public GeneratedData generated;
    public List<InventoryItem>   inventory;
    public int                   gold;
    public QuestProgress         questProgress;
    public string                lastAttendanceDate;
    public List<ExpeditionState> expeditions;
    public List<string>          storyLog;
    public int                   xp;
    public int                   level;
    public List<SkillData>       learnedSkills;
}

[Serializable]
public class CharacterListItem
{
    public string id;
    public string name;
    public string weapon;
    public string concept;
    public SpumPartsData spumParts;
    public int characterIndex;
}

[Serializable]
public class CharacterListResponse
{
    public bool success;
    public CharacterListItem[] data;
}

[Serializable]
public class CharacterResponse
{
    public bool success;
    public CharacterData data;
}
