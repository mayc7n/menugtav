using System;
using System.IO;
class TestesKitArmas
{
    static void Verificar(bool condicao, string mensagem) { if (!condicao) throw new Exception(mensagem); }
    static void Main()
    {
        var kit = new KitArmasSalvoMike { Versao = 1, Personagem = unchecked((int)0x9B22DBAF), Armas = new int[] { 42, -123 }, Municoes = new int[] { 50, 0 } };
        Verificar(KitArmasMikeMenu.Valido(kit), "kit valido");
        string caminho = Path.Combine(Path.GetTempPath(), "mike-kit-" + Guid.NewGuid().ToString("N") + ".xml");
        try { FavoritosMikeMenu.Gravar(caminho, kit); var lido = FavoritosMikeMenu.Ler<KitArmasSalvoMike>(caminho); Verificar(KitArmasMikeMenu.Valido(lido) && lido.Armas[1] == -123 && lido.Municoes[0] == 50, "serializacao"); }
        finally { if (File.Exists(caminho)) File.Delete(caminho); }
        kit.Armas = new int[] { 42, 42 }; Verificar(!KitArmasMikeMenu.Valido(kit), "duplicados");
        kit.Armas = new int[] { 42 }; Verificar(!KitArmasMikeMenu.Valido(kit), "matrizes incompletas");
        kit.Municoes = new int[] { -1 }; Verificar(!KitArmasMikeMenu.Valido(kit), "municao negativa");
        kit.Municoes[0] = 1; kit.Personagem = 1; Verificar(!KitArmasMikeMenu.Valido(kit), "personagem desconhecido");
        Console.WriteLine("Testes de kit de armas passaram.");
    }
}
