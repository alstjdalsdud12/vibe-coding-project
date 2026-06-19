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

const CHARACTER_INDEX_DESC = `
[캐릭터 인덱스 선택 (characterIndex: 1~8)]
  1: 남자 원시인 (석기시대, 동굴인, 원시 남성)
  2: 여자 원시인 (석기시대, 동굴인, 원시 여성)
  3: 남자 전사 (기사, 검사, 갑옷, 근접 전투 남성)
  4: 여자 궁수 (여성 활잡이, 레인저, 원거리 여성)
  5: 스파르타 (방패+창, 로마·그리스 전사, 글래디에이터)
  6: 아처 (남성 활잡이, 레인저, 원거리 남성)
  7: 특수 군인 (베레모, 나이프, 현대 특수부대)
  8: 일반 군인 (일반 보병, 소총수, 현대 군인)
`;

const SYSTEM_PROMPT = `당신은 RPG 게임의 캐릭터 크리에이터입니다.
유저가 제공한 외형, 무기, 컨셉, 세계관 정보를 바탕으로 캐릭터와 게임 장소를 생성하고
아래 캐릭터 인덱스와 SPUM 파츠 목록을 참고해 외형을 결정합니다.

${CHARACTER_INDEX_DESC}
${SPUM_PARTS}

반드시 아래 JSON 형식만 반환하고 다른 텍스트는 포함하지 마세요:
{
  "name": "캐릭터 이름",
  "characterIndex": 유저 입력에 가장 어울리는 1~8 정수,
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
  "story": "캐릭터 배경 스토리 4~5문장. 반드시 완결된 문장으로 끝낼 것",
  "uniqueSkill": { "name": "고유 스킬 이름", "description": "스킬 효과 설명", "mpCost": 정수, "isPassive": 불리언, "atkMultiplier": 숫자, "healAmount": 정수 },
  "levelSkills": [
    { "name": "레벨5 스킬 이름", "description": "효과 설명", "mpCost": 정수, "isPassive": 불리언, "atkMultiplier": 숫자, "healAmount": 정수 },
    { "name": "레벨10 스킬 이름", "description": "효과 설명", "mpCost": 정수, "isPassive": 불리언, "atkMultiplier": 숫자, "healAmount": 정수 },
    { "name": "레벨15 스킬 이름", "description": "효과 설명", "mpCost": 정수, "isPassive": 불리언, "atkMultiplier": 숫자, "healAmount": 정수 }
  ],
  "locations": [
    { "name": "장소 이름", "description": "분위기 설명 1문장" }
  ]
}

규칙:
- 이름은 컨셉에 어울리는 고유한 이름으로 짓는다
- characterIndex는 유저의 외형·무기·컨셉·성별에 가장 어울리는 번호를 위 목록에서 선택한다
- spumParts는 유저의 외형·무기·컨셉을 종합해 가장 어울리는 파츠를 선택한다
- hair는 캐릭터 성별과 스타일에 맞는 헤어를 위 목록에서 반드시 선택한다 (여성→여성형, 남성→남성형, 불명→중성/공용)
- hairColor는 유저의 외형 설명에서 머리 색상을 추출해 #RRGGBB 형식으로 반환한다 (예: 보라색→#8B00FF, 금발→#FFD700, 검은색→#1A1A1A, 흰색→#F5F5F5, 빨간색→#CC0000)
- 헬멧·방패·백은 컨셉에 맞으면 선택, 아니면 "none"
- HP: 50~200, ATK: 10~100, DEF: 5~80, MP: 0~150
- 스탯은 무기와 컨셉에 논리적으로 연관되어야 한다
- 능력은 1~3개 생성한다
- locations는 세계관에 어울리는 장소 5개를 생성한다
- locations는 난이도 순서로 쉬운 곳부터 어려운 곳 순으로 정렬한다
- story는 유저가 입력한 외형·무기·컨셉·세계관을 바탕으로 자연스럽게 이어지는 서사를 만들고, 반드시 완결된 문장으로 끝낸다
- uniqueSkill은 캐릭터의 컨셉·무기에 완벽히 어울리는 고유 스킬을 생성한다
- 패시브(isPassive: true): mpCost=0, atkMultiplier=1.1~1.5 (모든 공격에 ATK 배율 적용), healAmount=0
- 액티브(isPassive: false): mpCost=20~60, atkMultiplier=1.5~3.0 (사용 시 적에게 피해), 또는 healAmount=30~80 (회복 스킬, 이때 atkMultiplier=0)
- uniqueSkill의 healAmount가 0보다 크면 회복 스킬, atkMultiplier가 0보다 크면 공격 스킬
- levelSkills는 레벨 5, 10, 15에 배우는 스킬 3개이며, 반드시 캐릭터의 무기·컨셉·세계관과 일관된 테마로 만든다 (예: 총을 쏘는 군인/헌터라면 사격·탄환·전술 관련 스킬만 생성하고, 치유처럼 무기/컨셉과 무관한 스킬은 절대 만들지 않는다)
- levelSkills는 레벨이 오를수록 강력해야 한다: 레벨5는 mpCost 25~40·atkMultiplier 1.8~2.2, 레벨10은 mpCost 40~55·atkMultiplier 2.3~3.0, 레벨15는 mpCost 55~75·atkMultiplier 3.0~4.0 (회복형이면 레벨5 healAmount 40~60, 레벨10 healAmount 60~90, 레벨15 healAmount 90~130)
- levelSkills 3개 중 캐릭터 컨셉상 회복/치유가 자연스러운 경우(마법사, 사제, 힐러 등)에만 회복형을 1개 포함시키고, 그렇지 않은 전투 직업(군인, 전사, 궁수 등)은 전부 공격형 또는 공격 버프 패시브로만 구성한다`;

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

