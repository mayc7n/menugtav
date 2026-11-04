[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ScriptHookVDotNet3
)
$ErrorActionPreference = 'Stop'
$raizProjeto = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$codigoFonte = Join-Path $raizProjeto 'desenvolvimento\src\MikeMenu.3.cs'
$testes = Join-Path $raizProjeto 'desenvolvimento\tests\TestesRegras.cs'
$referencia = (Resolve-Path -LiteralPath $ScriptHookVDotNet3).Path
$compilador = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilador -PathType Leaf)) { throw 'Compilador .NET Framework não encontrado.' }
$pastaCompilacao = Join-Path $raizProjeto '.build'
$pastaDistribuicao = Join-Path $raizProjeto 'scripts'
New-Item -ItemType Directory -Force $pastaCompilacao, $pastaDistribuicao | Out-Null
$executavelTeste = Join-Path $pastaCompilacao 'TestesRegras.exe'
$binario = Join-Path $pastaDistribuicao 'MikeMenu.3.dll'
$pacote = Join-Path $raizProjeto 'MikeMenu.zip'
& $compilador /nologo /define:TEST "/out:$executavelTeste" $codigoFonte $testes
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes.' }
& $executavelTeste
if ($LASTEXITCODE -ne 0) { throw 'Os testes falharam.' }
& $compilador /nologo /target:library "/out:$binario" "/reference:$referencia" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $codigoFonte
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o menu.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$arquivoTemporario = Join-Path $pastaCompilacao 'MikeMenu.zip'
if (Test-Path -LiteralPath $arquivoTemporario) { Remove-Item -LiteralPath $arquivoTemporario -Force }
$arquivoZip = [System.IO.Compression.ZipFile]::Open($arquivoTemporario, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($arquivoZip, $binario, 'scripts/MikeMenu.3.dll')
    [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($arquivoZip, (Join-Path $raizProjeto 'desenvolvimento\LEIA-ME.txt'), 'LEIA-ME.txt')
} finally { $arquivoZip.Dispose() }
Copy-Item -LiteralPath $arquivoTemporario -Destination $pacote -Force
Write-Host "Pacote criado: $pacote"
