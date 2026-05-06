# 프로젝트 계획

## 개요

| 항목 | 내용 |
|------|------|
| 프로젝트명 | AI 기반 캐릭터 생성 2D 모바일 게임 |
| 목표 | MVP: 캐릭터 생성 + Firebase 저장/불러오기 + 텍스트 기반 게임플레이 |
| 기간 | 7주 (Phase 1~3) |

---

## Phase 1 — 백엔드 & Claude 연동 (1~2주차)

### Week 1: 프로젝트 기반 구축

- [ ] 저장소 초기화 및 폴더 구조 세팅 (`backend/`, `game/`, `docs/`)
- [ ] Node.js + Express 백엔드 프로젝트 초기화
- [ ] Claude API 연동 테스트 (단순 텍스트 → JSON 응답 확인)
- [ ] Firebase 프로젝트 생성 및 Firestore 설정
- [ ] Firebase Admin SDK 백엔드 연동 테스트
- [ ] `.env.example` 작성

### Week 2: 캐릭터 생성 API 완성

- [ ] `POST /api/characters` 엔드포인트 구현
- [ ] Claude 프롬프트 설계: 외형/무기/컨셉 입력 → 캐릭터명·스탯·특수능력·스토리 JSON 생성
- [ ] 프롬프트 캐싱 적용 (시스템 프롬프트 `cache_control`)
- [ ] 생성된 캐릭터 Firestore 저장
- [ ] 에러 핸들링 (Claude API 실패, 타임아웃, JSON 파싱 오류)
- [ ] `GET /api/characters`, `GET /api/characters/:id` 구현
- [ ] API 단위 테스트 작성 (Claude mock 처리)

---

## Phase 2 — Unity 게임 클라이언트 (3~5주차)

### Week 3: Unity 프로젝트 세팅 & 캐릭터 생성 화면

- [ ] Unity 2022 LTS 2D 프로젝트 생성
- [ ] 씬 구성: MainMenu, CharacterCreation, CharacterDetail, GameScene
- [ ] 캐릭터 생성 UI 구현
  - 외형/생김새 입력 필드
  - 무기 선택 (버튼 또는 드롭다운)
  - 캐릭터 컨셉 입력 필드
  - "캐릭터 생성" 버튼
- [ ] 백엔드 API 호출 연동 (UnityWebRequest)
- [ ] 로딩 인디케이터 표시

### Week 4: 캐릭터 결과 화면 & Firebase 불러오기

- [ ] 생성 완료 후 캐릭터 정보 화면 구현
  - AI가 지정한 캐릭터명 표시
  - 스탯 수치 (HP, 공격력, 방어력, 마나)
  - 특수능력 목록 및 설명
  - 배경 스토리 텍스트
- [ ] 메인 메뉴: 저장된 캐릭터 목록 불러오기 (Firebase)
- [ ] 캐릭터 선택 → 게임 시작 연동

### Week 5: 텍스트 기반 전투 시스템

- [ ] 전투 씬 UI 구성 (텍스트 로그 방식)
  - 전투 상황 텍스트 출력창
  - 행동 버튼: 일반 공격 / 스킬 사용 / 도망
  - 캐릭터 & 적 HP 바
- [ ] 전투 로직 구현
  - 턴제 또는 실시간 중 택일
  - 스탯 기반 데미지 계산
  - 특수능력 발동 로직
- [ ] 적 캐릭터 데이터 (고정 스탯, 텍스트로 표현)

---

## Phase 3 — 통합 & 마무리 (6~7주차)

### Week 6: 통합 테스트 & 게임 완성

- [ ] 전체 플로우 E2E 테스트 (캐릭터 생성 → 저장 → 불러오기 → 게임 플레이)
- [ ] 게임 오버 / 스테이지 클리어 화면
- [ ] 모바일 해상도 대응 (UI 레이아웃 조정)
- [ ] Firebase 캐릭터 삭제 기능
- [ ] 버그 수정

### Week 7: 배포 & 문서 정리

- [ ] 백엔드 서버 배포 (Railway 또는 Render)
- [ ] Android APK 빌드 및 기기 테스트
- [ ] 최종 문서 업데이트 (README, API 명세)
- [ ] 프로젝트 회고 작성

---

## 마일스톤

| 마일스톤 | 완료 기준 | 목표 주차 |
|----------|-----------|-----------|
| M1: Claude API MVP | 외형/무기/컨셉 입력 → 캐릭터 JSON 반환 + Firestore 저장 | Week 2 |
| M2: 클라이언트 MVP | Unity에서 캐릭터 생성 → 저장 → 목록 불러오기 동작 | Week 4 |
| M3: 게임 MVP | 생성된 캐릭터로 텍스트 전투 1판 플레이 가능 | Week 5 |
| M4: 출시 빌드 | Android APK 정상 동작 + 배포 서버 연결 | Week 7 |

---

## 리스크 & 대응

| 리스크 | 대응 |
|--------|------|
| Claude API 응답 지연 (> 10초) | 로딩 UI 표시, 타임아웃 30초 + 재시도 로직 |
| Claude JSON 형식 오류 | 응답 파싱 실패 시 재요청, 프롬프트에 JSON 스키마 명시 |
| Claude API 비용 초과 | 개발 중 mock 응답 사용, 프롬프트 캐싱으로 비용 절감 |
| Firebase 연결 오류 | 로컬 개발 시 Firebase Emulator 사용 |
| Unity-백엔드 통신 오류 | CORS 설정 확인, 배포 후 HTTPS 적용 |
