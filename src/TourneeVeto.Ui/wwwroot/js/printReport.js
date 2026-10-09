// printReport.js — impression du rapport de visite (story 10.2). Chargé en module isolé par ReportPrinter.
// La CSP interdit le JavaScript en ligne : l'appel à window.print() passe obligatoirement par ce module.

// Ouvre la boîte de dialogue d'impression du navigateur (« Enregistrer au format PDF » incluse).
// L'annulation par l'utilisateur ne change rien : la page du rapport reste affichée.
export function printPage() {
    window.print();
}
