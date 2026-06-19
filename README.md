# AI 기반 캐릭터 생성 2D 모바일 게임

유저가 외형·무기·컨셉·세계관을 입력하면 AI(LLaMA 3.3 70B)가 이름·스탯·스킬·배경 스토리를 자동 생성하고,
생성된 캐릭터로 즉시 2D 모바일 게임을 플레이할 수 있는 프로젝트.

## 주요 기능

| 기능 | 설명 |
|------|------|
| **AI 캐릭터 생성** | 외형·무기·컨셉 입력 → 이름·스탯·고유 스킬·레벨업 스킬·배경 스토리 자동 생성 |
| **마을 씬** | 상점(포션·강화), 미션, 출석 체크, 원정대, 가방 관리 |
| **던전 탐험** | 5구역 탐험, 실시간 전투(공격·스킬·아이템·도망) |
| **XP·레벨 시스템** | 전투 XP 획득 → 레벨업 → 컨셉 맞춤 스킬 자동 습득 |
| **HP 지속성** | 던전 ↔ 마을 씬 간 HP/MP 상태 유지 |
| **AI 소설 생성** | 플레이 기록 기반 개인화 소설 자동 생성 (페이지 분할) |
| **캐릭터 영구 삭제** | 전투 사망 시 Firebase에서 캐릭터 영구 삭제 |

## 기술 스택

| 레이어 | 기술 | 선택 이유 |
|--------|------|----------|
| 게임 엔진 | Unity 2022.3 LTS | 씬 관리, Android/iOS 동시 빌드, C# 전투 로직 |
| 백엔드 | Node.js + Express | Groq/Firebase SDK npm 제공, 비동기 I/O 적합 |
| AI 생성 | Groq API (llama-3.3-70b-versatile) | 빠른 응답, 한국어 이해 우수 |
| 데이터베이스 | Firebase Firestore | 서버리스, JSON 구조와 NoSQL 자연 일치 |
| AI 개발 도구 | Claude Code | 코드·문서·커밋 자동화 |

## 빠른 시작 (설치 가이드)

### 사전 요구사항

- Node.js 18 이상
- Unity 2022.3 LTS + Android Build Support 모듈
- Git

### 1. 저장소 클론

```bash
git clone https://github.com/alstjdalsdud12/vibe-coding-project.git
cd vibe-coding-project
```

### 2. 백엔드 설정

```bash
cd backend
npm install
cp .env.example .env
# .env 파일에 아래 값 입력
```

```env
ANTHROPIC_API_KEY=your_groq_or_claude_api_key
FIREBASE_PROJECT_ID=your_project_id
FIREBASE_CLIENT_EMAIL=your_client_email
FIREBASE_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n"
PORT=3000
```

### 3. 백엔드 실행

```bash
npm run dev
# → 서버 실행 중: http://localhost:3000
```

### 4. API 동작 확인 (PowerShell)

```powershell
# 캐릭터 목록 조회
Invoke-RestMethod -Uri "http://localhost:3000/api/characters" -Method GET

# 캐릭터 생성 테스트
Invoke-RestMethod `
  -Uri "http://localhost:3000/api/characters" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"appearance":"검은 머리","weapon":"쌍검","concept":"암살자","worldview":"전쟁 왕국","name":"테스트"}'
```

### 5. Unity 프로젝트 열기

1. Unity Hub → "프로젝트 열기" → `game/VibeCodingGame/` 선택
2. Unity 2022.3 LTS로 열기
3. 씬 빌드 설정 확인: `TitleScene → MainMenuScene → CharacterCreationScene → VillageScene → GameScene`
4. Play 버튼으로 에디터 실행

---

## 프로젝트 구조

```
vibe-coding-project/
├── backend/                          # Node.js 백엔드
│   └── src/
│       ├── routes/characters.js      # REST API 라우트
│       ├── services/
│       │   ├── claudeService.js      # Groq AI 캐릭터/소설 생성
│       │   └── firebaseService.js    # Firestore CRUD
│       └── app.js                    # Express 서버
│
├── game/VibeCodingGame/Assets/Scripts/
│   ├── Managers/
│   │   ├── GameState.cs              # 씬 간 공유 상태 (정적 클래스)
│   │   ├── TitleManager.cs           # 타이틀 씬
│   │   ├── MainMenuManager.cs        # 캐릭터 목록·선택·삭제
│   │   ├── CharacterCreationManager.cs # 캐릭터 생성 입력
│   │   ├── VillageManager.cs         # 마을 씬 (상점/미션/출석/가방)
│   │   └── GameManager.cs            # 던전·전투·레벨업
│   ├── Models/CharacterData.cs       # 데이터 모델
│   ├── Player/PlayerController.cs    # 맵 이동 컨트롤러
│   └── Network/ApiClient.cs          # REST API 클라이언트
│
├── .planning/                        # 기획 문서
│   ├── 00-vision.md                  # 비전·목표
│   ├── 01-requirements.md            # 기능 요구사항
│   ├── 02-wbs.md                     # WBS
│   ├── 04-schedule.md                # 7주 일정표
│   └── decisions/                    # ADR (Architecture Decision Records)
│       ├── ADR-0001-mobile-platform-unity.md
│       ├── ADR-0002-backend-nodejs.md
│       ├── ADR-0003-database-firebase.md
│       ├── ADR-0004-ai-skill-generation.md
│       └── ADR-0005-gamestate-singleton.md
│
├── docs/
│   ├── architecture.md               # 시스템 아키텍처
│   ├── setup.md                      # 개발 환경 설정
│   ├── deploy.md                     # 빌드·배포 가이드
│   ├── testing.md                    # 테스트 가이드
│   ├── llm-wiki.md                   # AI 운용 노하우
│   └── presentation-script.md       # 발표 대본 (5분)
│
├── AGENTS.md                         # AI Agent·Skills·Rules·Commands 통합 정의서
├── AUTHORING.choi.md                 # 개발자 행동 규칙
└── README.md
```

---

## 아키텍처 개요

```
[Unity 2D 클라이언트]
  타이틀 → 메인메뉴 → 캐릭터생성 → 마을씬 → 던전씬
      ↕ REST API (UnityWebRequest)
[Node.js 백엔드]
  POST /api/characters        → Groq AI 호출 → Firebase 저장
  GET  /api/characters        → Firebase 조회
  PATCH /api/characters/:id/state → Firebase 상태 업데이트
  DELETE /api/characters/:id  → Firebase 삭제
  POST /api/characters/:id/novel → Groq AI 소설 생성
      ↕
[Groq API]             [Firebase Firestore]
 llama-3.3-70b          characters 컬렉션
```

---

## 문서 목록

| 문서 | 링크 |
|------|------|
| 기획서·요구사항 | `.planning/00-vision.md`, `.planning/01-requirements.md` |
| WBS·일정 | `.planning/02-wbs.md`, `.planning/04-schedule.md` |
| 아키텍처·ADR | `docs/architecture.md`, `.planning/decisions/` |
| 개발 환경 설정 | `docs/setup.md` |
| 빌드·배포 | `docs/deploy.md` |
| 테스트 | `docs/testing.md` |
| AI Agent 정의 | `AGENTS.md` |
| LLM 노하우 | `docs/llm-wiki.md` |
| 발표 대본 | `docs/presentation-script.md` |

---

## 라이선스

MIT
