# ADR-0005 — 씬 간 데이터 관리: GameState 정적 클래스

## 배경

Unity에서 씬(Scene) 전환 시 모든 오브젝트가 파괴된다.
캐릭터 데이터·골드·XP·레벨·HP를 여러 씬에서 공유해야 했다.
방법으로 (A) DontDestroyOnLoad 매니저, (B) 정적(static) 클래스, (C) PlayerPrefs를 검토했다.

## 결정

**GameState 정적(static) 클래스** 채택

## 대안

| 후보 | 장점 | 탈락 이유 |
|------|------|----------|
| DontDestroyOnLoad | Unity 표준, 씬 유지 | MonoBehaviour 의존, 초기화 관리 복잡 |
| PlayerPrefs | 영구 저장 | 직렬화 번거로움, 민감 데이터 노출 |

## 이유

1. `static` 필드는 씬 전환과 무관하게 앱 생존 주기 동안 유지
2. MonoBehaviour 없이 어느 클래스에서도 직접 접근 가능
3. `LoadCharacter(ch)` 메서드로 ID 변경 감지 → 완전 초기화 보장

## 핵심 설계

```csharp
public static class GameState {
    public static CharacterData CurrentCharacter;
    public static string LoadedCharacterId;    // 캐릭터 전환 감지용
    public static int Gold, Xp, Level;
    public static int CurrentHp, CurrentMp;    // HP 던전 간 지속
    public static bool[] MissionRewarded;      // 씬 이동 후 유지

    public static void LoadCharacter(CharacterData ch) {
        if (LoadedCharacterId == ch.id) return; // 같은 캐릭터면 무시
        LoadedCharacterId = ch.id;
        Gold = ch.gold; Xp = ch.xp; Level = LevelFromXp(Xp);
        CurrentHp = ch.generated.stats.hp;     // 새 캐릭터는 풀 HP
        MissionRewarded = new bool[4];          // 미션 초기화
    }
}
```

## 결과

- 다른 캐릭터로 전환 시 이전 캐릭터 데이터 오염 방지
- HP/MP가 던전 씬 ↔ 마을 씬 간 정상 유지
- 미션 보상 수령 여부가 씬 재로드 후에도 유지
