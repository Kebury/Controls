# UTF-8 with BOM
# ========================================
# ОДИН СКРИПТ ДЛЯ СОЗДАНИЯ EXE ИНСТАЛЯТОРА
# ========================================

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   SOZDANIE EXE INSTALYATORA Controls        " -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# NASTROYKI
# ============================================
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
# Skript nahoditsya v src/Controls.Installer
$srcPath = Split-Path -Parent $scriptPath
$appProjectPath = Join-Path $srcPath "Controls\Controls"
$appProjectFile = Join-Path $appProjectPath "Controls.csproj"
$publishPath = Join-Path $appProjectPath "bin\$Configuration\net10.0-windows\publish"
# Output v koren proyekta Controls/
$rootPath = Split-Path -Parent $srcPath
$outputPath = Join-Path $rootPath "bin\$Configuration"
$innoSetupScript = Join-Path $scriptPath "Controls.iss"
# Pryamaya ssylka na Inno Setup 6.7.1 (poslednaya versiya)
$innoSetupUrl = "https://files.jrsoftware.org/is/6/innosetup-6.7.1.exe"
$innoSetupInstaller = Join-Path $env:TEMP "innosetup.exe"

# Proverka puti k proyektu
if (-not (Test-Path $appProjectFile)) {
    Write-Host ""
    Write-Host "ERROR: Proyekt ne nayden!" -ForegroundColor Red
    Write-Host "Ozhidayemsya: $appProjectFile" -ForegroundColor Yellow
    Write-Host "Tekushchaya papka skripta: $scriptPath" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

# ============================================
# SHAG 1: PROVERKA .NET SDK
# ============================================
Write-Host "[1/5] Proverka .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = & dotnet.exe --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  OK .NET SDK version: $dotnetVersion" -ForegroundColor Green
    } else {
        throw "dotnet ne nayden"
    }
} catch {
    Write-Host "  ERROR: .NET SDK ne nayden!" -ForegroundColor Red
    Write-Host "  Ustanovite .NET 10 SDK s: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# ============================================
# SHAG 2: PROVERKA INNO SETUP
# ============================================
Write-Host ""
Write-Host "[2/5] Proverka Inno Setup..." -ForegroundColor Yellow

# Poisk Inno Setup
$innoCompiler = $null
$possiblePaths = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $innoCompiler = $path
        break
    }
}

if (-not $innoCompiler) {
    Write-Host "  WARNING: Inno Setup ne nayden" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Inno Setup nuzhen dlya sozdaniya EXE instalyatora." -ForegroundColor White
    Write-Host ""
    
    $install = Read-Host "  Skachat i ustanovit Inno Setup avtomaticheski? (Y/N)"
    
    if ($install -eq 'Y' -or $install -eq 'y') {
        Write-Host ""
        Write-Host "  Skachivaniye Inno Setup..." -ForegroundColor Yellow
        
        try {
            # Skachat instalyator (s otobrazheniyem progressa)
            $ProgressPreference = 'Continue'
            Invoke-WebRequest -Uri $innoSetupUrl -OutFile $innoSetupInstaller -UseBasicParsing -TimeoutSec 30
            
            if (Test-Path $innoSetupInstaller) {
                $fileSize = (Get-Item $innoSetupInstaller).Length / 1MB
                Write-Host "  OK Inno Setup skachan ($([math]::Round($fileSize, 2)) MB)" -ForegroundColor Green
                Write-Host ""
                Write-Host "  Ustanovka Inno Setup (sleduyte instrukciyam)..." -ForegroundColor Yellow
                Start-Process -FilePath $innoSetupInstaller -Wait
                
                # Udalit instalyator
                Remove-Item $innoSetupInstaller -ErrorAction SilentlyContinue
            }
            
            # Proverit snova
            foreach ($path in $possiblePaths) {
                if (Test-Path $path) {
                    $innoCompiler = $path
                    break
                }
            }
            
            if ($innoCompiler) {
                Write-Host "  OK Inno Setup ustanovlen!" -ForegroundColor Green
            } else {
                Write-Host ""
                Write-Host "  WARNING: Inno Setup ne obnaruzhen" -ForegroundColor Yellow
                Write-Host "  Vozmozhno, nuzhno zapustit skript snova" -ForegroundColor Yellow
                exit 1
            }
        } catch {
            Write-Host ""
            Write-Host "  ERROR skachivaniya: $_" -ForegroundColor Red
            Write-Host ""
            Write-Host "  RESHENIYE: Ustanovite Inno Setup vruchnuyu" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "  Variant 1 - Otkroyte v brauzere:" -ForegroundColor White
            Write-Host "  https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "  Variant 2 - Pryamaya ssylka:" -ForegroundColor White
            Write-Host "  https://files.jrsoftware.org/is/6/innosetup-6.7.1.exe" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "  Posle ustanovki zapustite etot skript snova" -ForegroundColor White
            Write-Host ""
            
            $openBrowser = Read-Host "  Otkryt stranicu skachivaniya v brauzere? (Y/N)"
            if ($openBrowser -eq 'Y' -or $openBrowser -eq 'y') {
                Start-Process "https://jrsoftware.org/isdl.php"
            }
            exit 1
        }
    } else {
        Write-Host ""
        Write-Host "  Ustanovite Inno Setup vruchnuyu:" -ForegroundColor Yellow
        Write-Host "  1. Otkroyte: https://jrsoftware.org/isdl.php" -ForegroundColor White
        Write-Host "  2. Skachayte 'Inno Setup 6'" -ForegroundColor White
        Write-Host "  3. Ustanovite s nastroykami po umolchaniyu" -ForegroundColor White
        Write-Host "  4. Zapustite etot skript snova" -ForegroundColor White
        Write-Host ""
        Start-Process "https://jrsoftware.org/isdl.php"
        exit 1
    }
} else {
    Write-Host "  OK Inno Setup nayden: $innoCompiler" -ForegroundColor Green
}

