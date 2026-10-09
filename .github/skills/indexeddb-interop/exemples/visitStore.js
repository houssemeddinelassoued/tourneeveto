// visitStore.js — module ES isolé d'accès à IndexedDB pour TournéeVéto (skill indexeddb-interop, ADR 0001).
// Copie de référence de src/TourneeVeto.Ui/wwwroot/js/visitStore.js, chargé par IndexedDbVisitRepository.
// Toute erreur de stockage est renvoyée sous la forme « TOURNEEVETO_STORAGE:<Quota|Unavailable>:<détail> »,
// convertie en StorageUnavailableException côté C#.

const DB_NAME = "tourneeveto";
const DB_VERSION = 4; // À incrémenter à chaque changement de schéma, avec une nouvelle étape dans upgrade().
const MAX_PHOTO_SIZE = 1600;
const ERROR_PREFIX = "TOURNEEVETO_STORAGE:";

let dbPromise;

// Une étape par version : une base existante ne rejoue que les étapes qui lui manquent.
function upgrade(db, oldVersion, transaction) {
    if (oldVersion < 1) {
        db.createObjectStore("farms", { keyPath: "id" });

        const visits = db.createObjectStore("visits", { keyPath: "id" });
        visits.createIndex("farmId", "farmId");
        visits.createIndex("date", "date");

        const cows = db.createObjectStore("cows", { keyPath: "id" });
        cows.createIndex("farmId", "farmId");

        const photos = db.createObjectStore("photos", { keyPath: "id" });
        photos.createIndex("visitId", "visitId");
    }

    if (oldVersion < 2) {
        // Version 2 : une vache est identifiée par [farmId, id], deux fermes peuvent avoir le même numéro (import CSV, #49).
        // La clé d'un store ne se modifie pas : copie des vaches, recréation du store, réécriture, dans la même transaction.
        const read = transaction.objectStore("cows").getAll();
        read.onsuccess = () => {
            db.deleteObjectStore("cows");
            const cows = db.createObjectStore("cows", { keyPath: ["farmId", "id"] });
            cows.createIndex("farmId", "farmId");
            putAll(cows, read.result);
        };
    }

    if (oldVersion < 3) {
        // Version 3 : saisies de la visite (résultats et note par vache) et réponses au bilan de biosécurité (epic 7, epic 8).
        const records = db.createObjectStore("visitRecords", { keyPath: ["visitId", "cowId"] });
        records.createIndex("visitId", "visitId");
        db.createObjectStore("biosecurity", { keyPath: "visitId" });
    }

    if (oldVersion < 4) {
        // Version 4 : recommandations de la visite, un enregistrement par visite (epic 9). Aucun store existant n'est touché.
        db.createObjectStore("recommendations", { keyPath: "visitId" });
    }
}

function storageError(error) {
    const kind = error?.name === "QuotaExceededError" ? "Quota" : "Unavailable";
    return new Error(`${ERROR_PREFIX}${kind}:${error?.name ?? "Error"} ${error?.message ?? ""}`.trim());
}

function openDb() {
    dbPromise ??= new Promise((resolve, reject) => {
        if (!globalThis.indexedDB) {
            reject(storageError({ name: "NotSupportedError", message: "IndexedDB indisponible dans ce navigateur." }));
            return;
        }

        const request = indexedDB.open(DB_NAME, DB_VERSION);
        request.onupgradeneeded = (event) => upgrade(request.result, event.oldVersion, request.transaction);
        request.onsuccess = () => {
            const db = request.result;
            // Une nouvelle version ouverte ailleurs (autre onglet) : libérer la base pour sa migration.
            db.onversionchange = () => {
                db.close();
                dbPromise = undefined;
            };
            resolve(db);
        };
        request.onerror = () => reject(storageError(request.error));
        request.onblocked = () => reject(storageError({ name: "BlockedError", message: "Base ouverte dans un autre onglet." }));
    });

    // Après un échec, permettre une nouvelle tentative à l'appel suivant.
    dbPromise.catch(() => {
        dbPromise = undefined;
    });
    return dbPromise;
}

// Émet les requêtes de façon synchrone dans une transaction ; le résultat est lu quand elle est validée.
async function run(storeNames, mode, work) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        let readResult;
        try {
            const transaction = db.transaction(storeNames, mode);
            transaction.oncomplete = () => resolve(readResult?.());
            transaction.onabort = () => reject(storageError(transaction.error ?? { name: "AbortError", message: "Transaction annulée." }));
            readResult = work(transaction);
        } catch (error) {
            reject(storageError(error));
        }
    });
}

function getAllByIndex(storeName, indexName, key) {
    return run(storeName, "readonly", (transaction) => {
        const request = transaction.objectStore(storeName).index(indexName).getAll(key);
        return () => request.result;
    });
}

// --- Jeu de démonstration : tout ou rien, et seulement si la base est vide (story 2.1) ---

