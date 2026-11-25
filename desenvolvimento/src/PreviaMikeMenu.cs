using System;
#if !TEST
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using GTA.UI;
#endif

public static class PreviaMikeMenu
{
    // The caller supplies an asset key, never a filesystem path.
    public static bool NomeSeguro(string nome)
    {
        if (string.IsNullOrEmpty(nome) || nome.Length > 80) return false;
        foreach (char c in nome)
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-')) return false;
        return true;
    }

#if !TEST
    private static readonly Dictionary<string, CustomSprite> imagens = new Dictionary<string, CustomSprite>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> indisponiveis = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // Coordinates use the same 1280 x 720 base as GTA.UI text and containers.
    // Returns false if the image cannot be loaded, so the caller can show a fallback.
    public static bool Desenhar(string nome, float x, float y, float largura, float altura)
    {
        if (!NomeSeguro(nome) || largura <= 0f || altura <= 0f || indisponiveis.Contains(nome)) return false;
        CustomSprite imagem;
        if (!imagens.TryGetValue(nome, out imagem))
        {
            string caminho = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "MikeMenuAssets", nome + ".png");
            if (!File.Exists(caminho)) { indisponiveis.Add(nome); return false; }
            try
            {
                imagem = new CustomSprite(caminho, new SizeF(largura, altura), new PointF(x, y), Color.White);
                imagens.Add(nome, imagem);
            }
            catch (Exception) { indisponiveis.Add(nome); return false; }
        }
        try
        {
            imagem.Position = new PointF(x, y);
            imagem.Size = new SizeF(largura, altura);
            imagem.Draw();
            return true;
        }
        catch (Exception) { indisponiveis.Add(nome); return false; }
    }
#endif
}
