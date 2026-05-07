# 개발 환경 설정 가이드

이 문서는 프로젝트를 처음 세팅하는 사람을 위한 단계별 안내입니다.

---

## 필요한 프로그램

| 프로그램 | 버전 | 용도 |
|----------|------|------|
| Node.js | 18 이상 | 백엔드 서버 실행 |
| npm | Node.js에 포함 | 패키지 설치 |
| Git | 최신 | 저장소 관리 |
| Unity Hub | 최신 | Unity 설치 및 프로젝트 관리 |
| Unity Editor | 2022.3 LTS | 게임 개발 |

---

## 1단계 — 저장소 클론

```bash
git clone https://github.com/alstjdalsdud12/vibe-coding-project.git
cd vibe-coding-project
```

---

## 2단계 — 백엔드 의존성 설치

```bash
cd backend
npm install
```

설치되는 주요 패키지:
- `express` — HTTP 서버
- `@anthropic-ai/sdk` — Claude AI 연동
- `firebase-admin` — Firebase Firestore 연동
- `cors` — 크로스 오리진 허용
- `dotenv` — 환경 변수 로드

---

## 3단계 — 환경 변수 설정

`backend/` 폴더 안에 `.env` 파일을 생성한다. (`.env.example`을 복사해서 수정)

```env
ANTHROPIC_API_KEY=sk-ant-여기에_실제_키_입력
FIREBASE_PROJECT_ID=여기에_프로젝트_ID
FIREBASE_CLIENT_EMAIL=여기에_클라이언트_이메일
FIREBASE_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n"
PORT=3000
```

> **주의**: `.env` 파일은 절대 GitHub에 올리지 않는다. `.gitignore`에 이미 포함되어 있다.

### API 키 없이 개발할 경우 (Mock 모드)

실제 Claude API 키나 Firebase 키가 없어도 테스트할 수 있다.

```env
USE_MOCK=true
```

Mock 모드에서는 Claude API를 호출하지 않고 하드코딩된 캐릭터 데이터를 반환한다.

---

## 4단계 — Firebase 서비스 계정 키 발급

1. [Firebase Console](https://console.firebase.google.com/) 접속
2. 프로젝트 선택 → 프로젝트 설정 → 서비스 계정 탭
3. "새 비공개 키 생성" 버튼 클릭 → JSON 파일 다운로드
4. JSON 파일 안의 값들을 `.env`에 복사

> JSON 파일 자체는 프로젝트 폴더 **밖**의 안전한 위치에 보관한다.

---

## 5단계 — 백엔드 서버 실행

```bash
# backend/ 폴더에서
npm run dev
```

정상 실행 시 콘솔 출력:
```
서버 실행 중: http://localhost:3000
```

---

## 6단계 — API 동작 확인

PowerShell에서 테스트:

```powershell
Invoke-RestMethod -Uri "http://localhost:3000/api/characters" -Method POST -ContentType "application/json" -Body '{"appearance":"검은 머리","weapon":"지팡이","concept":"마법사"}'
```

정상 응답 예시:
```json
{
  "success": true,
  "data": {
    "id": "abc123",
    "userInput": { "appearance": "검은 머리", "weapon": "지팡이", "concept": "마법사" },
    "generated": {
      "name": "실바나 아쉬크로프트",
      "stats": { "hp": 85, "atk": 35, "def": 25, "mp": 120 },
      "abilities": [...],
      "story": "..."
    },
    "createdAt": "2025-..."
  }
}
```

---

## 7단계 — Unity 프로젝트 설정

1. Unity Hub 설치: https://unity.com/download
2. Unity Editor 2022.3 LTS 설치 (Unity Hub에서 설치 가능)
3. Unity Hub → "새 프로젝트" → 2D 템플릿 선택
4. 프로젝트 위치: `vibe-coding-project/game/` 폴더로 지정

---

## 폴더 구조 요약

```
vibe-coding-project/
├── backend/          ← Node.js 서버 (이 문서 기준 작업 폴더)
│   ├── src/
│   ├── .env          ← 직접 생성 (gitignore됨)
│   ├── .env.example  ← 템플릿
│   └── package.json
├── game/             ← Unity 프로젝트 (추후 생성)
└── docs/             ← 프로젝트 문서
```

---

## 문제 해결

| 증상 | 원인 | 해결 |
|------|------|------|
| `서버 실행 중` 안 뜸 | 포트 충돌 또는 오류 | 콘솔 에러 메시지 확인 |
| `Connection error.` | 잘못된 API 키 | `.env`의 키 확인 또는 `USE_MOCK=true` 설정 |
| Firebase 오류 | 키 형식 오류 | PRIVATE_KEY에 `\n` 이스케이프 확인 |
| Mock 모드 안 됨 | `.env` 미적용 | 서버 재시작 |
