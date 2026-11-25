using System;
using System.IO;
class TestesFavoritos
{
    static void Verificar(bool ok, string caso) { if (!ok) throw new Exception(caso); }
    static void Main()
    {
        string pasta = Path.Combine(Path.GetTempPath(), "mike-teste-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        try
        {
            Verificar(!FavoritosMikeMenu.SlotValido(0) && FavoritosMikeMenu.SlotValido(1) && FavoritosMikeMenu.SlotValido(3) && !FavoritosMikeMenu.SlotValido(4), "limites de slots");
            var t = new TrajeSalvoMike { Versao = 1, Modelo = 123, Pecas = new int[12], Texturas = new int[12], Paletas = new int[12], Aderecos = new int[8], TexturasAderecos = new int[8] };
            t.Pecas[3] = 7; t.Aderecos[0] = -1;
            string arquivo = Path.Combine(pasta, "traje.xml");
            FavoritosMikeMenu.Gravar(arquivo, t);
            var lido = FavoritosMikeMenu.Ler<TrajeSalvoMike>(arquivo);
            Verificar(FavoritosMikeMenu.TrajeValido(lido) && lido.Pecas[3] == 7 && lido.Aderecos[0] == -1, "traje salvo e restaurado");
            t.Pecas[3] = 8; FavoritosMikeMenu.Gravar(arquivo, t);
            Verificar(FavoritosMikeMenu.Ler<TrajeSalvoMike>(arquivo).Pecas[3] == 8, "substituicao atomica");
            var v = new VeiculoSalvoMike { Versao = 1, Modelo = -123, Placa = "MIKE", Mods = new int[50], Variacoes = new bool[50] };
            v.Mods[1] = -1; v.Turbo = true;
            FavoritosMikeMenu.Gravar(Path.Combine(pasta, "veiculo.xml"), v);
            var veiculo = FavoritosMikeMenu.Ler<VeiculoSalvoMike>(Path.Combine(pasta, "veiculo.xml"));
            Verificar(FavoritosMikeMenu.VeiculoValido(veiculo) && veiculo.Mods[1] == -1 && veiculo.Turbo, "veiculo salvo e restaurado");
            Verificar(!FavoritosMikeMenu.TrajeValido(new TrajeSalvoMike()) && !FavoritosMikeMenu.VeiculoValido(new VeiculoSalvoMike()), "dados incompletos rejeitados");
            bool rejeitou = false; File.WriteAllText(arquivo, "<!DOCTYPE foo [<!ENTITY x SYSTEM 'file:///test'>]><TrajeSalvoMike>&x;</TrajeSalvoMike>");
            try { FavoritosMikeMenu.Ler<TrajeSalvoMike>(arquivo); } catch (InvalidOperationException) { rejeitou = true; } catch (System.Xml.XmlException) { rejeitou = true; }
            Verificar(rejeitou, "DTD rejeitado");
            Console.WriteLine("Testes de favoritos passaram.");
        }
        finally { Directory.Delete(pasta, true); }
    }
}
