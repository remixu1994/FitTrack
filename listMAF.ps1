$path = 'D:\GitHub\FitTrack\FitTrack\FitTrack.Copilot\bin\Debug\net10.0\Microsoft.Agents.AI.Abstractions.dll'
$asm = [Reflection.Assembly]::LoadFile($path)
try {
    $asm.GetTypes() | Select-Object -First 40 | ForEach-Object { $_.FullName }
} catch {
    $_.Exception.LoaderExceptions | ForEach-Object { Write-Host 'Loader:' $_.Message }
    throw
}
