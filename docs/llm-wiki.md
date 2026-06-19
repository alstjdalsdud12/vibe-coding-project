# LLM Wiki — choi의 AI 노하우 노트

AI Agent를 사용하면서 배운 것들을 기록합니다.

---

## 1. 프롬프트 패턴

### JSON 응답 강제하기
Claude/Groq에게 JSON만 반환하게 하려면 시스템 프롬프트에
`"반드시 유효한 JSON만 반환하고 다른 텍스트는 포함하지 않는다"` 를 명시해야 한다.
이것만으로는 부족하고, **서버에서 JSON.parse 후 스키마 검증 + 재시도 로직이 필수**다.

### 스키마 검증 패턴
```javascript
function isValidSkill(s) {
  return s && typeof s.name === 'string' && s.name.length > 0
    && typeof s.mpCost === 'number'
    && typeof s.isPassive === 'boolean'
    && typeof s.atkMultiplier === 'number';
}
```
`typeof` 체크가 중요하다 — LLM이 숫자 자리에 문자열을 넣거나 필드를 누락하는 경우가 있다.

### 분량 제어
소설 길이를 "짧게 써줘" 같은 지시로 제어하면 불안정하다.
`maxTokens` 파라미터로 직접 제한하는 것이 훨씬 신뢰성 있다.

---

## 2. 한국어 LLM 사용 시 주의점

### 한자 섞임 현상
LLaMA 계열 모델은 한국어 텍스트를 생성할 때 한자나 일본어 가나를 자연스럽게 섞는다.
특히 고유명사, 지명, 장소 이름에서 자주 발생한다.

**해결 방법:**
```javascript
const FOREIGN_CHAR_RE = /[一-鿿㐀-䶿぀-ヿ豈-﫿]/g;

// 이름/설명 — 글자 단위 제거
const sanitizeText = (text) => text.replace(FOREIGN_CHAR_RE, '').replace(/ {2,}/g, ' ').trim();

// 소설/산문 — 단어 단위 제거 (한자 포함 단어 통째로 제거)
const sanitizeKorean = (text) => text.split(' ')
  .filter(w => !FOREIGN_CHAR_RE.test(w))
  .join(' ');
```

### 컨텍스트 의존성
같은 캐릭터 컨셉을 여러 번 요청해도 결과가 다르다. → **저장이 필수**.
생성 결과는 항상 서버에서 검증하고 Firebase에 저장한 후 클라이언트에 전달한다.

---

## 3. Claude Code 협업 노하우

### 파일을 먼저 읽어야 정확하게 수정한다
Claude Code에게 "이 부분 고쳐줘"라고만 하면 엉뚱한 곳을 고친다.
"먼저 GameManager.cs를 읽고 몬스터 생성 부분을 수정해줘"처럼
대상 파일을 명시하면 훨씬 정확하다.

### 커밋 메시지를 한국어로
영어 커밋 메시지보다 한국어가 나중에 내가 읽기 편하다.
Claude Code는 자동으로 한국어 커밋 메시지를 생성해준다.

### 컨텍스트가 길어지면 요약을 활용
대화가 길어지면 Claude Code가 앞 내용을 잊는다.
AGENTS.md와 AUTHORING.choi.md에 중요 규칙을 적어두면
새 대화에서도 일관된 행동을 유지한다.

---

## 4. 해결한 에러 사례

### JsonUtility null 역직렬화
**현상:** Firebase에 `levelSkills: undefined`인 캐릭터가 있으면
JsonUtility가 null 대신 `name=""` 인 빈 SkillData 객체를 만든다.

**해결:**
```csharp
_player.learnedSkills.RemoveAll(s => s == null || string.IsNullOrEmpty(s.name));
```
그리고 GetSkillForLevel()에서 `!string.IsNullOrEmpty(levelSkills[idx].name)` 가드 추가.

### GameState 오염
**현상:** 캐릭터 B를 새로 만들면 캐릭터 A의 골드·레벨이 그대로 남아 있음.

**원인:** `Math.Max(GameState.Gold, ch.gold)` 패턴 — GameState에 이전 캐릭터 값이 잔류.

**해결:** `LoadCharacter(ch)` 메서드에서 ID 변경 감지 시 모든 필드를 `ch`의 값으로 하드 리셋.

### Unity 씬 전환 후 미션 보상 버튼 재활성화
**현상:** 던전에서 돌아오면 미션 보상 버튼이 다시 활성화됨.

**원인:** `_missionRewarded` 가 VillageManager 인스턴스 필드 → 씬 재로드 시 초기화됨.

**해결:** `GameState.MissionRewarded` 정적 배열로 이전, `LoadCharacter()`에서 초기화.

---

## 5. AI Agent 도구별 차이

| 도구 | 역할 | 장점 |
|------|------|------|
| Claude Code (CLI) | 개발 보조 | 코드·커밋·문서 자동화, 대화형 지시 |
| Groq API | 런타임 생성 | 빠른 응답 (GPT-4 수준 품질, 높은 속도) |
| Claude API | 런타임 대안 | 프롬프트 캐싱으로 비용 절감 |

Groq API는 Claude보다 응답이 빠르지만 한국어 일관성이 약간 낮다.
→ 서버사이드 sanitize + 검증 재시도가 더 중요해진다.
