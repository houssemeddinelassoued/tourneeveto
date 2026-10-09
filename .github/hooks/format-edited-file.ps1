# Hook « après modification » commun à Copilot (.github/hooks/format.json) et à Claude Code (.claude/settings.json) :
# formate le ou les fichiers C# que l'agent vient d'écrire.
#
# Entrée : l'événement PostToolUse en JSON sur stdin. Les deux outils ne nomment pas pareil le chemin modifié
# (Claude : tool_input.file_path ; Copilot : tool_input.filePath, ou une liste de fichiers), d'où la recherche large.
#
# Seule la mise en forme des espaces est appliquée (dotnet format whitespace --folder, environ 5 s) : les règles de style
# et d'analyse demandent de charger toute la solution (environ 20 s par modification). Elles restent vérifiées par
# « dotnet format --verify-no-changes ». Les .razor ne sont pas pris en charge par dotnet format.
#
# Sortie : 0 si rien à faire ou si le formatage a réussi ; 1 (erreur non bloquante, affichée) si dotnet format échoue.

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path

function Get-EditedPaths($node) {
    if ($null -eq $node) { return }
    if ($node -is [string]) {
        # Copilot peut transmettre tool_input sous forme de chaîne JSON.
        if ($node.TrimStart().StartsWith('{') -or $node.TrimStart().StartsWith('[')) {
            try { Get-EditedPaths ($node | ConvertFrom-Json) } catch { }
        }
        return
    }
    if ($node -is [System.Collections.IEnumerable]) {
        foreach ($item in $node) {
            if ($item -is [string]) { $item } else { Get-EditedPaths $item }
        }
        return
    }
    foreach ($key in 'file_path', 'filePath', 'path') {
        $value = $node.$key
        if ($value -is [string] -and $value) { $value }
    }
    foreach ($key in 'files', 'filePaths', 'edits') {
        if ($null -ne $node.$key) { Get-EditedPaths $node.$key }
    }
}

try {
    $event = [Console]::In.ReadToEnd() | ConvertFrom-Json
} catch {
    exit 0
}

$include = @(
    @(Get-EditedPaths $event.tool_input) + @(Get-EditedPaths $event.tool_response) |
        Where-Object { $_ -like '*.cs' } |
        ForEach-Object { [System.IO.Path]::GetFullPath($_, $root) } |
        Where-Object { $_.StartsWith($root + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) } |
        ForEach-Object { [System.IO.Path]::GetRelativePath($root, $_) -replace '\\', '/' } |
        # Fichiers générés et sorties de compilation : jamais formatés.
        Where-Object { $_ -notmatch '(^|/)(bin|obj|publish)/' -and (Test-Path -LiteralPath (Join-Path $root $_)) } |
        Sort-Object -Unique
)
if ($include.Count -eq 0) { exit 0 }

$output = & dotnet format whitespace $root --folder --include @include --verbosity quiet 2>&1
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine("dotnet format a échoué pour $($include -join ', ') :`n$($output -join "`n")")
    exit 1
}
exit 0
