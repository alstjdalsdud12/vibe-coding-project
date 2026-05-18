# AI 기반 캐릭터 생성 2D 모바일 게임

유저가 입력한 외형·무기·컨셉을 Claude API가 분석해 고유한 캐릭터(이름·스탯·능력·스토리)를 자동 생성하고, 생성된 캐릭터로 텍스트 기반 2D 모바일 게임을 즉시 플레이할 수 있는 프로젝트.

## 주요 기능

- **캐릭터 생성**: 외형, 무기, 컨셉 입력 → AI가 이름·스탯·특수능력·배경 스토리 자동 생성
- **Claude API**: 캐릭터 분석 및 전체 캐릭터 정보 생성 (이름 포함)
- **텍스트 기반 게임**: 이미지 없이 텍스트 UI로 전투·이동 구현
- **Firebase 연동**: 생성된 캐릭터 저장 및 불러오기

## 기술 스택

| 레이어 | 기술 |
|--------|------|
| 게임 엔진 | Unity 2D |
| 백엔드 | Node.js + Express |
| AI | Anthropic Claude API (claude-sonnet-4-6) |
| 데이터베이스 | Firebase Firestore |
| 모바일 배포 | Android / iOS |

## 빠른 시작

```bash
# 저장소 클론
git clone <repo-url>
cd vibe-coding-project

# 백엔드 의존성 설치
cd backend
npm install

# 환경 변수 설정
cp .env.example .env
# .env 파일에 API 키 입력

# 백엔드 실행
npm run dev
```

## 프로젝트 구조

```
vibe-coding-project/
├── backend/                      # API 서버 (Node.js + Express)
│   ├── src/
│   │   ├── routes/
│   │   │   └── characters.js     # 캐릭터 REST API (POST/GET/DELETE)
│   │   ├── services/
│   │   │   ├── claudeService.js  # Claude API 캐릭터 생성
│   │   │   └── firebaseService.js# Firestore CRUD
│   │   ├── middleware/
│   │   │   └── errorHandler.js   # 전역 에러 처리
│   │   └── app.js                # Express 서버 진입점
│   ├── .env.example
│   └── package.json
├── game/                         # Unity 2D 게임 프로젝트
│   └── VibeCodingGame/           # Unity 프로젝트 폴더
├── .planning/                    # 기획 문서 (Session 2)
│   ├── 00-vision.md              # 비전·목표
│   ├── 01-requirements.md        # 기능 요구사항
│   ├── 02-wbs.md                 # WBS
│   ├── 04-schedule.md            # 7주 일정표
│   └── decisions/
│       └── ADR-0001-backend-nodejs.md  # 의사결정 로그
├── docs/                         # 기술 문서
│   ├── architecture.md           # 시스템 아키텍처
│   ├── flow.md                   # 앱 실행 흐름
│   ├── setup.md                  # 개발 환경 설정
│   └── llm-wiki.md               # AI 노하우 노트
├── AGENTS.md                     # AI Agent 정의서
├── AUTHORING.md                  # 개발 가이드
├── AUTHORING.choi.md             # AI Agent 행동 지침서
├── BONUS.md                      # 가산점 트래킹
└── README.md
```

## 환경 변수

```env
ANTHROPIC_API_KEY=your_claude_api_key
FIREBASE_PROJECT_ID=your_project_id
FIREBASE_CLIENT_EMAIL=your_client_email
FIREBASE_PRIVATE_KEY=your_private_key
PORT=3000
```

## 프로젝트 소개

1. AI 기반 캐릭터 생성 2D 모바일 게임

   예시: - 외형·무기·컨셉 입력하면 AI가 캐릭터 자동 생성
           - 스탯·특수능력·배경 스토리 자동 지정
           - 생성된 캐릭터로 즉시 텍스트 기반 전투 플레이
           - Firebase에 캐릭터 저장 & 불러오기

   왜 좋은가:
   ✓ 손으로 하면: 캐릭터 밸런싱, 스킬 설계, 스토리 작성
   ✓ AI 덕분에: 입력 3줄 → Claude가 완성된 캐릭터 한 번에 생성

## 라이선스

MIT
