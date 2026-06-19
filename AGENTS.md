# AGENTS.md — AI Agent 통합 정의서

> **본인만의 기법**: 이 파일 하나에 Agent 정의·Skills·Rules·Commands를 통합 관리.
> Claude Code가 이 파일을 읽어 프로젝트 전반의 AI 행동 규칙을 자동 적용한다.

---

## 1. Agent 정의

| Agent | 모델 | 역할 |
|-------|------|------|
| **Claude Code** | claude-sonnet-4-6 | 개발 보조 — 코드 작성·디버깅·문서·커밋 |
| **Groq API (런타임)** | llama-3.3-70b-versatile | 캐릭터·스킬·소설 AI 생성 (실시간) |

### 1-1. Claude Code (개발 Agent)

```
역할   : 코드 작성, 버그 수정, 문서 생성, GitHub 커밋/푸시
입력   : 자연어 지시 (한국어)
출력   : 코드 파일 수정, 마크다운 문서, git commit
위치   : 개발 시점 전용 (런타임 미사용)
```

### 1-2. Groq API (런타임 AI Agent)

```
역할   : 유저 입력(외형·무기·컨셉·세계관)으로 캐릭터 전체 생성
모델   : llama-3.3-70b-versatile
응답   : JSON only (스키마 검증 후 실패 시 1회 자동 재시도)
위치   : backend/src/services/claudeService.js
```

**입력 예시:**
```json
{ "appearance": "검은 머리, 냉혹한 눈빛", "weapon": "쌍검", "concept": "복수를 노리는 암살자", "worldview": "전쟁으로 폐허가 된 왕국" }
```

**출력 스키마:**
```json
{
  "name": "한국어 이름",
  "characterIndex": 1,
  "stats": { "hp": 120, "atk": 75, "def": 30, "mp": 60 },
  "abilities": [{ "name": "암살", "description": "..." }],
  "story": "배경 스토리 한국어",
  "locations": [{ "name": "폐허의 거리", "description": "..." }],
  "uniqueSkill": { "name": "...", "mpCost": 30, "isPassive": false, "atkMultiplier": 2.0, "healAmount": 0 },
  "levelSkills": [
    { "name": "Lv5 스킬", "mpCost": 25, "isPassive": false, "atkMultiplier": 1.8, "healAmount": 0 },
    { "name": "Lv10 스킬", "mpCost": 40, "isPassive": true, "atkMultiplier": 1.3, "healAmount": 0 },
    { "name": "Lv15 스킬", "mpCost": 60, "isPassive": false, "atkMultiplier": 3.5, "healAmount": 0 }
  ]
}
```

---

## 2. Skills (AI 기능 목록)

| Skill | 트리거 | 구현 위치 |
|-------|--------|----------|
| `generateCharacter` | 캐릭터 생성 버튼 | `claudeService.generateCharacter()` |
| `generateNovel` | 소설로 보기 버튼 | `claudeService.generateNovel()` |
| `sanitizeKorean` | 산문 텍스트 자동 적용 | `claudeService.sanitizeKorean()` |
| `sanitizeText` | 이름/설명 자동 적용 | `claudeService.sanitizeText()` |
| `isValidGenerated` | 생성 후 검증 | `claudeService.isValidGenerated()` |

### Skill 상세 — generateCharacter

```
입력  : { appearance, weapon, concept, worldview }
처리  : Groq API 호출 → JSON 파싱 → isValidGenerated 검증
실패시: 1회 재시도
성공시: sanitizeGeneratedTexts 적용 → Firestore 저장
```

### Skill 상세 — generateNovel

```
입력  : 전체 CharacterData (레벨·스킬·퀘스트·스토리로그 포함)
분량  : 레벨·로그 수에 따른 3단계 티어 (단편 / 중편 / 장편)
출력  : 한국어 소설 텍스트 (페이지 분할 표시)
```

---

## 3. Rules (코딩 규칙)

### 3-1. 한국어 텍스트 처리

