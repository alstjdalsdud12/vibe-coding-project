const express = require('express');
const router = express.Router();
const { generateCharacter, generateNovel } = require('../services/claudeService');
const { saveCharacter, getAllCharacters, getCharacterById, deleteCharacter, updateCharacterState } = require('../services/firebaseService');

// 캐릭터 생성
router.post('/', async (req, res, next) => {
  try {
    const { name, appearance, weapon, concept, worldview } = req.body;

    if (!name || !appearance || !weapon || !concept || !worldview) {
      return res.status(400).json({
        success: false,
        data: null,
        error: { code: 'MISSING_FIELDS', message: '이름, 외형, 무기, 컨셉, 세계관을 모두 입력해주세요.' },
      });
    }

    const generated = await generateCharacter(appearance, weapon, concept, worldview);
    const aiName = generated.name;
    generated.name = name;
    // AI가 스토리 안에 박은 자체 이름을 유저 이름으로 교체
    if (generated.story && aiName && aiName !== name) {
      generated.story = generated.story.split(aiName).join(name);
    }
    const character = await saveCharacter({ appearance, weapon, concept, worldview }, generated);

    res.status(201).json({ success: true, data: character, error: null });
  } catch (err) {
    next(err);
  }
});

// 전체 목록 조회
router.get('/', async (req, res, next) => {
  try {
    const characters = await getAllCharacters();
    res.json({ success: true, data: characters, error: null });
  } catch (err) {
    next(err);
  }
});

// 단건 조회
router.get('/:id', async (req, res, next) => {
  try {
    const character = await getCharacterById(req.params.id);
    res.json({ success: true, data: character, error: null });
  } catch (err) {
    next(err);
  }
});

// 게임 상태 저장 (인벤토리, 골드, 퀘스트, 출석, 원정)
router.patch('/:id/state', async (req, res, next) => {
  try {
    await updateCharacterState(req.params.id, req.body);
    res.json({ success: true, data: null, error: null });
  } catch (err) {
    next(err);
  }
});

// 스토리 기록 → 소설 생성
router.post('/:id/novel', async (req, res, next) => {
  try {
    const character = await getCharacterById(req.params.id);
    const log = character.storyLog;
    if (!log || log.length === 0) {
      return res.status(400).json({
        success: false,
        data: null,
        error: { code: 'NO_LOG', message: '모험 기록이 없습니다.' },
      });
    }
    const novel = await generateNovel(
      character.generated.name,
      character.generated.story || '',
      log,
    );
    res.json({ success: true, data: novel, error: null });
  } catch (err) {
    next(err);
  }
});

// 삭제
router.delete('/:id', async (req, res, next) => {
  try {
    await deleteCharacter(req.params.id);
    res.json({ success: true, data: null, error: null });
  } catch (err) {
    next(err);
  }
});

module.exports = router;
