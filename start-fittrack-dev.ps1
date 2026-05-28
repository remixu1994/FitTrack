param(
    [ValidateRange(1, 65535)]
    [int]$CopilotPort = 5097,

    [ValidateSet('Backend', 'Frontend', 'None')]
    [string]$FocusWindow = 'Backend',

    [string]$AdminEmail = $env:FITTRACK_ADMIN_EMAIL,

    [string]$AdminPassword = $env:FITTRACK_ADMIN_PASSWORD,

    [switch]$ResetAdminPasswordOnStartup,

    [string]$ModelPreset = $env:FITTRACK_MODEL_PRESET,

    [string]$ModelApiKey = $env:FITTRACK_MODEL_API_KEY,

    [string]$ModelEndpoint = $env:FITTRACK_MODEL_ENDPOINT,

    [string]$ModelId = $env:FITTRACK_MODEL_ID,

    [switch]$OverwriteModelConnectorFromEnvironment,

    [string]$UsdaApiKey = $env:USDA_API_KEY
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

function Show-ProcessWindow {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$Process
    )

    $shell = New-Object -ComObject WScript.Shell

    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        Start-Sleep -Milliseconds 250

        try {
            if ($shell.AppActivate($Process.Id)) {
                return $true
            }
        }
        catch {
            # Ignore activation races while the window is still being created.
        }
    }

    return $false
}

function Test-TruthyEnvironmentValue {
    param(
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return @('1', 'true', 'yes', 'y', 'on') -contains $Value.Trim().ToLowerInvariant()
}

function New-EnvironmentAssignment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $literal = ConvertTo-SingleQuotedLiteral -Value $Value.Trim()
    return "`$env:$Name = '$literal'"
}

$repoRoot = $PSScriptRoot
$copilotDir = Join-Path $repoRoot 'FitTrack\FitTrack.Copilot'
$copilotProject = Join-Path $copilotDir 'FitTrack.Copilot.csproj'
$frontendDir = Join-Path $repoRoot 'FitTrack\FitTrack.React'
$frontendPackageJson = Join-Path $frontendDir 'package.json'
$frontendNodeModules = Join-Path $frontendDir 'node_modules'
$copilotBaseUrl = "http://localhost:$CopilotPort"

if ([string]::IsNullOrWhiteSpace($AdminEmail)) {
    $AdminEmail = 'admin@fittrack.local'
}

if ([string]::IsNullOrWhiteSpace($AdminPassword)) {
    $AdminPassword = 'FitTrack123!'
}

if ([string]::IsNullOrWhiteSpace($ModelPreset)) {
    $ModelPreset = 'mimo'
}

$resetAdminPassword = $ResetAdminPasswordOnStartup.IsPresent -or (Test-TruthyEnvironmentValue -Value $env:FITTRACK_ADMIN_RESET_PASSWORD_ON_STARTUP)
$overwriteModelConnector = $OverwriteModelConnectorFromEnvironment.IsPresent -or (Test-TruthyEnvironmentValue -Value $env:FITTRACK_MODEL_OVERWRITE_FROM_ENVIRONMENT)

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

$backendEnvironmentAssignments = @(
    New-EnvironmentAssignment -Name 'Admin__Email' -Value $AdminEmail
    New-EnvironmentAssignment -Name 'Admin__Password' -Value $AdminPassword
    New-EnvironmentAssignment -Name 'Admin__ResetPasswordOnStartup' -Value $resetAdminPassword.ToString().ToLowerInvariant()
    New-EnvironmentAssignment -Name 'ModelConnector__DefaultPreset' -Value $ModelPreset
    New-EnvironmentAssignment -Name 'ModelConnector__ApiKey' -Value $ModelApiKey
    New-EnvironmentAssignment -Name 'ModelConnector__Endpoint' -Value $ModelEndpoint
    New-EnvironmentAssignment -Name 'ModelConnector__ModelId' -Value $ModelId
    New-EnvironmentAssignment -Name 'ModelConnector__OverwriteFromEnvironment' -Value $overwriteModelConnector.ToString().ToLowerInvariant()
    New-EnvironmentAssignment -Name 'USDA__ApiKey' -Value $UsdaApiKey
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$backendEnvironmentBlock = $backendEnvironmentAssignments -join "`r`n"

$backendCommand = @"
`$Host.UI.RawUI.WindowTitle = 'FitTrack.Copilot'
`$env:DOTNET_ENVIRONMENT = 'Development'
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
`$env:ASPNETCORE_URLS = '$copilotBaseUrlLiteral'
$backendEnvironmentBlock
Set-Location '$copilotDirLiteral'
dotnet watch --project '$copilotProjectLiteral' run --no-launch-profile
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
    -WindowStyle Normal `
    -PassThru

$frontendProcess = Start-Process -FilePath 'powershell.exe' `
    -WorkingDirectory $frontendDir `
    -ArgumentList @('-NoLogo', '-NoExit', '-ExecutionPolicy', 'Bypass', '-Command', $frontendCommand) `
    -WindowStyle Normal `
    -PassThru

switch ($FocusWindow) {
    'Backend' {
        [void](Show-ProcessWindow -Process $backendProcess)
    }
    'Frontend' {
        [void](Show-ProcessWindow -Process $frontendProcess)
    }
}

Write-Host ''
Write-Host 'Launched FitTrack development services:' -ForegroundColor Green
Write-Host "  FitTrack.Copilot (PID $($backendProcess.Id)): $copilotBaseUrl"
Write-Host "  FitTrack.React   (PID $($frontendProcess.Id)): http://localhost:3000"
Write-Host "  React API port env: NEXT_PUBLIC_COPILOT_PORT=$CopilotPort"
Write-Host "  Admin email: $AdminEmail"
Write-Host "  Admin password configured: $(-not [string]::IsNullOrWhiteSpace($AdminPassword))"
Write-Host "  Reset admin password on startup: $resetAdminPassword"
Write-Host "  Model connector preset: $ModelPreset"
Write-Host "  Model connector endpoint: $(if ([string]::IsNullOrWhiteSpace($ModelEndpoint)) { 'from Copilot config' } else { $ModelEndpoint })"
Write-Host "  Model connector model: $(if ([string]::IsNullOrWhiteSpace($ModelId)) { 'from Copilot config' } else { $ModelId })"
Write-Host "  Model API key configured: $(-not [string]::IsNullOrWhiteSpace($ModelApiKey))"
Write-Host "  Overwrite model connector from environment: $overwriteModelConnector"
Write-Host "  Focused window: $FocusWindow"
Write-Host ''
Write-Host 'Stop each service by closing its PowerShell window or pressing Ctrl+C inside that window.' -ForegroundColor Cyan
