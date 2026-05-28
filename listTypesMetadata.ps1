Add-Type -AssemblyName System.Reflection.Metadata
$path = 'D:\GitHub\FitTrack\FitTrack\FitTrack.Copilot\bin\Debug\net10.0\Microsoft.Agents.AI.Abstractions.dll'
$stream = [System.IO.File]::OpenRead($path)
$peReader = New-Object System.Reflection.PortableExecutable.PEReader($stream)
$metadata = $peReader.GetMetadataReader()
$metadata.TypeDefinitions | Select-Object -First 20 | ForEach-Object {
    $typeDef = $metadata.GetTypeDefinition($_)
    $metadata.GetString($typeDef.Namespace) + '.' + $metadata.GetString($typeDef.Name)
}
$stream.Dispose()
