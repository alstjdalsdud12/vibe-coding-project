const Groq = require('groq-sdk');

const IS_MOCK = process.env.USE_MOCK === 'true' || !process.env.GROQ_API_KEY;

const groq = IS_MOCK ? null : new Groq({ apiKey: process.env.GROQ_API_KEY });

const SPUM_PARTS = `
[헤어 스타일 목록 (성별 참고해서 선택)]
  여성형 : New_Hair_02(뿌까머리/양갈래번), New_Hair_03(단발·귀보임), New_Hair_04(언더컷단발·사이버펑크), New_Hair_09(똑단발+꼭다리), New_Hair_10(중발), New_Hair_12(똥머리), New_Hair_13(숱친 중발), New_Hair_18(높이솟은 단발·마지심슨반토막)
  남성형 : New_Hair_01(앞머리올린 짧은), New_Hair_05(뾰족솟은·앵무새), New_Hair_08(짧은남자), New_Hair_11(단발미소년), New_Hair_14(얇은리젠트·양아치), New_Hair_15(두꺼운리젠트), New_Hair_16(상투·나루토지로보), New_Hair_19(남자중단발)
  중성/공용: New_Hair_06(꼬랑지), New_Hair_07(5:5가르마 중단발), New_Hair_17(중성단발), New_Hair_20(중성중발)
[헬멧 (New_Helmet_01 ~ New_Helmet_10 또는 "none"): 10종 + 없음]
[무기 종류별 목록]
  Sword  : New_Weapon_01, New_Weapon_06, New_Weapon_17, New_Weapon_18, New_Weapon_19, New_Weapon_20
  Spear  : New_Weapon_09, New_Weapon_13, New_Weapon_14, New_Weapon_16
  Axe    : New_Weapon_04, New_Weapon_08
  Bow    : New_Weapon_10, New_Weapon_12, New_Weapon_15
  Wand   : New_Weapon_03
  Dagger : New_Weapon_05, New_Weapon_11
  Mace   : New_Weapon_07
[방패 (New_Shield_01 ~ New_Shield_04 또는 "none"): 4종 + 없음]
[백 악세서리 (New_Back_01, New_Back_02 또는 "none"): 2종 + 없음]
`;

const SYSTEM_PROMPT = `당신은 RPG 게임의 캐릭터 크리에이터입니다.
유저가 제공한 외형, 무기, 컨셉, 세계관 정보를 바탕으로 캐릭터와 게임 장소를 생성하고
아래 SPUM 파츠 목록에서 캐릭터에 어울리는 외형을 조합합니다.

${SPUM_PARTS}

반드시 아래 JSON 형식만 반환하고 다른 텍스트는 포함하지 마세요:
{
  "name": "캐릭터 이름",
  "spumParts": {
    "hair": "New_Hair_XX",
    "hairColor": "#RRGGBB 형식의 16진수 색상 코드",
    "helmet": "New_Helmet_XX 또는 none",
    "weapon": "New_Weapon_XX",
    "weaponType": "Sword|Spear|Axe|Bow|Wand|Dagger|Mace 중 하나",
    "shield": "New_Shield_XX 또는 none",
    "back": "New_Back_XX 또는 none"
  },
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
- spumParts는 유저의 외형·무기·컨셉을 종합해 가장 어울리는 파츠를 선택한다
- hair는 캐릭터 성별과 스타일에 맞는 헤어를 위 목록에서 반드시 선택한다 (여성→여성형, 남성→남성형, 불명→중성/공용)
- hairColor는 유저의 외형 설명에서 머리 색상을 추출해 #RRGGBB 형식으로 반환한다 (예: 보라색→#8B00FF, 금발→#FFD700, 검은색→#1A1A1A, 흰색→#F5F5F5, 빨간색→#CC0000)
- 헬멧·방패·백은 컨셉에 맞으면 선택, 아니면 "none"
- HP: 50~200, ATK: 10~100, DEF: 5~80, MP: 0~150
- 스탯은 무기와 컨셉에 논리적으로 연관되어야 한다
- 능력은 1~3개 생성한다
- locations는 세계관에 어울리는 장소 5개를 생성한다
- locations는 난이도 순서로 쉬운 곳부터 어려운 곳 순으로 정렬한다`;

