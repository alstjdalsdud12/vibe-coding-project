# 앱 실행 흐름

## 전체 흐름 요약

```
유저
 │
 │  외형 / 무기 / 컨셉 입력
 ▼
[Unity — 캐릭터 생성 화면]
 │
 │  POST /api/characters
 ▼
[Node.js 백엔드]
 │
 ├──▶ [Claude API]
 │       외형·무기·컨셉 분석
 │       → 이름·스탯·능력·스토리 JSON 반환
 │
 └──▶ [Firebase Firestore]
         캐릭터 데이터 저장
         → 저장된 ID 반환
 │
 │  생성된 캐릭터 데이터 응답
 ▼
[Unity — 캐릭터 결과 화면]
 │  이름 / 스탯 / 능력 / 스토리 표시
 │
 │  "게임 시작" 버튼
 ▼
[Unity — 전투 씬]
 │  텍스트 기반 턴제 전투
 │  일반 공격 / 스킬 사용 / 도망
 │
 ├──▶ 승리 → 클리어 화면 → 메인 메뉴
 └──▶ 패배 → 게임 오버 화면 → 메인 메뉴
```

---

## 캐릭터 불러오기 흐름

```
유저
 │
 │  앱 실행 → 메인 메뉴
 ▼
[Unity — 메인 메뉴]
 │
 │  GET /api/characters
 ▼
[Node.js 백엔드]
 │
 └──▶ [Firebase Firestore]
         저장된 캐릭터 목록 조회
 │
 │  캐릭터 목록 응답
 ▼
[Unity — 캐릭터 목록 표시]
 │
 ├──▶ 캐릭터 선택 → 게임 시작
 └──▶ 캐릭터 삭제 → DELETE /api/characters/:id
```

---

## API 요청/응답 상세

### 캐릭터 생성 (POST /api/characters)

```
요청 (Unity → 백엔드)
{
  "appearance": "검은 머리, 키 크고 우아함",
  "weapon": "지팡이",
  "concept": "신중하고 차분한 마법사"
}

응답 (백엔드 → Unity)
{
  "success": true,
  "data": {
    "id": "firebase-doc-id",
    "generated": {
      "name": "실바나 아쉬크로프트",
      "stats": { "hp": 85, "atk": 35, "def": 25, "mp": 120 },
      "abilities": [
        { "name": "정적의 화살", "description": "MP 20 소모. ATK×1.8 피해" }
      ],
      "story": "오래된 숲의 끝자락에서..."
    }
  }
}
```

---

## 전투 흐름

```
전투 시작
 │
 ├── 내 턴
 │    ├── 일반 공격  → 데미지 = 내 ATK - 적 DEF (최소 1)
 │    ├── 스킬 사용  → MP 소모 + 스킬별 효과 적용
 │    └── 도망       → 메인 메뉴로 복귀
 │
 └── 적 턴
      └── 적 자동 공격 → 데미지 = 적 ATK - 내 DEF (최소 1)

내 HP ≤ 0  → 게임 오버
적 HP ≤ 0  → 스테이지 클리어
```

---

## 레이어별 역할 요약

| 레이어 | 역할 |
|--------|------|
| Unity (클라이언트) | UI 표시, 유저 입력 처리, 게임 로직 실행 |
| Node.js (백엔드) | API 요청 처리, Claude·Firebase 연결 |
| Claude API | 캐릭터 이름·스탯·능력·스토리 생성 |
| Firebase Firestore | 캐릭터 데이터 영구 저장 |
