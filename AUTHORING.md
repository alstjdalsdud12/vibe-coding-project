# 개발 가이드 (AUTHORING)

이 문서는 프로젝트에 기여하거나 코드를 수정할 때 따라야 할 규칙과 관례를 정의합니다.

## 개발 환경 설정

### 필수 도구

- Node.js 20 LTS 이상
- Unity 2022 LTS
- Git
- VS Code (권장 에디터)

### VS Code 권장 확장

- ESLint
- Prettier
- C# (Unity 개발용)
- REST Client (API 테스트)
- Firebase Explorer (선택)

## 코드 스타일

### JavaScript (백엔드)

- 들여쓰기: 스페이스 2칸
- 세미콜론: 사용
- 따옴표: 작은따옴표(`'`)
- 함수: 화살표 함수 선호
- 비동기: `async/await` 사용 (`.then()` 체이닝 금지)

```js
// Good
const generateCharacter = async (input) => {
  const character = await claudeService.generate(input);
  return character;
};

// Bad
function generateCharacter(input) {
  return claudeService.generate(input).then(c => c);
}
```

### 파일 네이밍

| 대상 | 규칙 | 예시 |
|------|------|------|
| JS 파일 | camelCase | `characterService.js` |
| 상수 파일 | UPPER_SNAKE | `API_CONSTANTS.js` |
| Unity C# | PascalCase | `CharacterController.cs` |
| 문서 파일 | kebab-case | `project-plan.md` |

## API 설계 규칙

### 엔드포인트 네이밍

- RESTful 규칙 준수
- 복수형 명사 사용
- 소문자 + 하이픈

```
POST   /api/characters          # 캐릭터 생성 (Claude API 호출)
GET    /api/characters           # 목록 조회 (Firebase)
GET    /api/characters/:id       # 단건 조회 (Firebase)
DELETE /api/characters/:id       # 삭제 (Firebase)
```

### 응답 형식

모든 API 응답은 아래 구조를 따릅니다:

```json
{
  "success": true,
  "data": { },
  "error": null
}
```

오류 응답:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "CLAUDE_API_ERROR",
    "message": "캐릭터 생성에 실패했습니다."
  }
}
```

## AI API 사용 규칙

### Claude API (캐릭터 전체 생성)

- 모델: `claude-sonnet-4-6`
- 프롬프트 캐싱 활성화: 시스템 프롬프트에 `cache_control: { type: 'ephemeral' }` 적용
- 응답은 반드시 JSON만 반환하도록 프롬프트에 명시
- 생성 항목: 캐릭터명, HP/공격력/방어력/마나 스탯, 특수능력 1~3개, 배경 스토리

```js
// Claude 호출 예시 (프롬프트 캐싱 포함)
const response = await anthropic.messages.create({
  model: 'claude-sonnet-4-6',
  max_tokens: 1024,
  system: [
    {
      type: 'text',
      text: SYSTEM_PROMPT,
      cache_control: { type: 'ephemeral' },
    },
  ],
  messages: [{ role: 'user', content: userPrompt }],
});
```

## Firebase 사용 규칙

- Firestore 직접 접근은 백엔드 서버에서만 수행 (클라이언트에서 직접 접근 금지)
- Firebase Admin SDK 사용 (서버 사이드)
- 컬렉션명: `characters`
- 문서 ID: 자동 생성 UUID 사용 (`db.collection('characters').doc()`)

## Git 규칙

### 브랜치 전략

```
main          # 프로덕션 배포 브랜치
develop       # 통합 개발 브랜치
feature/*     # 기능 개발 (예: feature/character-creation)
fix/*         # 버그 수정 (예: fix/claude-timeout)
```

### 커밋 메시지

```
feat: 캐릭터 생성 API 엔드포인트 추가
fix: Claude 응답 JSON 파싱 오류 수정
refactor: Firebase 서비스 모듈 분리
docs: API 명세 업데이트
```

## 테스트

- 모든 API 엔드포인트에 단위 테스트 작성
- Claude API 호출은 mock 처리 (비용 절감)
- Firebase는 Firebase Emulator Suite 사용 (로컬 테스트)
- `npm test`로 전체 테스트 실행

## 환경 변수 관리

- `.env` 파일은 절대 커밋하지 않음 (`.gitignore`에 포함)
- 새 환경 변수 추가 시 `.env.example`에도 키 이름(값 없이) 추가
- API 키 및 Firebase 서비스 계정 키는 코드에 하드코딩 금지
