# 빌드 & 배포 가이드

## 개요

이 프로젝트는 두 가지 배포 대상이 있다.

| 대상 | 방법 |
|------|------|
| Node.js 백엔드 | 로컬 실행 or Railway/Render 클라우드 배포 |
| Unity 클라이언트 | Android APK 빌드 or Unity Remote 테스트 |

---

## 1. 백엔드 — 로컬 실행

```bash
cd backend
npm install
cp .env.example .env   # API 키 입력
npm run dev
# → http://localhost:3000
```

---

## 2. 백엔드 — 클라우드 배포 (Railway)

### 2-1. Railway 프로젝트 생성

1. https://railway.app 접속 → GitHub 로그인
2. "New Project" → "Deploy from GitHub repo" → 이 저장소 선택
3. 루트 디렉토리 설정: `backend`

### 2-2. 환경 변수 설정

Railway 대시보드 Variables 탭에서 입력:

```
ANTHROPIC_API_KEY=...
FIREBASE_PROJECT_ID=...
FIREBASE_CLIENT_EMAIL=...
FIREBASE_PRIVATE_KEY=...
PORT=3000
```

### 2-3. 배포 확인

```bash
curl https://your-app.railway.app/api/characters
# → { "success": true, "data": [] }
```

### 2-4. Unity BASE_URL 변경

`game/VibeCodingGame/Assets/Scripts/Network/ApiClient.cs` 의 `BASE_URL`을
Railway 주소로 변경:

```csharp
private const string BASE_URL = "https://your-app.railway.app/api";
```

---

## 3. Unity 클라이언트 — Android APK 빌드

### 3-1. 사전 준비

| 항목 | 확인 |
|------|------|
| Unity 2022.3 LTS 설치 | Unity Hub에서 확인 |
| Android Build Support 모듈 | Unity Hub → 설치 → 모듈 추가 |
| Android SDK/NDK | Unity가 자동 설치 |

### 3-2. 빌드 단계

1. Unity Editor에서 프로젝트 열기: `game/VibeCodingGame/`
2. **File → Build Settings** 열기
3. Platform: **Android** 선택 → "Switch Platform"
4. Player Settings 확인:
   - Company Name, Product Name, Bundle Identifier 설정
   - Minimum API Level: Android 7.0 (API 24) 이상
5. **Build** 버튼 → APK 저장 위치 선택
6. 빌드 완료 → `.apk` 파일 생성

### 3-3. 기기 설치

```bash
# ADB로 직접 설치 (USB 디버깅 활성화 필요)
adb install path/to/game.apk
```

또는 `.apk` 파일을 기기로 직접 전송 → 열기 → 설치

---

## 4. 개발 중 테스트 — Unity Remote

빌드 없이 스마트폰에서 실시간 테스트:

1. 스마트폰에 "Unity Remote 5" 앱 설치
2. USB로 PC와 연결
3. Unity Editor: **Edit → Project Settings → Editor → Device** → "Any Android Device"
4. Unity Editor에서 Play → 스마트폰 화면에 미러링

---

## 5. 배포 체크리스트

```
백엔드
[ ] .env 파일 확인 (API 키 유효)
[ ] npm run dev 정상 실행
[ ] POST /api/characters 응답 확인
[ ] Firebase 연결 확인

Unity
[ ] BASE_URL 이 백엔드 주소와 일치
[ ] Android Build Support 모듈 설치 완료
[ ] Player Settings Bundle ID 설정
[ ] APK 빌드 성공
[ ] 실기기 테스트 완료
```

---

## 6. 빌드 vs 배포 개념 정리

| 구분 | 설명 |
|------|------|
| **빌드 (Build)** | 소스 코드를 실행 가능한 바이너리로 변환. Unity: `.apk` / 백엔드: `npm run build` |
| **배포 (Deploy)** | 빌드된 결과물을 실제 서버/기기에 올려 사용자가 접근할 수 있게 하는 과정 |
| **CI/CD** | 코드 push 시 자동으로 빌드+배포가 실행되는 파이프라인 (GitHub Actions 등) |
