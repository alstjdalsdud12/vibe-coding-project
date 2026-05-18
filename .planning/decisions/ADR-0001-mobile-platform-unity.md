# ADR-0001 — 모바일 플랫폼: Unity 2D

## 배경

텍스트 기반 2D 모바일 RPG를 개발하기 위해 플랫폼을 선택해야 했다.
후보로 Unity, Flutter, React Native, Android(Kotlin)을 검토했다.

## 결정

**Unity 2D** 채택

## 대안

| 후보 | 장점 | 탈락 이유 |
|------|------|----------|
| Flutter | 빠른 UI 개발, Dart 단순 | 게임 엔진 기능 없음, 전투 로직 구현 복잡 |
| React Native | JS 생태계, 익숙함 | 게임 루프·씬 관리 직접 구현 필요 |
| Android (Kotlin) | 풀 네이티브 | iOS 미지원, 게임 기능 없음 |

## 이유

1. 씬(Scene) 단위 화면 관리 — 타이틀/메뉴/전투 전환이 내장 기능으로 지원
2. Android/iOS 동시 빌드 — 한 프로젝트로 양 플랫폼 배포 가능
3. 게임 루프(Update), 충돌, 애니메이션 등 게임 전용 기능 내장
4. C# 스크립트로 전투 로직 구현이 직관적
5. 텍스트 기반 UI(TextMeshPro) 공식 지원

## 결과

- Unity 2022.3 LTS 설치 완료
- `game/VibeCodingGame/` 프로젝트 생성 및 GitHub push 완료
