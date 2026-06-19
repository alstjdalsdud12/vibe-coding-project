# 테스트 가이드

## 테스트 전략

| 테스트 종류 | 대상 | 방법 |
|------------|------|------|
| 단위 테스트 | 백엔드 API 엔드포인트 | PowerShell / curl 직접 호출 |
| 단위 테스트 | GameState 로직 | Unity Editor Play Mode |
| 통합 테스트 | 캐릭터 생성 → 저장 → 게임 흐름 | 실기기 or Unity Editor 전체 플레이 |

---

## 1. 백엔드 단위 테스트

백엔드 서버를 실행한 후 (`npm run dev`) 각 엔드포인트를 검증한다.

### 1-1. 캐릭터 생성 (POST)

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:3000/api/characters" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"appearance":"검은 머리 냉혹한 눈빛","weapon":"쌍검","concept":"복수를 노리는 암살자","worldview":"전쟁으로 폐허가 된 왕국","name":"테스트"}'
```

**기대 결과:**
```json
{
  "success": true,
  "data": {
    "id": "auto-id",
    "generated": {
      "name": "한국어 캐릭터 이름",
      "stats": { "hp": ..., "atk": ..., "def": ..., "mp": ... },
      "uniqueSkill": { "name": "...", "mpCost": ..., "isPassive": false },
      "levelSkills": [ {...}, {...}, {...} ]
    }
  }
}
```

### 1-2. 캐릭터 목록 조회 (GET)

```powershell
Invoke-RestMethod -Uri "http://localhost:3000/api/characters" -Method GET
```

**기대 결과:** `success: true`, `data` 배열에 저장된 캐릭터 목록

### 1-3. 단건 조회 (GET /:id)

```powershell
Invoke-RestMethod -Uri "http://localhost:3000/api/characters/{위에서_얻은_id}" -Method GET
```

### 1-4. 상태 업데이트 (PATCH /:id/state)

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:3000/api/characters/{id}/state" `
  -Method PATCH `
  -ContentType "application/json" `
  -Body '{"gold":500,"xp":200,"level":3}'
