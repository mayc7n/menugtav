using System;
using System.IO;
using System.Windows.Forms;
class TestesAtalhos
{
    static void Verificar(bool v, string m) { if (!v) throw new Exception(m); }
    static void Main()
    {
        Keys chave;
        Verificar(AtalhosMikeMenu.TentarInterpretar("Ctrl+F5", out chave) && chave == (Keys.Control | Keys.F5), "interpreta padrao");
        Verificar(AtalhosMikeMenu.TentarInterpretar("Shift+Alt+F2", out chave) && chave == (Keys.Shift | Keys.Alt | Keys.F2), "multiplos modificadores");
        Verificar(!AtalhosMikeMenu.TentarInterpretar("F5", out chave), "modificador obrigatorio");
        Verificar(!AtalhosMikeMenu.TentarInterpretar("Ctrl+F10", out chave), "reserva atalho de dinheiro");
        Verificar(!AtalhosMikeMenu.TentarInterpretar("Ctrl+Delete", out chave), "reserva menu");
        string dir = Path.Combine(Path.GetTempPath(), "mike-atalhos-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try {
            string aviso; var a = AtalhosMikeMenu.CarregarDeDiretorio(dir, out aviso);
            Verificar(aviso == null && a.Identificar(Keys.Control | Keys.F5) == 0, "cria configuracao padrao");
            string caminho = Path.Combine(dir, "atalhos.ini");
            File.WriteAllText(caminho, "Vida=Alt+F1\nReparar=Ctrl+F6\nPolicia=Ctrl+F7\n");
            a = AtalhosMikeMenu.CarregarDeDiretorio(dir, out aviso);
            Verificar(aviso == null && a.Identificar(Keys.Alt | Keys.F1) == 0 && a.Identificar(Keys.Control | Keys.F5) == -1, "carrega atalho personalizado");
            string invalido = "Vida=Ctrl+F6\nReparar=Ctrl+F6\nPolicia=Ctrl+F7\n";
            File.WriteAllText(caminho, invalido);
            a = AtalhosMikeMenu.CarregarDeDiretorio(dir, out aviso);
            Verificar(aviso != null && a.Identificar(Keys.Control | Keys.F5) == 0 && File.ReadAllText(caminho) == invalido, "duplicados preservam arquivo e usam padrao");
            Console.WriteLine("Testes de atalhos passaram.");
        } finally { Directory.Delete(dir, true); }
    }
}
