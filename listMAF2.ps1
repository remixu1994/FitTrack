$path = 'D:\GitHub\FitTrack\FitTrack\FitTrack.Copilot\bin\Debug\net10.0\Microsoft.Agents.AI.Abstractions.dll'
$asm = [Reflection.Assembly]::LoadFile($path)
try {
    $asm.GetTypes() | Select-Object -First 40 | ForEach-Object { $_.FullName }
} catch {
    foreach ($loader in $_.Exception.LoaderExceptions) {
        Write-Host ('Loader Exception: ' + $loader.Message)
    }
    throw
}