# ============================================
# SHAG 3: SBORKA PRILOZHENIYA
# ============================================
Write-Host ""
Write-Host "[3/5] Sborka prilozheniya Controls..." -ForegroundColor Yellow

Push-Location $appProjectPath
try {
    # Vosstanovleniye paketov
    Write-Host "  Vosstanovleniye paketov..." -ForegroundColor Gray
    $restoreOutput = & dotnet.exe restore "$appProjectFile" 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "  Oshibka vosstanovleniya paketov:" -ForegroundColor Red
        Write-Host $restoreOutput -ForegroundColor Gray
        throw "Ne udayotsya vosstanovit NuGet pakety"
    }
    
    # Sborka
    Write-Host "  Kompilyaciya proyekta..." -ForegroundColor Gray
    $buildOutput = & dotnet.exe build "$appProjectFile" --configuration $Configuration 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "  Oshibka kompilyacii:" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Gray
        throw "Ne udayotsya sobrat proyekt"
    }
    
    Write-Host "  OK Prilozheniye sobrano" -ForegroundColor Green
} catch {
    Write-Host ""
    Write-Host "  ERROR sborki: $_" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

# ============================================
# SHAG 4: PUBLIKACIYA PRILOZHENIYA
# ============================================
Write-Host ""
Write-Host "[4/5] Publikaciya prilozheniya..." -ForegroundColor Yellow

Push-Location $appProjectPath
try {
    # Ochistka staroy papki publish
    if (Test-Path $publishPath) {
        Remove-Item -Path $publishPath -Recurse -Force
    }
    
    # Publikaciya (self-contained - vse zavisimosti vklyucheny)
    Write-Host "  Sborka gotovoy versii (s .NET Runtime vnutri)..." -ForegroundColor Gray
    $publishOutput = & dotnet.exe publish "$appProjectFile" `
        --configuration $Configuration `
        --output $publishPath `
        --self-contained true `
        --runtime win-x64 `
        /p:PublishSingleFile=false `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        2>&1 | Out-String
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "  Oshibka publikacii:" -ForegroundColor Red
        Write-Host $publishOutput -ForegroundColor Gray
        throw "Ne udayotsya opublikovat proyekt"
    }
    
    # Proverka razmera
    if (Test-Path $publishPath) {
        $totalSize = (Get-ChildItem -Path $publishPath -Recurse -File | Measure-Object -Property Length -Sum).Sum
        $sizeMB = [math]::Round($totalSize / 1MB, 2)
        Write-Host "  OK Prilozheniye opublikovano: $publishPath" -ForegroundColor Green
        Write-Host "  Razmer: $sizeMB MB (vklyuchayet .NET Runtime)" -ForegroundColor Gray
    }
} catch {
    Write-Host ""
    Write-Host "  ERROR publikacii: $_" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

# ============================================
# SHAG 5: SOZDANIYE EXE INSTALYATORA
# ============================================
Write-Host ""
Write-Host "[5/5] Sozdaniye EXE instalyatora..." -ForegroundColor Yellow

# Sozdat vykhodnuyu papku
if (-not (Test-Path $outputPath)) {
    New-Item -Path $outputPath -ItemType Directory -Force | Out-Null
}

# Kompilyaciya Inno Setup skripta
try {
    Write-Host "  Kompilyaciya instalyatora..." -ForegroundColor Gray
    
    $arguments = "/Q `"$innoSetupScript`""
    $process = Start-Process -FilePath $innoCompiler -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    
    if ($process.ExitCode -ne 0) {
        throw "Inno Setup vernul kod oshibki: $($process.ExitCode)"
    }
    
    Write-Host "  OK EXE instalyator sozdan!" -ForegroundColor Green
} catch {
    Write-Host "  ERROR sozdaniya instalyatora: $_" -ForegroundColor Red
    exit 1
}

# ============================================
# РЕЗУЛЬТАТ
# ============================================
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "             GOTOVO!                            " -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

$exePath = Join-Path $outputPath "ControlsSetup.exe"

if (Test-Path $exePath) {
    $exeInfo = Get-Item $exePath
    
    Write-Host "Instalyator sozdan:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   File:   $exePath" -ForegroundColor White
    Write-Host "   Size:   $([math]::Round($exeInfo.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "   Date:   $($exeInfo.LastWriteTime)" -ForegroundColor White
    Write-Host ""
    Write-Host "Etot EXE file gotov k rasprostraneniyu!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Dlya ustanovki:" -ForegroundColor Yellow
    Write-Host "  * Dvazhdy kliknite po ControlsSetup.exe" -ForegroundColor White
    Write-Host "  * Ili zapustite: .\ControlsSetup.exe /SILENT (tikhaya ustanovka)" -ForegroundColor White
    Write-Host ""
    
    # Sprosit ob otkrytii papki
    $open = Read-Host "Otkryt papku s instalyatorom? (Y/N)"
    if ($open -eq 'Y' -or $open -eq 'y') {
        explorer.exe $outputPath
    }
} else {
    Write-Host "ERROR: File instalyatora ne nayden!" -ForegroundColor Red
    Write-Host "Proverte vyvod Inno Setup na nalichiye oshibok" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Instalyator gotov k ispolzovaniyu!" -ForegroundColor Green
Write-Host ""
