const Anthropic = require('@anthropic-ai/sdk');

const IS_MOCK = process.env.USE_MOCK === 'true' || !process.env.ANTHROPIC_API_KEY || !process.env.ANTHROPIC_API_KEY.startsWith('sk-ant-');

const client = IS_MOCK ? null : new Anthropic({ apiKey: process.env.ANTHROPIC_API_KEY });

const SYSTEM_PROMPT = `당신은 RPG 게임의 캐릭터 크리에이터입니다.
유저가 제공한 외형, 무기, 컨셉, 세계관 정보를 바탕으로 캐릭터와 게임 장소를 생성합니다.

반드시 아래 JSON 형식만 반환하고 다른 텍스트는 포함하지 마세요:
{
  "name": "캐릭터 이름",
  "stats": { "hp": 숫자, "atk": 숫자, "def": 숫자, "mp": 숫자 },
  "abilities": [
    { "name": "능력 이름", "description": "효과 설명 (MP 소모량 포함)" }
  ],
  "story": "배경 스토리 2~3문장",
  "locations": [
    { "name": "장소 이름", "description": "분위기 설명 1문장" }
  ]
}

규칙:
- 이름은 컨셉에 어울리는 고유한 이름으로 짓는다
- HP: 50~200, ATK: 10~100, DEF: 5~80, MP: 0~150
- 스탯은 무기와 컨셉에 논리적으로 연관되어야 한다
- 능력은 1~3개 생성한다
- locations는 세계관에 어울리는 장소 5개를 생성한다
- locations는 난이도 순서로 쉬운 곳부터 어려운 곳 순으로 정렬한다`;

const mockResponse = (appearance, weapon, concept, worldview) => ({
  name: '실바나 아쉬크로프트',
  stats: { hp: 85, atk: 35, def: 25, mp: 120 },
  abilities: [
    { name: '정적의 화살', description: 'MP 20 소모. 적에게 ATK×1.8 피해를 주고 1턴 침묵 상태로 만든다.' },
    { name: '마나 방벽', description: 'MP 30 소모. 다음 공격을 완전히 무효화하는 방어막을 생성한다.' },
  ],
  story: `외형이 "${appearance}"이고 ${weapon}을 다루는 ${concept} 캐릭터입니다. (mock 응답)`,
  locations: [
    { name: '마을 외곽', description: '초보 모험가들이 처음 발을 내딛는 평화로운 들판.' },
    { name: '어두운 숲', description: '빛이 거의 닿지 않는 울창한 숲으로 야생 몬스터가 출몰한다.' },
    { name: '고대 유적', description: '오래전 문명의 흔적이 남아있는 위험한 던전.' },
    { name: '마왕의 성', description: '어둠의 기운이 가득한 난공불락의 성.' },
    { name: '심연의 탑', description: '최강의 마물이 깃든 전설의 탑. 생환자가 없다.' },
  ],
});

const generateCharacter = async (appearance, weapon, concept, worldview) => {
  if (IS_MOCK) {
    console.log('[MOCK] Claude API 호출 생략 — mock 응답 반환');
    return mockResponse(appearance, weapon, concept, worldview);
  }

  const response = await client.messages.create({
    model: 'claude-sonnet-4-6',
    max_tokens: 1024,
    system: [
      {
        type: 'text',
        text: SYSTEM_PROMPT,
        cache_control: { type: 'ephemeral' },
      },
    ],
    messages: [
      {
        role: 'user',
        content: `외형: ${appearance}\n무기: ${weapon}\n컨셉: ${concept}\n세계관: ${worldview}`,
      },
    ],
  });

  const text = response.content[0].text;
  return JSON.parse(text);
};

module.exports = { generateCharacter };
