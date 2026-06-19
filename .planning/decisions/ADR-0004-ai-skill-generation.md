# ADR-0004 — AI 기반 레벨업 스킬 생성

## 배경

레벨 5·10·15 달성 시 새로운 스킬을 획득하는 시스템이 필요했다.
방법으로 (A) 하드코딩된 범용 스킬 표, (B) AI가 캐릭터 컨셉에 맞춰 생성하는 방식을 검토했다.

## 결정

**캐릭터 생성 시 AI가 levelSkills 3개를 함께 생성** (캐릭터 컨셉 반영)

## 대안

| 후보 | 장점 | 탈락 이유 |
|------|------|----------|
| 범용 스킬 표 | 구현 단순, 안정적 | 캐릭터 개성 없음 — 암살자도 치유의 손길 |
| 개별 AI 호출 | 필요 시점에 생성 | 추가 API 비용, 레벨업 시 딜레이 |

## 이유

1. 캐릭터 생성 시 함께 생성하면 추가 API 비용 없음
2. 컨셉(암살자 → 공격형 스킬, 마법사 → MP 절약 스킬)과 일치
3. `generated.levelSkills[0/1/2]`로 저장해 GameState.GetSkillForLevel()에서 즉시 참조

## 구현

```json
"levelSkills": [
  { "name": "Lv5 스킬", "mpCost": 25, "isPassive": false, "atkMultiplier": 1.8 },
  { "name": "Lv10 스킬", "mpCost": 0, "isPassive": true, "atkMultiplier": 1.3 },
  { "name": "Lv15 스킬", "mpCost": 60, "isPassive": false, "atkMultiplier": 3.5 }
]
```

## 결과

- 각 캐릭터가 컨셉에 맞는 고유 스킬 세트를 보유
- isValidGenerated() 검증으로 스킬 스키마 오류 시 재시도
- 구버전 캐릭터 호환: levelSkills 없으면 범용 스킬 대체 적용
