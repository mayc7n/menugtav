using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

// Atalhos de ações, independentes da tecla Del e dos atalhos de dinheiro.
public sealed class AtalhosMikeMenu
{
    private static readonly string[] Nomes = { "Vida", "Reparar", "Policia" };
    private static readonly Keys[] Padrao = { Keys.Control | Keys.F5, Keys.Control | Keys.F6, Keys.Control | Keys.F7 };
    private readonly Keys[] combinacoes;
    private readonly string arquivo;

    private AtalhosMikeMenu(string caminho, Keys[] valores) { arquivo = caminho; combinacoes = (Keys[])valores.Clone(); }

    // Somente teclas de funcao com modificador; F10 e F11 pertencem ao dinheiro.
    public static bool TentarInterpretar(string texto, out Keys valor)
    {
        valor = Keys.None;
        if (string.IsNullOrWhiteSpace(texto) || texto.Length > 60) return false;
        Keys mods = Keys.None, tecla = Keys.None;
        string[] partes = texto.Split('+');
        foreach (string parte in partes)
        {
            string termo = parte.Trim().ToUpperInvariant();
            Keys mod = termo == "CTRL" || termo == "CONTROL" ? Keys.Control : termo == "SHIFT" ? Keys.Shift : termo == "ALT" ? Keys.Alt : Keys.None;
            if (mod != Keys.None) { if ((mods & mod) != 0) return false; mods |= mod; continue; }
            if (tecla != Keys.None || termo.Length < 2 || termo[0] != 'F') return false;
            int numero;
            if (!int.TryParse(termo.Substring(1), out numero) || numero < 1 || numero > 12 || numero == 10 || numero == 11) return false;
            tecla = (Keys)((int)Keys.F1 + numero - 1);
        }
        if (mods == Keys.None || tecla == Keys.None) return false;
        valor = mods | tecla;
        return true;
    }

    private static string Formatar(Keys valor)
    {
        string texto = "";
        if ((valor & Keys.Control) != 0) texto += "Ctrl+";
        if ((valor & Keys.Shift) != 0) texto += "Shift+";
        if ((valor & Keys.Alt) != 0) texto += "Alt+";
        return texto + "F" + ((int)(valor & Keys.KeyCode) - (int)Keys.F1 + 1);
    }

    private static string Serializar(Keys[] valores)
    {
        var texto = new StringBuilder("# Edite os atalhos e reinicie o jogo. Use Ctrl, Shift ou Alt com F1-F9 ou F12.\r\n");
        for (int i = 0; i < Nomes.Length; i++) texto.Append(Nomes[i]).Append('=').Append(Formatar(valores[i])).Append("\r\n");
        return texto.ToString();
    }

    public static AtalhosMikeMenu Carregar(out string aviso)
    {
        return CarregarDeDiretorio(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "MikeMenu"), out aviso);
    }

    public static AtalhosMikeMenu CarregarDeDiretorio(string diretorio, out string aviso)
    {
        aviso = null;
        string caminho = Path.Combine(diretorio, "atalhos.ini");
        try
        {
            Directory.CreateDirectory(diretorio);
            if (!File.Exists(caminho))
            {
                using (var arquivoNovo = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var escritor = new StreamWriter(arquivoNovo, new UTF8Encoding(false))) escritor.Write(Serializar(Padrao));
            }
            if (new FileInfo(caminho).Length > 4096) throw new InvalidDataException();
            string[] linhas = File.ReadAllLines(caminho);
            Keys[] valores = new Keys[3]; bool[] vistos = new bool[3];
            foreach (string linhaOriginal in linhas)
            {
                string linha = linhaOriginal.Trim();
                if (linha.Length == 0 || linha.StartsWith("#") || linha.StartsWith(";")) continue;
                int separador = linha.IndexOf('=');
                if (separador <= 0) throw new InvalidDataException();
                string nome = linha.Substring(0, separador).Trim();
                int indice = Array.FindIndex(Nomes, delegate(string n) { return string.Equals(n, nome, StringComparison.OrdinalIgnoreCase); });
                Keys valor;
                if (indice < 0 || vistos[indice] || !TentarInterpretar(linha.Substring(separador + 1), out valor)) throw new InvalidDataException();
                vistos[indice] = true; valores[indice] = valor;
            }
            if (!vistos[0] || !vistos[1] || !vistos[2] || valores[0] == valores[1] || valores[0] == valores[2] || valores[1] == valores[2]) throw new InvalidDataException();
            return new AtalhosMikeMenu(caminho, valores);
        }
        catch (Exception e)
        {
            if (!(e is IOException) && !(e is UnauthorizedAccessException) && !(e is ArgumentException) && !(e is InvalidDataException) && !(e is System.Security.SecurityException)) throw;
            aviso = "Atalhos invalidos ou inacessiveis; padrao ativado. Confira scripts/MikeMenu/atalhos.ini.";
            return new AtalhosMikeMenu(caminho, Padrao);
        }
    }

    // Retorna 0 para vida, 1 para reparo, 2 para policia; -1 para nenhuma ação.
    public int Identificar(Keys dados)
    {
        Keys valor = dados & (Keys.KeyCode | Keys.Modifiers);
        for (int i = 0; i < combinacoes.Length; i++) if (valor == combinacoes[i]) return i;
        return -1;
    }

    public string Descricao(int acao) { return acao >= 0 && acao < combinacoes.Length ? Formatar(combinacoes[acao]) : ""; }
}