const hashStr = (s) => {
  let h = 0;
  for (const c of s) h = (Math.imul(31, h) + c.charCodeAt(0)) | 0;
  return Math.abs(h);
};

const pick = (arr, seed) => arr[seed % arr.length];

const pickMockParts = (weapon, concept, appearance = '') => {
  const text = `${weapon} ${concept} ${appearance}`.toLowerCase();
  const seed = hashStr(text);

  const hairColorMap = [
    [/보라|자주|purple/,   '#8B00FF'],
    [/금발|노란|yellow|gold/, '#FFD700'],
    [/빨간|붉은|red/,      '#CC0000'],
    [/파란|blue/,          '#1E90FF'],
    [/초록|green/,         '#228B22'],
    [/흰|백발|white/,      '#F0F0F0'],
    [/갈|brown/,           '#8B4513'],
    [/분홍|핑크|pink/,     '#FF69B4'],
    [/회|gray|grey/,       '#808080'],
  ];
  const hairColor = (hairColorMap.find(([re]) => re.test(text)) || [null, '#1A1A1A'])[1];

  if (/마법|지팡이|주문|마나|소환/.test(text))
    return { hair: pick(['New_Hair_07','New_Hair_15','New_Hair_20'], seed), hairColor, helmet: 'none', weapon: 'New_Weapon_03', weaponType: 'Wand', shield: 'none', back: 'New_Back_01' };
  if (/암살|도적|단검|독|은신/.test(text))
    return { hair: pick(['New_Hair_03','New_Hair_08','New_Hair_13'], seed), hairColor, helmet: 'none', weapon: pick(['New_Weapon_05','New_Weapon_11'], seed), weaponType: 'Dagger', shield: 'none', back: 'none' };
  if (/활|궁수|원거리/.test(text))
    return { hair: pick(['New_Hair_10','New_Hair_16','New_Hair_19'], seed), hairColor, helmet: 'none', weapon: pick(['New_Weapon_10','New_Weapon_12','New_Weapon_15'], seed), weaponType: 'Bow', shield: 'none', back: 'New_Back_02' };
  if (/도끼|처형|망치/.test(text))
    return { hair: pick(['New_Hair_02','New_Hair_06','New_Hair_17'], seed), hairColor, helmet: pick(['New_Helmet_04','New_Helmet_07'], seed), weapon: pick(['New_Weapon_04','New_Weapon_08'], seed), weaponType: 'Axe', shield: 'none', back: 'none' };
  if (/창|스피어|기사|팔라딘/.test(text))
    return { hair: pick(['New_Hair_04','New_Hair_09','New_Hair_14'], seed), hairColor, helmet: pick(['New_Helmet_02','New_Helmet_05'], seed), weapon: pick(['New_Weapon_09','New_Weapon_13','New_Weapon_14','New_Weapon_16'], seed), weaponType: 'Spear', shield: 'none', back: 'none' };

  const hairNum = String(pick([1,2,4,5,6,9,11,12,14,17,18], seed)).padStart(2, '0');
  return {
    hair: `New_Hair_${hairNum}`,
    hairColor,
    helmet: pick(['New_Helmet_01','New_Helmet_03','New_Helmet_06','none'], seed),
    weapon: pick(['New_Weapon_01','New_Weapon_06','New_Weapon_17','New_Weapon_18','New_Weapon_19','New_Weapon_20'], seed),
    weaponType: 'Sword',
    shield: pick(['New_Shield_01','New_Shield_02','New_Shield_03','none'], seed + 1),
    back: 'none',
  };
};

const mockResponse = (appearance, weapon, concept, worldview) => ({
  name: '실바나 아쉬크로프트',
  spumParts: pickMockParts(weapon, concept, appearance),
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
    console.log('[MOCK] Groq API 호출 생략 — mock 응답 반환');
    return mockResponse(appearance, weapon, concept, worldview);
  }

  const completion = await groq.chat.completions.create({
    model: 'llama-3.3-70b-versatile',
    messages: [
      { role: 'system', content: SYSTEM_PROMPT },
      { role: 'user', content: `외형: ${appearance}\n무기: ${weapon}\n컨셉: ${concept}\n세계관: ${worldview}` },
    ],
    response_format: { type: 'json_object' },
    temperature: 0.7,
  });

  const text = completion.choices[0].message.content.trim();
  return JSON.parse(text);
};

module.exports = { generateCharacter };
