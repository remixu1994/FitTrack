Add-Type -AssemblyName System.Reflection.MetadataLoadContext
Add-Type -AssemblyName System.Linq
$path = 'D:\GitHub\FitTrack\FitTrack\FitTrack.Copilot\bin\Debug\net10.0\Microsoft.Agents.AI.Abstractions.dll'
$assemblies = @([object].Assembly.Location)
$assemblies += [System.Runtime.GCSettings].Assembly.Location
$assemblies += [System.Linq.Enumerable].Assembly.Location
$assemblies += ([System.Collections.Generic.List[int]]).Assembly.Location
$resolver = New-Object System.Reflection.Metadata.LoadContext.PathAssemblyResolver($assemblies)
$mlc = New-Object System.Reflection.Metadata.LoadContext.MetadataLoadContext($resolver)
$assembly = $mlc.LoadFromAssemblyPath($path)
$assembly.GetTypes() | ForEach-Object { $_.FullName } | Select-Object -First 40
$mlc.Dispose()
