[CmdletBinding()]
param(
    [string]$UnityPath,
    [switch]$SkipUnity,
    [switch]$SkipDotNetBuild
)

# Verificacion reproducible de Library Manager: compila el proyecto de pruebas con
# dotnet (opcional) y ejecuta la suite Unity EditMode en batch.

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot "LibraryManager"
$projectVersionPath = Join-Path $projectPath "ProjectSettings/ProjectVersion.txt"
$testProjectPath = Join-Path $projectPath "LibraryManager.Tests.EditMode.csproj"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$resultDirectory = Join-Path $repositoryRoot "TestResults/Verification/$timestamp"

if (-not (Test-Path -LiteralPath $projectVersionPath)) {
    throw "No se encontro un proyecto Unity en '$projectPath'."
}

New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null

$projectVersionLine = Get-Content -LiteralPath $projectVersionPath |
    Where-Object { $_ -like "m_EditorVersion:*" } |
    Select-Object -First 1

if (-not $projectVersionLine) {
    throw "No se pudo leer la version de Unity desde '$projectVersionPath'."
}

$projectVersion = ($projectVersionLine -split ":", 2)[1].Trim()

if (-not $SkipDotNetBuild) {
    if (-not (Test-Path -LiteralPath $testProjectPath)) {
        throw "Unity debe generar '$testProjectPath' antes de ejecutar la comprobacion dotnet."
    }

    Write-Host "Restaurando el proyecto EditMode..."
    & dotnet restore $testProjectPath --nologo -v:q
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore fallo con codigo $LASTEXITCODE."
    }

    Write-Host "Compilando el proyecto EditMode..."
    & dotnet build $testProjectPath --no-restore --nologo -v:q
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build fallo con codigo $LASTEXITCODE."
    }
}

if ($SkipUnity) {
    Write-Host "Verificacion parcial completada. La suite Unity EditMode no se ejecuto."
    Write-Host "Resultados: $resultDirectory"
    exit 0
}

if (-not $UnityPath) {
    $UnityPath = "C:/Program Files/Unity/Hub/Editor/$projectVersion/Editor/Unity.exe"
}

if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "No se encontro Unity $projectVersion en '$UnityPath'. Usa -UnityPath para indicar el ejecutable."
}

$normalizedProjectPath = [System.IO.Path]::GetFullPath($projectPath).TrimEnd('\')
$openProjectProcesses = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" |
    Where-Object {
        $_.CommandLine -and
        $_.CommandLine.IndexOf($normalizedProjectPath, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    }

if ($openProjectProcesses) {
    throw "Cierra Unity para este proyecto antes de ejecutar EditMode en batch. No uses dos editores sobre la misma carpeta."
}

$testResultsPath = Join-Path $resultDirectory "editmode-results.xml"
$unityLogPath = Join-Path $resultDirectory "unity-editmode.log"

Write-Host "Ejecutando la suite Unity EditMode con Unity $projectVersion..."
$unityArguments = @(
    "-batchmode",
    "-nographics",
    "-projectPath", "`"$projectPath`"",
    "-runTests",
    "-testPlatform", "EditMode",
    "-testResults", "`"$testResultsPath`"",
    "-logFile", "`"$unityLogPath`""
)
$unityProcess = Start-Process `
    -FilePath $UnityPath `
    -ArgumentList $unityArguments `
    -Wait `
    -PassThru

if ($unityProcess.ExitCode -ne 0) {
    throw "Unity EditMode fallo con codigo $($unityProcess.ExitCode). Revisa '$unityLogPath'."
}

if (-not (Test-Path -LiteralPath $testResultsPath)) {
    throw "Unity termino sin generar '$testResultsPath'. Revisa '$unityLogPath'."
}

[xml]$testResults = Get-Content -LiteralPath $testResultsPath -Raw
$testRun = $testResults.'test-run'

if (-not $testRun) {
    throw "El resultado de Unity no contiene un nodo test-run valido."
}

Write-Host (
    "Unity EditMode: total={0}, correctas={1}, fallidas={2}, omitidas={3}" -f
    $testRun.total,
    $testRun.passed,
    $testRun.failed,
    $testRun.skipped
)
Write-Host "Resultados: $resultDirectory"

if ([int]$testRun.failed -gt 0) {
    exit 1
}