const pickCharacterIndex = (appearance, weapon, concept) => {
  const t = `${appearance} ${weapon} ${concept}`.toLowerCase();
  if (/원시|구석기|석기/.test(t) && /여자|여성|woman|female/.test(t)) return 2;
  if (/원시|구석기|석기/.test(t)) return 1;
  if (/궁수|활|archer|bow|ranger/.test(t) && /여자|여성|woman|female/.test(t)) return 4;
  if (/궁수|활|archer|bow|ranger/.test(t)) return 6;
  if (/스파르타|sparta|글래디에이터|방패.*창|창.*방패|gladiator/.test(t)) return 5;
  if (/베레모|특수부대|special forces|나이프/.test(t)) return 7;
  if (/군인|soldier|infantry|보병/.test(t)) return 8;
  if (/여자|여성|woman|female/.test(t)) return 4;
  return 3;
};

const mockResponse = (appearance, weapon, concept, worldview) => ({
  name: '실바나 아쉬크로프트',
  characterIndex: pickCharacterIndex(appearance, weapon, concept),
  spumParts: pickMockParts(weapon, concept, appearance),
  stats: { hp: 85, atk: 35, def: 25, mp: 120 },
  abilities: [
    { name: '정적의 화살', description: 'MP 20 소모. 적에게 ATK×1.8 피해를 주고 1턴 침묵 상태로 만든다.' },
    { name: '마나 방벽', description: 'MP 30 소모. 다음 공격을 완전히 무효화하는 방어막을 생성한다.' },
  ],
  story: `외형이 "${appearance}"이고 ${weapon}을 다루는 ${concept} 캐릭터입니다. (mock 응답)`,
  uniqueSkill: { name: '정밀 사격', description: 'MP 40 소모. ATK×2.0 피해를 입힌다.', mpCost: 40, isPassive: false, atkMultiplier: 2.0, healAmount: 0 },
  levelSkills: [
    { name: '연속 사격', description: 'MP 30 소모. ATK×2.0 피해를 입힌다.', mpCost: 30, isPassive: false, atkMultiplier: 2.0, healAmount: 0 },
    { name: '관통탄', description: 'MP 45 소모. ATK×2.6 피해를 입힌다.', mpCost: 45, isPassive: false, atkMultiplier: 2.6, healAmount: 0 },
    { name: '집중 사격', description: 'MP 65 소모. ATK×3.5 강력한 피해를 입힌다.', mpCost: 65, isPassive: false, atkMultiplier: 3.5, healAmount: 0 },
  ],
  locations: [
    { name: '마을 외곽', description: '초보 모험가들이 처음 발을 내딛는 평화로운 들판.' },
    { name: '어두운 숲', description: '빛이 거의 닿지 않는 울창한 숲으로 야생 몬스터가 출몰한다.' },
    { name: '고대 유적', description: '오래전 문명의 흔적이 남아있는 위험한 던전.' },
    { name: '마왕의 성', description: '어둠의 기운이 가득한 난공불락의 성.' },
    { name: '심연의 탑', description: '최강의 마물이 깃든 전설의 탑. 생환자가 없다.' },
  ],
});

const FOREIGN_CHAR_RE = /[一-鿿㐀-䶿぀-ヿ豈-﫿]/g;

