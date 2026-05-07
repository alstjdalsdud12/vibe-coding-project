# AGENTS.md
> 이 프로젝트에서 사용하는 AI Agent 정의서

---

## 1. 사용 Agent

| Agent | 도구 | 역할 |
|-------|------|------|
| Claude Code | Anthropic Claude (claude-sonnet-4-6) | 코드 작성, 문서 생성, 커밋, 디버깅 |
| Claude API | Anthropic Claude (claude-sonnet-4-6) | 캐릭터 스탯·능력·스토리 생성 (런타임) |

---

## 2. Claude Code — 개발 Agent

### 역할
- 코드 작성 및 수정
- 문서 생성 및 업데이트
- GitHub 커밋 & 푸시
- 오류 분석 및 디버깅

### 행동 기준
- `AUTHORING.choi.md` 규칙을 따른다
- 작업 전 관련 파일을 먼저 읽는다
- 파일별로 나눠서 커밋한다

### 사용 방법
```
Claude Code CLI에서 자연어로 지시
예) "캐릭터 생성 API 만들어줘"
    "커밋해줘"
    "다음 할 일"
```

---

## 3. Claude API — 런타임 Agent

### 역할
유저가 입력한 외형·무기·컨셉을 분석해 캐릭터 전체 정보를 JSON으로 생성

### 입력
```json
{
  "appearance": "검은 머리, 키 크고 우아함",
  "weapon": "지팡이",
  "concept": "신중하고 차분한 마법사"
}
```

### 출력
```json
{
  "name": "실바나 아쉬크로프트",
  "stats": { "hp": 85, "atk": 35, "def": 25, "mp": 120 },
  "abilities": [
    { "name": "정적의 화살", "description": "MP 20 소모. ATK×1.8 피해" }
  ],
  "story": "오래된 숲의 끝자락에서 홀로 수련한 마법사..."
}
```

### 설정
```
모델   : claude-sonnet-4-6
방식   : 프롬프트 캐싱 적용 (시스템 프롬프트 cache_control)
응답   : JSON only
위치   : backend/src/services/claudeService.js
```

---

## 4. Agent 간 역할 분리

```
개발 시점
  choi (사람)  →  Claude Code  →  코드·문서·GitHub

런타임
  유저 입력  →  Claude API  →  캐릭터 데이터  →  Firebase 저장  →  게임 시작
```

---

## 5. 프롬프트 관리 원칙

- 시스템 프롬프트는 `backend/src/services/claudeService.js`에서 중앙 관리
- 프롬프트 변경 시 반드시 커밋 메시지에 명시
- 개발 중 API 호출은 mock으로 대체 (비용 절감)
