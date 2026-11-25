using System;
using System.Globalization;

// Chaves sao identificadores de arquivos PNG opcionais, nao caminhos.
public static class MapaPreviaMikeMenu
{
    public static string ChaveVeiculo(int modelo)
    {
        return modelo == 0 ? null : "veiculo_" + unchecked((uint)modelo).ToString("X8", CultureInfo.InvariantCulture);
    }

    public static string ChaveRoupa(int personagem, int componente, int modelo, int textura)
    {
        uint id = unchecked((uint)personagem);
        if ((id != 0x0D7114C9u && id != 0x9B22DBAFu && id != 0x9B810FA2u) || componente < 0 || componente > 11 || modelo < 0 || textura < 0) return null;
        return "roupa_" + unchecked((uint)personagem).ToString("X8", CultureInfo.InvariantCulture) + "_" +
            componente.ToString(CultureInfo.InvariantCulture) + "_" + modelo.ToString(CultureInfo.InvariantCulture) + "_" + textura.ToString(CultureInfo.InvariantCulture);
    }

    // Correspondencias ja utilizadas no menu; os recursos sao lidos do jogo.
    public static bool TentarPreviaNativa(string modelo, out string dicionario, out string textura)
    {
        dicionario = null; textura = null;
        if (string.IsNullOrEmpty(modelo)) return false;
        switch (modelo.ToLowerInvariant())
        {
            case "adder": case "cheetah": case "banshee": dicionario = "lgm_default"; break;
            case "zentorno": dicionario = "lgm_dlc_business2"; break;
            case "bmx": dicionario = "pandm_default"; break;
            default: return false;
        }
        textura = modelo.ToLowerInvariant();
        return true;
    }
}