// 소설용: 단어 단위로 외국어 단어 전체 제거 (자모 잔여물 방지)
const sanitizeKorean = (text) => {
  const wordRe = /[一-鿿㐀-䶿぀-ヿa-zA-Z豈-﫿]/;
  return text
    .split('\n')
    .map((line) =>
      line
        .split(' ')
        .filter((word) => word === '' || !wordRe.test(word))
        .join(' '),
    )
    .join('\n')
    .replace(/ {2,}/g, ' ')
    .trim();
};

// 짧은 텍스트(이름·설명)용: 글자 단위로 외국어 문자만 제거
const sanitizeText = (text) =>
  text ? text.replace(FOREIGN_CHAR_RE, '').replace(/ {2,}/g, ' ').trim() : text;

// 플레이 분량(레벨/모험 기록 수)에 따라 소설 분량을 단계별로 조절
const getNovelLengthTier = (level, logCount) => {
  if (level >= 15 || logCount >= 15)
    return { targetChars: 2200, maxTokens: 3200, instruction: '2000~2500자 내외의 장편으로, 여러 단락(필요하면 장 구분처럼 문단을 여러 개로 나누어)으로 써주세요.' };
  if (level >= 5 || logCount >= 5)
    return { targetChars: 1000, maxTokens: 1800, instruction: '900~1100자 내외로 두세 단락에 걸쳐 써주세요.' };
  return { targetChars: 400, maxTokens: 800, instruction: '350~450자 내외의 짧은 일화로 써주세요.' };
};

const generateNovel = async (character) => {
  const charName       = character.generated.name;
  const originalStory  = character.generated.story || '';
  const storyLog       = character.storyLog || [];
  const userInput      = character.userInput || {};
  const stats          = character.generated.stats || {};
  const abilities       = character.generated.abilities || [];
  const uniqueSkill     = character.generated.uniqueSkill;
  const levelSkills     = (character.generated.levelSkills || []).filter((s) => s && s.name);
  const learnedSkills   = (character.learnedSkills || []).filter((s) => s && s.name);
  const locations       = character.generated.locations || [];
  const inventory       = character.inventory || [];
  const level           = character.level || 1;
  const gold            = character.gold || 0;
  const questProgress   = character.questProgress || {};

  if (IS_MOCK) {
    return `[MOCK] ${charName}의 이야기\n\n${originalStory}\n\n— 모험 기록 —\n${storyLog.join('\n')}`;
  }

  const logText = storyLog.join('\n');

  const abilitiesText = abilities.length
    ? abilities.map((a) => `- ${a.name}: ${a.description}`).join('\n') : '없음';
  const skillsText = [
    uniqueSkill ? `- (고유) ${uniqueSkill.name}: ${uniqueSkill.description}` : null,
    ...learnedSkills.filter((s) => !uniqueSkill || s.name !== uniqueSkill.name)
      .map((s) => `- (습득) ${s.name}: ${s.description}`),
  ].filter(Boolean).join('\n') || '없음';
  const locationsText = locations.length
    ? locations.map((l) => `- ${l.name}: ${l.description}`).join('\n') : '없음';
  const inventoryText = inventory.length
    ? inventory.map((i) => `- ${i.name} x${i.qty}`).join('\n') : '없음';

  const lengthTier = getNovelLengthTier(level, storyLog.length);

  const buildPrompt = () => `다음은 "${charName}"이라는 캐릭터의 모든 정보입니다.

[외형/무기/컨셉/세계관]
외형: ${userInput.appearance || '정보 없음'}
무기: ${userInput.weapon || '정보 없음'}
컨셉: ${userInput.concept || '정보 없음'}
세계관: ${userInput.worldview || '정보 없음'}

[배경 스토리]
${originalStory}

[현재 상태]
레벨: ${level}, 골드: ${gold}
스탯: HP ${stats.hp ?? '?'} / ATK ${stats.atk ?? '?'} / DEF ${stats.def ?? '?'} / MP ${stats.mp ?? '?'}
던전 입장 ${questProgress.dungeonCount ?? 0}회, 몬스터 처치 ${questProgress.monsterCount ?? 0}마리

[보유 능력]
${abilitiesText}

[보유 스킬]
${skillsText}

[탐험 지역]
${locationsText}

[소지품]
${inventoryText}

[모험 기록]
${logText}

위 내용을 바탕으로 하나의 자연스러운 단편 소설 형식으로 재구성해주세요.
3인칭 시점으로 작성하고, 캐릭터의 외형·무기·능력·세계관이 이야기 속에 자연스럽게 드러나도록 하고, 모험 기록들을 이야기의 흐름에 맞게 연결해주세요.
[보유 스킬]에 나열된 스킬은 빠짐없이 모두, 적어도 한 번씩은 캐릭터가 전투나 위기 상황에서 실제로 사용하는 장면으로 등장시켜야 합니다 (이름을 그대로 언급할 것).
나머지 항목(능력, 탐험 지역, 소지품 등)은 기계적으로 나열하지 말고 이야기에 녹아드는 것만 자연스럽게 선택해 사용하세요.
${lengthTier.instruction}

[필수 규칙 — 반드시 준수]
- 오직 한국어(한글)로만 작성하세요. 한 글자도 예외 없습니다.
- 한자, 중국어, 일본어 가나, 영어, 로마자, 특수문자는 절대 사용하지 마세요.
- 숫자는 한글로 표기하세요 (예: 3 → 세, 10 → 열).
- 이 캐릭터의 이름은 반드시 "${charName}"입니다. 다른 이름이 나와도 모두 "${charName}"으로 바꾸세요.
- 작성을 마치기 전에 한자나 영어 단어가 섞이지 않았는지 스스로 검토하세요.`;

  const callGroq = async () => {
    const completion = await groq.chat.completions.create({
      model: 'llama-3.3-70b-versatile',
      messages: [{ role: 'user', content: buildPrompt() }],
      temperature: 0.6,
      max_tokens: lengthTier.maxTokens,
    });
    return completion.choices[0].message.content.trim();
  };

  let raw = await callGroq();
  let cleaned = sanitizeKorean(raw);

  // 정제 후 글자 수가 크게 줄었다면(외국어 비중이 높았다면) 한 번 더 시도해 더 깨끗한 쪽을 채택
  if (cleaned.length < raw.length * 0.7) {
    const retryRaw = await callGroq();
    const retryCleaned = sanitizeKorean(retryRaw);
    if (retryCleaned.length > cleaned.length) cleaned = retryCleaned;
  }

  return cleaned;
};