export function seedIfEmpty(farms, cows, visits) {
    return run(["farms", "cows", "visits"], "readwrite", (transaction) => {
        let seeded = false;
        const count = transaction.objectStore("farms").count();
        count.onsuccess = () => {
            if (count.result > 0) {
                return;
            }
            putAll(transaction.objectStore("farms"), farms);
            putAll(transaction.objectStore("cows"), cows);
            putAll(transaction.objectStore("visits"), visits);
            seeded = true;
        };
        return () => seeded;
    });
}

function putAll(store, items) {
    for (const item of items) {
        store.put(item);
    }
}

// --- Élevages ---

export function getFarms() {
    return run("farms", "readonly", (transaction) => {
        const request = transaction.objectStore("farms").getAll();
        return () => request.result;
    });
}

// --- Visites ---

export function getVisitsByDate(date) {
    return getAllByIndex("visits", "date", date);
}

export function getVisit(id) {
    return run("visits", "readonly", (transaction) => {
        const request = transaction.objectStore("visits").get(id);
        return () => request.result ?? null;
    });
}

export function putVisit(visit) {
    return run("visits", "readwrite", (transaction) => {
        transaction.objectStore("visits").put(visit);
    });
}

export function deleteVisit(id) {
    return run(["visits", "photos", "visitRecords", "biosecurity", "recommendations"], "readwrite", (transaction) => {
        transaction.objectStore("visits").delete(id);
        transaction.objectStore("biosecurity").delete(id);
        transaction.objectStore("recommendations").delete(id);
        for (const storeName of ["photos", "visitRecords"]) {
            const store = transaction.objectStore(storeName);
            const cursor = store.index("visitId").openKeyCursor(IDBKeyRange.only(id));
            cursor.onsuccess = () => {
                if (cursor.result) {
                    store.delete(cursor.result.primaryKey);
                    cursor.result.continue();
                }
            };
        }
    });
}

// --- Saisies de la visite (une par vache) et bilan de biosécurité ---

export function getVisitRecords(visitId) {
    return getAllByIndex("visitRecords", "visitId", visitId);
}

export function putVisitRecord(record) {
    return run("visitRecords", "readwrite", (transaction) => {
        transaction.objectStore("visitRecords").put(record);
    });
}

export function getBiosecurity(visitId) {
    return run("biosecurity", "readonly", (transaction) => {
        const request = transaction.objectStore("biosecurity").get(visitId);
        return () => request.result ?? null;
    });
}

export function putBiosecurity(answers) {
    return run("biosecurity", "readwrite", (transaction) => {
        transaction.objectStore("biosecurity").put(answers);
    });
}

// --- Recommandations de la visite (une liste par visite) ---

export function getRecommendations(visitId) {
    return run("recommendations", "readonly", (transaction) => {
        const request = transaction.objectStore("recommendations").get(visitId);
        return () => request.result ?? null;
    });
}

export function putRecommendations(recommendations) {
    return run("recommendations", "readwrite", (transaction) => {
        transaction.objectStore("recommendations").put(recommendations);
    });
}

// --- Vaches ---

export function getCowsByFarm(farmId) {
    return getAllByIndex("cows", "farmId", farmId);
}

export function putCows(cows) {
    return run("cows", "readwrite", (transaction) => putAll(transaction.objectStore("cows"), cows));
}

// Remplace tout le troupeau d'une ferme en une seule transaction : en cas d'échec, l'ancien troupeau reste intact.
export function replaceCows(farmId, cows) {
    return run("cows", "readwrite", (transaction) => {
        const store = transaction.objectStore("cows");
        const cursor = store.index("farmId").openKeyCursor(IDBKeyRange.only(farmId));
        cursor.onsuccess = () => {
            if (cursor.result) {
                store.delete(cursor.result.primaryKey);
                cursor.result.continue();
            } else {
                putAll(store, cows);
            }
        };
    });
}

// --- Photos : redimensionnées et stockées en Blob sans passer par .NET (ADR 0001) ---

export async function addPhotoFromInput(visitId, input) {
    const file = input?.files?.[0];
    if (!file) {
        return null;
    }

    const blob = await resize(file); // Hors transaction : une transaction IndexedDB ne survit pas à un await.
    const id = crypto.randomUUID();
    await run("photos", "readwrite", (transaction) => {
        transaction.objectStore("photos").put({ id, visitId, blob, createdAt: new Date().toISOString() });
    });
    return id;
}

export function deletePhoto(id) {
    return run("photos", "readwrite", (transaction) => {
        transaction.objectStore("photos").delete(id);
    });
}

async function resize(file) {
    const bitmap = await createImageBitmap(file);
    const scale = Math.min(1, MAX_PHOTO_SIZE / Math.max(bitmap.width, bitmap.height));
    const canvas = new OffscreenCanvas(Math.round(bitmap.width * scale), Math.round(bitmap.height * scale));
    canvas.getContext("2d").drawImage(bitmap, 0, 0, canvas.width, canvas.height);
    bitmap.close();
    return canvas.convertToBlob({ type: "image/jpeg", quality: 0.8 });
}
