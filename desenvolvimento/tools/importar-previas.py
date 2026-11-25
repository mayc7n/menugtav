#!/usr/bin/env python3
"""Importa prévias de terceiros para uso local; não redistribui as imagens.

Requer Python 3 e Pillow com suporte a WebP. Exemplo:
  python desenvolvimento/tools/importar-previas.py --destino .build/preview-import
As imagens vêm de Rendererrr/gtaDiscoveryApi, não do Mike Menu. O catálogo pode
estar incompleto ou indisponível. Arquivos existentes nunca são substituídos.
"""
import argparse
import concurrent.futures
import io
import json
import os
from pathlib import Path
import re
import sys
import urllib.parse
import urllib.request
import warnings

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow é necessário: python -m pip install Pillow")

INDEX = "https://cdn.jsdelivr.net/gh/Rendererrr/gtaDiscoveryApi@main/api/vehicles/index.json"
PREFIX = "https://cdn.jsdelivr.net/gh/Rendererrr/gtaDiscoveryApi@main/assets/vehicles/images/"
MAX_INDEX = 8 * 1024 * 1024
MAX_IMAGE = 4 * 1024 * 1024
MAX_PIXELS = 20_000_000
Image.MAX_IMAGE_PIXELS = MAX_PIXELS


def fetch(url, limit):
    request = urllib.request.Request(url, headers={"User-Agent": "MikeMenu-preview-import/1.0"})
    with urllib.request.urlopen(request, timeout=20) as response:
        final = urllib.parse.urlparse(response.geturl())
        if final.scheme != "https" or final.hostname != "cdn.jsdelivr.net":
            raise ValueError("Origem de resposta inesperada")
        length = response.headers.get("Content-Length")
        if length and int(length) > limit:
            raise ValueError("Arquivo excede limite")
        data = response.read(limit + 1)
        if len(data) > limit:
            raise ValueError("Arquivo excede limite")
        return data


def joaat(text):
    value = 0
    for byte in text.lower().encode("ascii"):
        value = (value + byte) & 0xFFFFFFFF
        value = (value + (value << 10)) & 0xFFFFFFFF
        value ^= value >> 6
    value = (value + (value << 3)) & 0xFFFFFFFF
    value ^= value >> 11
    return (value + (value << 15)) & 0xFFFFFFFF


def importar(item, destino):
    ident = item.get("id")
    hash_valor = item.get("hash")
    if not isinstance(ident, str) or not re.fullmatch(r"[a-z0-9_]{1,64}", ident) or not isinstance(hash_valor, int) or isinstance(hash_valor, bool) or not (0 < hash_valor <= 0xFFFFFFFF) or joaat(ident) != hash_valor:
        return "inválido", str(ident)
    caminho = destino / ("veiculo_%08X.png" % hash_valor)
    if caminho.exists():
        return "existente", ident
    try:
        data = fetch(PREFIX + ident + ".webp", MAX_IMAGE)
        with warnings.catch_warnings():
            warnings.simplefilter("error", Image.DecompressionBombWarning)
            with Image.open(io.BytesIO(data)) as imagem:
                if imagem.format != "WEBP" or imagem.width <= 0 or imagem.height <= 0 or imagem.width * imagem.height > MAX_PIXELS:
                    raise ValueError("Imagem inválida")
                imagem.load()
                imagem.thumbnail((256, 128), Image.Resampling.LANCZOS)
                saida = io.BytesIO()
                imagem.convert("RGBA").save(saida, format="PNG", optimize=True)
        # Criação exclusiva garante preservação de imagem existente, mesmo em execução concorrente.
        try:
            with open(caminho, "xb") as arquivo:
                arquivo.write(saida.getvalue())
        except FileExistsError:
            return "existente", ident
        return "criado", ident
    except Exception as exc:
        return "erro", ident + ": " + str(exc)[:160]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--destino", type=Path, required=True, help="diretório local para os PNGs")
    parser.add_argument("--limite", type=int, default=0, help="máximo de modelos; 0 = catálogo completo")
    parser.add_argument("--paralelo", type=int, default=6, choices=range(1, 9), help="downloads simultâneos (1–8)")
    args = parser.parse_args()
    if args.limite < 0:
        parser.error("--limite deve ser >= 0")
    args.destino.mkdir(parents=True, exist_ok=True)
    dados = json.loads(fetch(INDEX, MAX_INDEX))
    modelos = dados.get("items")
    if not isinstance(modelos, list) or len(modelos) > 3000:
        sys.exit("Catálogo inválido")
    if args.limite:
        modelos = modelos[:args.limite]
    resumo = {"criado": 0, "existente": 0, "inválido": 0, "erro": 0}
    with concurrent.futures.ThreadPoolExecutor(max_workers=args.paralelo) as pool:
        for estado, detalhe in pool.map(lambda x: importar(x, args.destino), modelos):
            resumo[estado] += 1
            if estado in ("erro", "inválido"):
                print(estado + ": " + detalhe, file=sys.stderr)
    print("Catálogo: %d; " % len(modelos) + "; ".join("%s: %d" % p for p in resumo.items()))
    if resumo["erro"] or resumo["inválido"]:
        sys.exit(1)


if __name__ == "__main__":
    main()
