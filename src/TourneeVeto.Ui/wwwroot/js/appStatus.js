// appStatus.js — état de l'application dans le navigateur : connexion, disponibilité hors ligne (story 3.2)
// et nouvelle version en attente (story 3.3). Chargé par AppStatusService (src/TourneeVeto.Ui/Platform).
// Sans service worker (WebView WPF ou MAUI, développement), seules les informations de connexion sont fournies.

let listener;
const report = async () => listener?.invokeMethodAsync("OnStatusChanged", await snapshot());

async function registration() {
    return "serviceWorker" in navigator ? await navigator.serviceWorker.getRegistration() : undefined;
}

async function snapshot() {
    const current = await registration();
    return {
        online: navigator.onLine,
        offlineReady: Boolean(current?.active),
        // Une version attend seulement si une autre contrôle déjà la page (pas au tout premier chargement).
        updateAvailable: Boolean(current?.waiting && navigator.serviceWorker.controller),
    };
}

export async function watch(dotNetListener) {
    listener = dotNetListener;
    window.addEventListener("online", report);
    window.addEventListener("offline", report);

    const current = await registration();
    if (current) {
        current.addEventListener("updatefound", () => current.installing?.addEventListener("statechange", report));
        navigator.serviceWorker.ready.then(report);
        current.update().catch(() => { /* hors ligne : la vérification sera refaite au prochain chargement */ });
    }

    return snapshot();
}

export function unwatch() {
    window.removeEventListener("online", report);
    window.removeEventListener("offline", report);
    listener = undefined;
}

// Active la version en attente puis recharge la page ; les saisies sont déjà enregistrées dans IndexedDB.
export async function applyUpdate() {
    const current = await registration();
    if (!current?.waiting) {
        return false;
    }

    navigator.serviceWorker.addEventListener("controllerchange", () => window.location.reload(), { once: true });
    current.waiting.postMessage("skipWaiting");
    return true;
}
