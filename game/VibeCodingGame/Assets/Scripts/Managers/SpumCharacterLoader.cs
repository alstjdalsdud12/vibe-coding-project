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

    // SPUM_Prefabs의 ImageElement에 파츠 적용
    public static void Apply(SPUM_Prefabs spum, SpumPartsData parts)
    {
        if (spum == null || parts == null) return;

        foreach (var el in spum.ImageElement)
        {
            switch (el.PartType)
            {
                case "Hair":
                    SetSprite(el.image, BASE + "2_Hair/" + parts.hair);
                    break;
                case "Helmet":
                    if (!string.IsNullOrEmpty(parts.helmet) && parts.helmet != "none")
                        SetSprite(el.image, BASE + "4_Helmet/" + parts.helmet);
                    else
                        el.image.sprite = null;
                    break;
                case "Weapons":
                    if (el.PartSubType == "Shield")
                    {
                        if (!string.IsNullOrEmpty(parts.shield) && parts.shield != "none")
                            SetSprite(el.image, BASE + "8_Weapons/7_Shield/" + parts.shield);
                        else
                            el.image.sprite = null;
                    }
                    else
                    {
                        if (WeaponFolders.TryGetValue(parts.weaponType, out var folder))
                            SetSprite(el.image, BASE + folder + parts.weapon);
                    }
                    break;
                case "Back":
                    if (!string.IsNullOrEmpty(parts.back) && parts.back != "none")
                        SetSprite(el.image, BASE + "10_Back/" + parts.back);
                    else
                        el.image.sprite = null;
                    break;
            }
        }
    }

    // 정적 초상화용 — 헤어 스프라이트를 UI Image에 로드
    public static void SetPortrait(Image target, SpumPartsData parts)
    {
        if (target == null || parts == null) return;
        var sprites = Resources.LoadAll<Sprite>(BASE + "2_Hair/" + parts.hair);
        if (sprites != null && sprites.Length > 0)
            target.sprite = sprites[0];
    }

    private static void SetSprite(Image img, string path)
    {
        if (img == null) return;
        var sprites = Resources.LoadAll<Sprite>(path);
        if (sprites != null && sprites.Length > 0)
            img.sprite = sprites[0];
    }
}
