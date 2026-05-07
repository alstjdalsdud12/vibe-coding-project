const admin = require('firebase-admin');

if (!admin.apps.length) {
  admin.initializeApp({
    credential: admin.credential.cert({
      projectId: process.env.FIREBASE_PROJECT_ID,
      clientEmail: process.env.FIREBASE_CLIENT_EMAIL,
      privateKey: process.env.FIREBASE_PRIVATE_KEY?.replace(/\\n/g, '\n'),
    }),
  });
}

const db = admin.firestore();
const COLLECTION = 'characters';

const saveCharacter = async (userInput, generated) => {
  const ref = db.collection(COLLECTION).doc();
  const data = {
    id: ref.id,
    userInput,
    generated,
    createdAt: admin.firestore.FieldValue.serverTimestamp(),
  };
  await ref.set(data);
  return data;
};

const getAllCharacters = async () => {
  const snapshot = await db.collection(COLLECTION).orderBy('createdAt', 'desc').get();
  return snapshot.docs.map((doc) => {
    const d = doc.data();
    return {
      id: d.id,
      name: d.generated.name,
      weapon: d.userInput.weapon,
      concept: d.userInput.concept,
      createdAt: d.createdAt,
    };
  });
};

const getCharacterById = async (id) => {
  const doc = await db.collection(COLLECTION).doc(id).get();
  if (!doc.exists) {
    const err = new Error('캐릭터를 찾을 수 없습니다.');
    err.status = 404;
    throw err;
  }
  return doc.data();
};

const deleteCharacter = async (id) => {
  await db.collection(COLLECTION).doc(id).delete();
};

module.exports = { saveCharacter, getAllCharacters, getCharacterById, deleteCharacter };