// 스킬 객체가 전투에 필요한 숫자 필드를 모두 갖추고 있는지 검사
const isValidSkill = (s) =>
  s && typeof s.name === 'string' && s.name.length > 0
  && typeof s.mpCost === 'number'
  && typeof s.isPassive === 'boolean'
  && typeof s.atkMultiplier === 'number'
  && typeof s.healAmount === 'number';

// AI 응답이 필수 스킬 스키마(고유 스킬 + 레벨 5/10/15 스킬 3종)를 갖췄는지 검사
const isValidGenerated = (g) =>
  g && isValidSkill(g.uniqueSkill)
  && Array.isArray(g.levelSkills) && g.levelSkills.length === 3
  && g.levelSkills.every(isValidSkill);

// 한자/일본어/영문이 섞인 생성 결과 텍스트를 정리 (캐릭터 정보 팝업·UI에 표시되는 모든 텍스트 대상)
const sanitizeGeneratedTexts = (g) => {
  if (g.story) g.story = sanitizeKorean(g.story);
  const cleanSkill = (s) => {
    if (!s) return;
    if (s.name)        s.name        = sanitizeText(s.name);
    if (s.description) s.description = sanitizeText(s.description);
  };
  cleanSkill(g.uniqueSkill);
  (g.levelSkills || []).forEach(cleanSkill);
  (g.abilities || []).forEach((a) => {
    if (a.name)        a.name        = sanitizeText(a.name);
    if (a.description) a.description = sanitizeText(a.description);
  });
  (g.locations || []).forEach((l) => {
    if (l.name)        l.name        = sanitizeText(l.name);
    if (l.description) l.description = sanitizeText(l.description);
  });
  return g;
};

const generateCharacter = async (appearance, weapon, concept, worldview) => {
  if (IS_MOCK) {
    console.log('[MOCK] Groq API 호출 생략 — mock 응답 반환');
    return mockResponse(appearance, weapon, concept, worldview);
  }

  const callGroq = async () => {
    const completion = await groq.chat.completions.create({
      model: 'llama-3.3-70b-versatile',
      messages: [
        { role: 'system', content: SYSTEM_PROMPT },
        { role: 'user', content: `외형: ${appearance}\n무기: ${weapon}\n컨셉: ${concept}\n세계관: ${worldview}` },
      ],
      response_format: { type: 'json_object' },
      temperature: 0.7,
      max_tokens: 1800,
    });
    return JSON.parse(completion.choices[0].message.content.trim());
  };

  let generated = await callGroq();

  // uniqueSkill/levelSkills가 스키마를 안 지켰으면(필드 누락 등) 한 번 더 시도
  if (!isValidGenerated(generated)) {
    const retry = await callGroq();
    if (isValidGenerated(retry)) generated = retry;
  }

  return sanitizeGeneratedTexts(generated);
};

module.exports = { generateCharacter, generateNovel };
