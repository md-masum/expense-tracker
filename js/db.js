/* Finance Tracker PWA — Firestore Data Layer
   Global FinanceDB object providing all database operations via Firebase Firestore.
   Requires firebase-config.js to be loaded first (fbAuth, fbDb must be defined). */

const FinanceDB = (() => {
  function auth() {
    return globalThis.fbAuth || null;
  }

  function db() {
    return globalThis.fbDb || null;
  }

  /* ── Collection helper ───────────────────────────────────────────────── */

  /** Returns a Firestore sub-collection reference for the current user. */
  function col(store) {
    const authRef = auth();
    const dbRef = db();
    const uid = authRef?.currentUser?.uid;
    if (!dbRef) throw new Error('Database service unavailable');
    if (!uid) throw new Error('Not authenticated');
    return dbRef.collection('users').doc(uid).collection(store);
  }

  /* ── Generic CRUD ─────────────────────────────────────────────────────── */

  async function getAll(store) {
    const snap = await col(store).get();
    return snap.docs.map(d => ({ id: d.id, ...d.data() }));
  }

  async function getById(store, id) {
    const doc = await col(store).doc(String(id)).get();
    if (!doc.exists) return null;
    return { id: doc.id, ...doc.data() };
  }

  /** Add a new item; returns the generated Firestore document ID (string). */
  async function addItem(store, item) {
    // eslint-disable-next-line no-unused-vars
    const { id: _ignored, ...data } = item;
    const ref = await col(store).add({
      ...data,
      createdAt: data.createdAt || new Date().toISOString(),
    });
    return ref.id;
  }

  /** Upsert an item using its id field as the document key. */
  async function putItem(store, item) {
    const { id, ...data } = item;
    await col(store).doc(String(id)).set(data);
    return id;
  }

  async function deleteItem(store, id) {
    await col(store).doc(String(id)).delete();
  }

  async function clearStore(store) {
    const dbRef = db();
    if (!dbRef) throw new Error('Database service unavailable');
    const snap  = await col(store).get();
    const batch = dbRef.batch();
    snap.docs.forEach(d => batch.delete(d.ref));
    await batch.commit();
  }

  /* ── Transaction helpers ─────────────────────────────────────────────── */

  /**
   * Returns the next seqNo for a transaction within a project.
   * seqNo is a 1-based counter scoped to each project (1, 2, 3 …).
   */
  async function getNextProjectSeq(projectId) {
    const snap = await col('transactions')
      .where('projectId', '==', String(projectId))
      .get();
    if (snap.empty) return 1;
    const maxSeq = snap.docs.reduce((max, d) => {
      const n = d.data().seqNo || 0;
      return n > max ? n : max;
    }, 0);
    return maxSeq + 1;
  }

  async function getTransactionsByProject(projectId) {
    const snap = await col('transactions')
      .where('projectId', '==', String(projectId))
      .get();
    return snap.docs
      .map(d => ({ id: d.id, ...d.data() }))
      .sort((a, b) => {
        // Primary: date descending (latest date first)
        const dateDiff = new Date(b.date) - new Date(a.date);
        if (dateDiff !== 0) return dateDiff;
        // Secondary: seqNo descending (latest entry first within same date)
        return (b.seqNo || 0) - (a.seqNo || 0);
      });
  }

  /**
   * Deletes every transaction linked to the given project.
   * Uses chunked batches to stay safely under Firestore batch write limits.
   * Returns the number of deleted transaction documents.
   */
  async function deleteTransactionsByProject(projectId) {
    const dbRef = db();
    if (!dbRef) throw new Error('Database service unavailable');
    const projectKey = String(projectId);
    const chunkSize = 400;
    let deletedCount = 0;

    while (true) {
      const snap = await col('transactions')
        .where('projectId', '==', projectKey)
        .limit(chunkSize)
        .get();

      if (snap.empty) break;

      const batch = dbRef.batch();
      snap.docs.forEach(doc => batch.delete(doc.ref));
      await batch.commit();
      deletedCount += snap.size;
    }

    return deletedCount;
  }

  /* ── Seed defaults ───────────────────────────────────────────────────── */

  async function seedDefaults() {
    const dbRef = db();
    if (!dbRef) throw new Error('Database service unavailable');
    const snap = await col('categories').limit(1).get();
    if (!snap.empty) return;

    const defaults = [
      { name: 'Sand',       type: 'Expense' },
      { name: 'Labour',     type: 'Expense' },
      { name: 'Brick',      type: 'Expense' },
      { name: 'Materials',  type: 'Expense' },
      { name: 'Investment', type: 'Income'  },
      { name: 'Sale',       type: 'Income'  },
    ];

    const batch = dbRef.batch();
    defaults.forEach(cat => {
      batch.set(col('categories').doc(), cat);
    });
    await batch.commit();
  }

  /* ── Public API ──────────────────────────────────────────────────────── */

  return {
    getAll,
    getById,
    addItem,
    putItem,
    deleteItem,
    clearStore,
    getNextProjectSeq,
    getTransactionsByProject,
    deleteTransactionsByProject,
    seedDefaults,
  };
})();
