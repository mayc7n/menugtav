[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ScriptHookVDotNet3
)
$ErrorActionPreference = 'Stop'
$raizProjeto = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$codigoFonte = Join-Path $raizProjeto 'desenvolvimento\src\MikeMenu.3.cs'
$previaFonte = Join-Path $raizProjeto 'desenvolvimento\src\PreviaMikeMenu.cs'
$favoritosFonte = Join-Path $raizProjeto 'desenvolvimento\src\FavoritosMikeMenu.cs'
$companheirosFonte = Join-Path $raizProjeto 'desenvolvimento\src\CompanheirosMikeMenu.cs'
$extrasFonte = Join-Path $raizProjeto 'desenvolvimento\src\ExtrasMikeMenu.cs'
$atalhosFonte = Join-Path $raizProjeto 'desenvolvimento\src\AtalhosMikeMenu.cs'
$mapaFonte = Join-Path $raizProjeto 'desenvolvimento\src\MapaPreviaMikeMenu.cs'
$mundoExtraFonte = Join-Path $raizProjeto 'desenvolvimento\src\MundoExtraMikeMenu.cs'
$kitFonte = Join-Path $raizProjeto 'desenvolvimento\src\KitArmasMikeMenu.cs'
$testesMundo = Join-Path $raizProjeto 'desenvolvimento\tests\TestesMundoExtra.cs'
$testesKit = Join-Path $raizProjeto 'desenvolvimento\tests\TestesKitArmas.cs'
$testesMapa = Join-Path $raizProjeto 'desenvolvimento\tests\TestesMapaPrevia.cs'
$testesAtalhos = Join-Path $raizProjeto 'desenvolvimento\tests\TestesAtalhos.cs'
$testes = Join-Path $raizProjeto 'desenvolvimento\tests\TestesRegras.cs'
$testesFavoritos = Join-Path $raizProjeto 'desenvolvimento\tests\TestesFavoritos.cs'
$referencia = (Resolve-Path -LiteralPath $ScriptHookVDotNet3).Path
$compilador = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilador -PathType Leaf)) { throw 'Compilador .NET Framework não encontrado.' }
$pastaCompilacao = Join-Path $raizProjeto '.build'
$pastaDistribuicao = Join-Path $raizProjeto 'scripts'
New-Item -ItemType Directory -Force $pastaCompilacao, $pastaDistribuicao | Out-Null
$executavelTeste = Join-Path $pastaCompilacao 'TestesRegras.exe'
$binario = Join-Path $pastaDistribuicao 'MikeMenu.3.dll'
$pacote = Join-Path $raizProjeto 'MikeMenu.zip'
& $compilador /nologo /define:TEST "/out:$executavelTeste" $codigoFonte $previaFonte $testes
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes.' }
& $executavelTeste
if ($LASTEXITCODE -ne 0) { throw 'Os testes falharam.' }
$executavelFavoritos = Join-Path $pastaCompilacao 'TestesFavoritos.exe'
& $compilador /nologo /define:TEST "/out:$executavelFavoritos" $favoritosFonte $testesFavoritos
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes de favoritos.' }
& $executavelFavoritos
if ($LASTEXITCODE -ne 0) { throw 'Os testes de favoritos falharam.' }
$executavelAtalhos = Join-Path $pastaCompilacao 'TestesAtalhos.exe'
& $compilador /nologo /reference:System.Windows.Forms.dll "/out:$executavelAtalhos" $atalhosFonte $testesAtalhos
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes de atalhos.' }
& $executavelAtalhos
if ($LASTEXITCODE -ne 0) { throw 'Os testes de atalhos falharam.' }
$executavelMapa = Join-Path $pastaCompilacao 'TestesMapaPrevia.exe'
& $compilador /nologo "/out:$executavelMapa" $mapaFonte $testesMapa
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes do mapa de imagens.' }
& $executavelMapa
if ($LASTEXITCODE -ne 0) { throw 'Os testes do mapa de imagens falharam.' }
$executavelMundo = Join-Path $pastaCompilacao 'TestesMundoExtra.exe'
& $compilador /nologo /define:TEST "/out:$executavelMundo" $mundoExtraFonte $testesMundo
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes de mundo.' }
& $executavelMundo
if ($LASTEXITCODE -ne 0) { throw 'Os testes de mundo falharam.' }
$executavelKit = Join-Path $pastaCompilacao 'TestesKitArmas.exe'
& $compilador /nologo /define:TEST "/out:$executavelKit" $favoritosFonte $kitFonte $testesKit
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes de kits.' }
& $executavelKit
if ($LASTEXITCODE -ne 0) { throw 'Os testes de kits falharam.' }
& $compilador /nologo /target:library "/out:$binario" "/reference:$referencia" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $codigoFonte $previaFonte $favoritosFonte $companheirosFonte $extrasFonte $atalhosFonte $mapaFonte $mundoExtraFonte $kitFonte
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o menu.' }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$arquivoTemporario = Join-Path $pastaCompilacao 'MikeMenu.zip'
if (Test-Path -LiteralPath $arquivoTemporario) { Remove-Item -LiteralPath $arquivoTemporario -Force }
$arquivoZip = [System.IO.Compression.ZipFile]::Open($arquivoTemporario, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($arquivoZip, $binario, 'scripts/MikeMenu.3.dll')
    Get-ChildItem -LiteralPath (Join-Path $pastaDistribuicao 'MikeMenuAssets') -File | ForEach-Object {
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($arquivoZip, $_.FullName, ('scripts/MikeMenuAssets/' + $_.Name))
    }
    [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($arquivoZip, (Join-Path $raizProjeto 'desenvolvimento\LEIA-ME.txt'), 'LEIA-ME.txt')
} finally { $arquivoZip.Dispose() }
Copy-Item -LiteralPath $arquivoTemporario -Destination $pacote -Force
Write-Host "Pacote criado: $pacote"
