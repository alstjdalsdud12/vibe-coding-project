# ADR-0003 — 데이터베이스: Firebase Firestore

## 배경

생성된 캐릭터 데이터를 저장하고 불러오는 데이터베이스가 필요했다.
후보로 Firebase Firestore, MySQL, MongoDB Atlas, SQLite를 검토했다.

## 결정

**Firebase Firestore** 채택

## 대안

| 후보 | 장점 | 탈락 이유 |
|------|------|----------|
| MySQL | 안정성, SQL 표준 | 서버 별도 구축 필요, 스키마 설계 복잡 |
| MongoDB Atlas | 유연한 문서 구조 | 무료 티어 제한, 설정 복잡 |
| SQLite | 로컬 간단 | 서버 공유 불가, 모바일 동기화 없음 |

## 이유

1. 서버리스 — 별도 DB 서버 구축 없이 바로 사용 가능
2. Firebase Admin SDK로 Node.js 백엔드에서 간단하게 연동
3. JSON 형태의 캐릭터 데이터 구조와 NoSQL 문서 모델이 자연스럽게 일치
4. 무료 티어(Spark Plan)로 소규모 프로젝트에 충분
5. 자동 ID 생성, 타임스탬프 등 편의 기능 내장

## 결과

- Firebase 프로젝트 생성 및 Firestore 활성화 완료
- `backend/src/services/firebaseService.js` 연동 구현 완료
- 캐릭터 저장/조회/삭제 API 동작 확인 완료
