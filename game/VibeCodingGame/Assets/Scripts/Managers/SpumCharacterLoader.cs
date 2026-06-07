using UnityEngine;
using UnityEngine.UI;

public static class SpumCharacterLoader
{
    private const string BASE = "Addons/Ver300/0_Unit/0_Sprite/";

    private static readonly System.Collections.Generic.Dictionary<string, string> WeaponFolders =
        new System.Collections.Generic.Dictionary<string, string>
        {
            { "Sword",  "8_Weapons/0_Sword/" },
            { "Spear",  "8_Weapons/1_Spear/" },
            { "Axe",    "8_Weapons/2_Axe/"   },
            { "Bow",    "8_Weapons/3_Bow/"    },
            { "Wand",   "8_Weapons/5_Wand/"   },
            { "Dagger", "8_Weapons/6_Dagger/" },
            { "Mace",   "8_Weapons/8_Mace/"   },
        };

    // 게임 월드 SPUM 캐릭터에 파츠 적용 (SpriteRenderer 방식)
    public static void Apply(SPUM_Prefabs spum, SpumPartsData parts)
    {
        if (spum == null || parts == null) return;

        SetAllByName(spum.transform, "P_Hair", BASE + "2_Hair/" + parts.hair);

        if (!string.IsNullOrEmpty(parts.hairColor))
            SetColor(spum.transform, "P_Hair", parts.hairColor);

        if (!string.IsNullOrEmpty(parts.helmet) && parts.helmet != "none")
            SetAllByName(spum.transform, "P_Helmet", BASE + "4_Helmet/" + parts.helmet);
        else
            ClearAllByName(spum.transform, "P_Helmet");

        if (!string.IsNullOrEmpty(parts.weaponType) && WeaponFolders.TryGetValue(parts.weaponType, out var folder))
            SetAllByName(spum.transform, "P_Weapon", BASE + folder + parts.weapon);

        if (!string.IsNullOrEmpty(parts.shield) && parts.shield != "none")
            SetAllByName(spum.transform, "P_Shield", BASE + "8_Weapons/7_Shield/" + parts.shield);
        else
            ClearAllByName(spum.transform, "P_Shield");

        if (!string.IsNullOrEmpty(parts.back) && parts.back != "none")
            SetAllByName(spum.transform, "P_Back", BASE + "10_Back/" + parts.back);
        else
            ClearAllByName(spum.transform, "P_Back");
    }

    private static void SetColor(Transform root, string partName, string hexColor)
    {
        if (!hexColor.StartsWith("#")) hexColor = "#" + hexColor;
        if (!ColorUtility.TryParseHtmlString(hexColor, out Color color)) return;
        ApplyColorToAll(root, partName, color);
    }

    private static void ApplyColorToAll(Transform t, string name, Color color)
    {
        if (t.name == name)
        {
            var sr = t.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null) sr.color = color;
        }
        foreach (Transform child in t)
            ApplyColorToAll(child, name, color);
    }

    // 정적 초상화용 — 헤어 스프라이트를 UI Image에 로드
    public static void SetPortrait(Image target, SpumPartsData parts)
    {
        if (target == null || parts == null) return;
        var sprite = Resources.Load<Sprite>(BASE + "2_Hair/" + parts.hair);
        if (sprite != null) target.sprite = sprite;
    }

    private static void ClearAllByName(Transform root, string partName)
    {
        ClearRecursive(root, partName);
    }

    private static void ClearRecursive(Transform t, string name)
    {
        if (t.name == name)
        {
            var sr = t.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null) sr.sprite = null;
        }
        foreach (Transform child in t)
            ClearRecursive(child, name);
    }

    private static void SetAllByName(Transform root, string partName, string path)
    {
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning("[SPUM] 스프라이트 로드 실패: " + path);
            return;
        }
        ApplyToAll(root, partName, sprite);
    }

    private static void ApplyToAll(Transform t, string name, Sprite sprite)
    {
        if (t.name == name)
        {
            var sr = t.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                sr.sprite = sprite;
                Debug.Log($"[SPUM] {name} 스프라이트 적용: {sprite.name}");
            }
        }
        foreach (Transform child in t)
            ApplyToAll(child, name, sprite);
    }
}
