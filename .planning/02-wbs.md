# WBS (Work Breakdown Structure)

## 전체 작업 분해 구조

```
AI 기반 캐릭터 생성 2D 모바일 게임
│
├── 1.0 백엔드 서버
│   ├── 1.1 프로젝트 초기화
│   │   ├── 1.1.1 Node.js + Express 세팅
│   │   ├── 1.1.2 Firebase Admin SDK 연동
│   │   └── 1.1.3 .env 환경 변수 구성
│   ├── 1.2 Claude API 연동
│   │   ├── 1.2.1 claudeService.js 구현
│   │   ├── 1.2.2 캐릭터 생성 프롬프트 설계
│   │   └── 1.2.3 JSON 응답 파싱 처리
│   └── 1.3 REST API 구현
│       ├── 1.3.1 POST /api/characters
│       ├── 1.3.2 GET  /api/characters
│       ├── 1.3.3 GET  /api/characters/:id
│       └── 1.3.4 DELETE /api/characters/:id
│
├── 2.0 Firebase
│   ├── 2.1 Firestore 프로젝트 생성
│   ├── 2.2 characters 컬렉션 설계
│   └── 2.3 firebaseService.js 구현
│       ├── 2.3.1 저장 (save)
│       ├── 2.3.2 목록 조회 (getAll)
│       ├── 2.3.3 단건 조회 (getById)
│       └── 2.3.4 삭제 (delete)
│
├── 3.0 Unity 클라이언트
│   ├── 3.1 프로젝트 세팅
│   │   ├── 3.1.1 Unity 2022 LTS 프로젝트 생성
│   │   └── 3.1.2 씬 구성 (MainMenu / CharacterCreation / CharacterDetail / GameScene)
│   ├── 3.2 캐릭터 생성 화면
│   │   ├── 3.2.1 입력 폼 UI (외형, 무기, 컨셉)
│   │   ├── 3.2.2 생성 버튼 및 로딩 인디케이터
│   │   └── 3.2.3 백엔드 API 호출 (UnityWebRequest)
│   ├── 3.3 캐릭터 결과 화면
│   │   ├── 3.3.1 이름, 스탯 표시 UI
│   │   ├── 3.3.2 특수능력 목록 표시
│   │   └── 3.3.3 배경 스토리 텍스트 표시
│   ├── 3.4 캐릭터 목록 화면
│   │   ├── 3.4.1 Firebase에서 목록 불러오기
│   │   ├── 3.4.2 캐릭터 카드 UI
│   │   └── 3.4.3 선택 → 게임 시작 연동
│   └── 3.5 전투 씬
│       ├── 3.5.1 전투 UI (HP바, 행동 버튼, 로그창)
│       ├── 3.5.2 전투 로직 (데미지 계산)
│       ├── 3.5.3 스킬 사용 로직
│       ├── 3.5.4 적 캐릭터 데이터
│       └── 3.5.5 게임 오버 / 클리어 화면
│
├── 4.0 테스트
│   ├── 4.1 백엔드 API 단위 테스트
│   ├── 4.2 Claude mock 테스트
│   └── 4.3 Unity 플레이 테스트
│
└── 5.0 배포
    ├── 5.1 백엔드 서버 배포 (Railway / Render)
    ├── 5.2 Android APK 빌드
    └── 5.3 최종 문서화
```

---

## Session별 작업 매핑

| Session | 작업 항목 |
|---------|----------|
| Session 1 | 문서 작성, 리포지토리 구성 |
| Session 2 | 기획서·WBS·일정표 (현재) |
| Session 3 | 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 3.1 |
| Session 4 | 3.2, 3.3, 3.4 → 프로토타입 |
| Session 5 | 3.5, 4.1, 4.2, 4.3 |
| Session 6 | 5.1, 5.2, 5.3 |
| Session 7 | 발표 자료 준비 |
