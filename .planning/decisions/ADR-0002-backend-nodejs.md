# ADR-0001 — 백엔드 프레임워크: Node.js + Express

## 배경

Unity 클라이언트와 Claude AI, Firebase 사이를 연결하는 백엔드 서버가 필요했다.
후보로 Node.js(Express), Python(FastAPI), Java(Spring)을 검토했다.

## 결정

**Node.js + Express** 채택

## 대안

| 후보 | 장점 | 탈락 이유 |
|------|------|----------|
| Python + FastAPI | AI 라이브러리 풍부 | 학습 곡선, 설정 복잡 |
| Java + Spring | 안정성 | 1인 프로젝트에 과함, 빌드 속도 느림 |

## 이유

1. `@anthropic-ai/sdk`, `firebase-admin` 공식 npm 패키지 제공
2. JavaScript 단일 언어로 프론트·백 맥락 전환 비용 감소
3. 빠른 프로토타이핑에 적합 (Express 보일러플레이트 최소)
4. 수업 환경(Windows)에서 별도 런타임 설치 불필요

## 결과

- `backend/src/app.js` 기준 Express 서버 구현 완료
- Claude API + Firebase Firestore 연동 정상 동작 확인
