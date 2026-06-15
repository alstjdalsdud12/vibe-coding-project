// 씬 간 데이터를 전달하는 전역 상태 (씬이 바뀌어도 유지됨)
public static class GameState
{
    public static CharacterData CurrentCharacter;
    public static int Gold         = 0;
    public static int DungeonCount = 0;
    public static int MonsterCount = 0;
    public static int BossCount    = 0;
}