```

### 1-5. 캐릭터 삭제 (DELETE)

```powershell
Invoke-RestMethod -Uri "http://localhost:3000/api/characters/{id}" -Method DELETE
```

**기대 결과:** `success: true`

---

## 2. AI 생성 검증 테스트

### 스키마 검증 항목

```
[ ] generated.name         : 한국어 문자열, 한자 없음
[ ] generated.stats.hp     : 50 ~ 200 사이 숫자
[ ] generated.stats.atk    : 10 ~ 100 사이 숫자
[ ] generated.uniqueSkill.name    : 한국어 문자열
[ ] generated.uniqueSkill.mpCost  : 숫자 (0 이상)
[ ] generated.levelSkills.length  : 3
[ ] 각 levelSkill.name     : 비어있지 않은 한국어 문자열
[ ] 텍스트에 한자 포함 여부 : 없어야 함
```

### 재시도 로직 확인

백엔드 로그에서 다음 확인:
```
[CLAUDE] 검증 실패, 재시도...   ← 이 줄이 나오면 재시도 발동
[CLAUDE] 재시도 성공           ← 또는 기본 스킬로 대체
```

---

## 3. Unity 단위 테스트 (Play Mode 확인)

Unity Editor에서 Play 버튼을 눌러 각 씬을 확인한다.

### 3-1. 타이틀 씬

```
[ ] 배경 애니메이션 표시
[ ] "시작하기" 버튼 클릭 → MainMenuScene 이동
```

### 3-2. 메인 메뉴 씬

```
[ ] 저장된 캐릭터 목록 불러오기 (Firebase 조회)
[ ] 캐릭터 카드에 이름·레벨·직업 표시
[ ] 캐릭터 선택 → VillageScene 이동
[ ] "새 캐릭터 만들기" → CharacterCreationScene 이동
[ ] "X" 버튼 → 확인 팝업 → 삭제 → 목록 갱신
```

### 3-3. 캐릭터 생성 씬

```
[ ] 입력 필드 (이름·외형·무기·컨셉·세계관) 작동
[ ] "생성" 버튼 → 로딩 화면 표시
[ ] AI 생성 완료 → 캐릭터 정보 표시 (이름·스탯·스킬·스토리)
[ ] "게임 시작" → VillageScene 이동
```

### 3-4. 마을 씬 (VillageScene)

```
[ ] 상단 HUD: 레벨·HP·MP·ATK·골드 표시
[ ] 상점: 회복/마나 포션 구매 → 가방에 추가
[ ] 상점: 방어구/무기 강화 → DEF/ATK 실제 증가
[ ] 가방: 회복 포션 사용 → HP 50 회복, HUD 반영
[ ] 미션: 던전 1회 입장 → 보상 버튼 활성화 → 레벨 +5
[ ] 출석 체크: 오늘 보상 수령 → 포션 or 골드 지급
[ ] 던전 입장 → GameScene 이동, DungeonCount+1
```

### 3-5. 게임 씬 (GameScene / 던전)

```
[ ] 맵 5개 구역 표시 (Slime→Goblin→Skull 순)
[ ] 몬스터와 충돌 → 전투 패널 표시
[ ] 전투: 공격 → 데미지 계산 (bonusAtk 반영)
[ ] 전투: 스킬 → MP 소모 → 효과 적용
[ ] 전투: 아이템 → 가방 포션 사용 → HP/MP 회복
[ ] 몬스터 처치 → 골드·XP 획득 → 레벨업 배너
[ ] HP 0 → 사망 → 캐릭터 삭제 → 메인 메뉴 버튼만 표시
[ ] 마을 귀환 → HP/MP GameState에 보존 → VillageScene 복귀
```

---

## 4. 통합 테스트 시나리오

전체 게임 흐름을 처음부터 끝까지 실행한다.

### 시나리오 A — 정상 플레이

```
1. 앱 실행 → 타이틀 화면
2. 새 캐릭터 만들기 → 정보 입력 → AI 생성 (약 5~10초)
3. 캐릭터 확인 → 마을 씬 진입
4. 상점에서 회복 포션 구매
5. 던전 입장 → 몬스터 처치 2마리
6. 마을 귀환 → HP 지속 확인 (던전에서 받은 데미지 유지)
7. 미션 보상 수령 (레벨 +5)
8. 소설로 보기 → AI 소설 생성 확인
9. 메인 메뉴 복귀 → 캐릭터 목록에 표시 확인
```

### 시나리오 B — 사망 처리

```
1. 던전에서 HP가 0이 되도록 유도 (도망 없이 계속 전투)
2. 사망 화면: "OOO 사망, 캐릭터가 삭제되었습니다" 표시
3. "메인 메뉴로" 버튼만 표시 (마을 귀환 버튼 없음)
4. 메인 메뉴 → 해당 캐릭터가 목록에서 삭제됨 확인
```

### 시나리오 C — 멀티 캐릭터 전환

```
1. 캐릭터 A 생성 (레벨 1)
2. 던전 플레이로 레벨 5 달성
3. 메인 메뉴 복귀 → 캐릭터 B 생성
4. 캐릭터 B 레벨이 1임을 확인 (캐릭터 A 데이터 오염 없음)
5. 다시 캐릭터 A 선택 → 레벨 5 유지 확인
```

---

## 5. 테스트 결과 기록

| 일자 | 시나리오 | 결과 | 발견 버그 |
|------|----------|------|----------|
| 2026-05-18 | 캐릭터 생성 | 통과 | - |
| 2026-05-25 | 마을 씬 기본 기능 | 통과 | HP 미표시 |
| 2026-06-01 | 전투 시스템 | 통과 | 몬스터 비주얼 순서 오류 |
| 2026-06-10 | 레벨업/스킬 | 통과 | 치유의 손길 고정 (서버 미재시작) |
| 2026-06-15 | 멀티 캐릭터 전환 | 통과 | GameState 오염 (수정 완료) |
| 2026-06-20 | 최종 통합 테스트 | 통과 | - |