```
산문(소설/스토리)   → sanitizeKorean()  : 단어 단위, 한자 포함 단어 제거
이름/설명/장소명    → sanitizeText()    : 글자 단위, 한자 문자만 제거
정규식: /[一-鿿㐀-䶿぀-ヿ豈-﫿]/g
```

### 3-2. 스킬 스키마 검증

```javascript
// isValidSkill: name(string) + mpCost(number) + isPassive(bool) + atkMultiplier(float) + healAmount(number)
// isValidGenerated: uniqueSkill + 3개 levelSkills 모두 유효해야 통과
// 실패 시 1회 재시도, 재시도 후에도 실패하면 기본 스킬 사용
```

### 3-3. GameState 규칙

```csharp
// 캐릭터 전환 시 반드시 LoadCharacter(ch) 호출 — 직접 CurrentCharacter 대입 금지
// CurrentHp/CurrentMp : 던전 씬 간 HP 지속 보존
// MissionRewarded[]   : 씬 이동 후에도 수령 여부 유지
// LoadCharacter()가 ID 변경을 감지해 Gold/Xp/Level 등 완전 초기화
```

### 3-4. Unity API 통신

```
- 모든 HTTP 요청: UnityWebRequest + 코루틴
- StateRequest에 포함: inventory, gold, questProgress, xp, level, learnedSkills, bonusAtk, bonusDef
- 백엔드 PATCH /state 로 전달 → Firestore .update() 호출
```

### 3-5. 커밋 규칙

```
형식   : "feat:" | "fix:" | "docs:" | "refactor:" | "style:"
언어   : 커밋 메시지 한국어
단위   : 기능 단위로 커밋, 파일 묶어서 한 번에
공동저자: Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
```

---

## 4. Commands (Claude Code 사용 명령 패턴)

| 한국어 명령 | 동작 |
|------------|------|
| `커밋해줘` | git add → commit (한국어 메시지) → 완료 알림 |
| `푸시해줘` | git push origin main |
| `다음 할 일` | BONUS.md·플래닝 확인 후 미완료 항목 제안 |
| `버그 고쳐줘` | 오류 로그 분석 → 원인 추론 → 수정 → 재현 확인 |
| `문서 만들어줘` | 코드 현황 파악 후 마크다운 문서 생성 |
| `발표 대본 써줘` | 평가 기준 기반 5분 대본 작성 |

---

## 5. Agent 간 역할 분리 (플로우)

```
[개발 시점]
  개발자(choi)  →  Claude Code  →  코드·문서·GitHub 관리
                                    ↓ (AGENTS.md·Rules 적용)

[런타임]
  유저 입력 (외형·무기·컨셉·세계관)
      ↓
  Unity 클라이언트 (UnityWebRequest)
      ↓ POST /api/characters
  Node.js 백엔드
      ├─► Groq API (LLaMA 3.3 70B)  →  캐릭터 JSON 생성
      │       └─► 검증 실패 시 1회 재시도
      └─► Firebase Firestore  →  저장
      ↓ 캐릭터 데이터 반환
  Unity: 마을 씬 → 던전 → 전투 → 레벨업
```

---

## 6. 프롬프트 관리

```
위치   : backend/src/services/claudeService.js
변경시 : 커밋 메시지에 "프롬프트 변경:" 명시
캐싱   : Groq API 시스템 프롬프트 재사용으로 응답 속도 향상
버전   : SYSTEM_PROMPT 상수 단일 관리
```

---

## 7. 암묵지 (LLM 운용 노하우)

| 노하우 | 내용 |
|--------|------|
| JSON 강제 | 시스템 프롬프트에 "유효한 JSON만 반환" 명시 + 재시도 로직 필수 |
| 한국어 오염 | LLM이 한자·일본어를 자연스럽게 섞음 → 정규식 sanitize 필수 |
| 스키마 검증 | typeof 체크로 숫자/불리언 필드 누락 감지 후 재시도 |
| 길이 제어 | 소설 분량 = maxTokens 파라미터로 직접 제어 (지시만으로는 불안정) |
| 컨텍스트 오염 | GameState 전역 상태는 캐릭터 전환 시 반드시 명시적 초기화 |
