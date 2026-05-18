using System;
using System.Collections.Generic;

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
public class GeneratedData
{
    public string name;
    public StatsData stats;
    public List<AbilityData> abilities;
    public string story;
    public List<LocationData> locations;
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
}

[Serializable]
public class CharacterListItem
{
    public string id;
    public string name;
    public string weapon;
    public string concept;
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
