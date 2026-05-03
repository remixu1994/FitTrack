param(
    [ValidateRange(1, 65535)]
    [int]$CopilotPort = 5097
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Test-RequiredCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

function Assert-PathExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Container', 'Leaf')]
        [string]$PathType
    )

    if (-not (Test-Path -LiteralPath $Path -PathType $PathType)) {
        throw "$Description not found: $Path"
    }
}

function ConvertTo-SingleQuotedLiteral {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value.Replace("'", "''")
}

$repoRoot = $PSScriptRoot
$copilotDir = Join-Path $repoRoot 'FitTrack\FitTrack.Copilot'
$copilotProject = Join-Path $copilotDir 'FitTrack.Copilot.csproj'
$frontendDir = Join-Path $repoRoot 'FitTrack\FitTrack.React'
$frontendPackageJson = Join-Path $frontendDir 'package.json'
$frontendNodeModules = Join-Path $frontendDir 'node_modules'
$copilotBaseUrl = "http://localhost:$CopilotPort"

Test-RequiredCommand -Name 'dotnet'
Test-RequiredCommand -Name 'npm'

Assert-PathExists -Path $copilotDir -Description 'FitTrack.Copilot directory' -PathType 'Container'
Assert-PathExists -Path $copilotProject -Description 'FitTrack.Copilot project file' -PathType 'Leaf'
Assert-PathExists -Path $frontendDir -Description 'FitTrack.React directory' -PathType 'Container'
Assert-PathExists -Path $frontendPackageJson -Description 'FitTrack.React package.json' -PathType 'Leaf'

if (-not (Test-Path -LiteralPath $frontendNodeModules -PathType Container)) {
    Write-Host 'FitTrack.React dependencies not found. Running npm install...' -ForegroundColor Yellow
    Push-Location $frontendDir
    try {
        & npm install
        if ($LASTEXITCODE -ne 0) {
            throw "npm install failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host 'FitTrack.React dependencies already installed.' -ForegroundColor DarkGray
}

$repoRootLiteral = ConvertTo-SingleQuotedLiteral -Value $repoRoot
$copilotDirLiteral = ConvertTo-SingleQuotedLiteral -Value $copilotDir
$copilotProjectLiteral = ConvertTo-SingleQuotedLiteral -Value $copilotProject
$frontendDirLiteral = ConvertTo-SingleQuotedLiteral -Value $frontendDir
$copilotBaseUrlLiteral = ConvertTo-SingleQuotedLiteral -Value $copilotBaseUrl
$copilotPortLiteral = ConvertTo-SingleQuotedLiteral -Value $CopilotPort.ToString()

$backendCommand = @"
`$Host.UI.RawUI.WindowTitle = 'FitTrack.Copilot'
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
`$env:ASPNETCORE_URLS = '$copilotBaseUrlLiteral'
Set-Location '$copilotDirLiteral'
dotnet watch run --no-launch-profile
"@

$frontendCommand = @"
`$Host.UI.RawUI.WindowTitle = 'FitTrack.React'
`$env:NEXT_PUBLIC_COPILOT_PORT = '$copilotPortLiteral'
Set-Location '$frontendDirLiteral'
npm run dev
"@

$backendProcess = Start-Process -FilePath 'powershell.exe' `
    -WorkingDirectory $repoRoot `
    -ArgumentList @('-NoLogo', '-NoExit', '-ExecutionPolicy', 'Bypass', '-Command', $backendCommand) `
    -PassThru

$frontendProcess = Start-Process -FilePath 'powershell.exe' `
    -WorkingDirectory $frontendDir `
    -ArgumentList @('-NoLogo', '-NoExit', '-ExecutionPolicy', 'Bypass', '-Command', $frontendCommand) `
    -PassThru

Write-Host ''
Write-Host 'Launched FitTrack development services:' -ForegroundColor Green
Write-Host "  FitTrack.Copilot (PID $($backendProcess.Id)): $copilotBaseUrl"
Write-Host "  FitTrack.React   (PID $($frontendProcess.Id)): http://localhost:3000"
Write-Host "  React API port env: NEXT_PUBLIC_COPILOT_PORT=$CopilotPort"
Write-Host ''
Write-Host 'Stop each service by closing its PowerShell window or pressing Ctrl+C inside that window.' -ForegroundColor Cyan
