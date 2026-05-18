const express = require('express');
const router = express.Router();
const { generateCharacter } = require('../services/claudeService');
const { saveCharacter, getAllCharacters, getCharacterById, deleteCharacter } = require('../services/firebaseService');

// 캐릭터 생성
router.post('/', async (req, res, next) => {
  try {
    const { appearance, weapon, concept, worldview } = req.body;

    if (!appearance || !weapon || !concept || !worldview) {
      return res.status(400).json({
        success: false,
        data: null,
        error: { code: 'MISSING_FIELDS', message: '외형, 무기, 컨셉, 세계관을 모두 입력해주세요.' },
      });
    }

    const generated = await generateCharacter(appearance, weapon, concept, worldview);
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
