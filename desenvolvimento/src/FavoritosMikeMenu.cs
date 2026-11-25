using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
#if !TEST
using GTA;
using GTA.Native;
#endif

[Serializable]
public sealed class TrajeSalvoMike
{
    public int Versao;
    public int Modelo;
    public int[] Pecas;
    public int[] Texturas;
    public int[] Paletas;
    public int[] Aderecos;
    public int[] TexturasAderecos;
}

[Serializable]
public sealed class VeiculoSalvoMike
{
    public int Versao;
    public int Modelo;
    public int CorPrimaria;
    public int CorSecundaria;
    public int TipoRoda;
    public int[] Mods;
    public bool[] Variacoes;
    public bool Turbo;
    public string Placa;
}

public static class FavoritosMikeMenu
{
    private const int MaxArquivo = 65536;
    public static bool SlotValido(int slot) { return slot >= 1 && slot <= 3; }
    private static string Caminho(string tipo, int slot)
    {
        if (!SlotValido(slot) || (tipo != "traje" && tipo != "veiculo")) throw new ArgumentOutOfRangeException("slot");
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "MikeMenu", tipo + "-" + slot + ".xml");
    }
    public static void Gravar<T>(string destino, T dados)
    {
        string dir = Path.GetDirectoryName(destino);
        Directory.CreateDirectory(dir);
        string temp = Path.Combine(dir, ".mike-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var arquivo = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                new XmlSerializer(typeof(T)).Serialize(arquivo, dados);
            if (File.Exists(destino)) File.Replace(temp, destino, null);
            else File.Move(temp, destino);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public static T Ler<T>(string caminho)
    {
        var info = new FileInfo(caminho);
        if (!info.Exists || info.Length > MaxArquivo) throw new InvalidDataException("Arquivo ausente ou grande demais.");
        var opcoes = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxArquivo };
        using (var arquivo = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var leitor = XmlReader.Create(arquivo, opcoes)) return (T)new XmlSerializer(typeof(T)).Deserialize(leitor);
    }
    public static bool TrajeValido(TrajeSalvoMike d)
    {
        if (d == null || d.Versao != 1 || d.Modelo == 0 || d.Pecas == null || d.Pecas.Length != 12 || d.Texturas == null || d.Texturas.Length != 12 || d.Paletas == null || d.Paletas.Length != 12 || d.Aderecos == null || d.Aderecos.Length != 8 || d.TexturasAderecos == null || d.TexturasAderecos.Length != 8) return false;
        for (int i = 0; i < 12; i++) if (d.Pecas[i] < 0 || d.Texturas[i] < 0 || d.Paletas[i] < 0 || d.Paletas[i] > 3) return false;
        for (int i = 0; i < 8; i++) if (d.Aderecos[i] < -1 || d.TexturasAderecos[i] < 0) return false;
        return true;
    }
    public static bool VeiculoValido(VeiculoSalvoMike d)
    {
        if (d == null || d.Versao != 1 || d.Modelo == 0 || d.Mods == null || d.Mods.Length != 50 || d.Variacoes == null || d.Variacoes.Length != 50 || d.Placa == null || d.Placa.Length > 8 || d.TipoRoda < 0 || d.TipoRoda > 20 || d.CorPrimaria < 0 || d.CorSecundaria < 0) return false;
        for (int i = 0; i < 50; i++) if (d.Mods[i] < -1 || d.Mods[i] > 1000) return false;
        return true;
    }
#if !TEST
    private static bool JogadorSeguro(Ped ped)
    {
        if (ped == null || !ped.Exists() || Game.Player.Character == null || !Game.Player.Character.Exists() || ped.Handle != Game.Player.Character.Handle) return false;
        if (Function.Call<bool>(Hash.NETWORK_IS_SESSION_ACTIVE) || Function.Call<bool>(Hash.NETWORK_IS_GAME_IN_PROGRESS) || Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT) || Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS) || !Function.Call<bool>(Hash.IS_PLAYER_PLAYING, Game.Player.Handle)) return false;
        int hash = ped.Model.Hash;
        return hash == unchecked((int)PedHash.Michael) || hash == unchecked((int)PedHash.Franklin) || hash == unchecked((int)PedHash.Trevor);
    }
    public static bool SalvarTraje(Ped ped, int slot, out string mensagem)
    {
        mensagem = "Nao foi possivel salvar o traje.";
        if (!SlotValido(slot) || !JogadorSeguro(ped)) return false;
        try
        {
            var d = new TrajeSalvoMike { Versao = 1, Modelo = ped.Model.Hash, Pecas = new int[12], Texturas = new int[12], Paletas = new int[12], Aderecos = new int[8], TexturasAderecos = new int[8] };
            for (int i = 0; i < 12; i++) { d.Pecas[i] = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, ped.Handle, i); d.Texturas[i] = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, ped.Handle, i); d.Paletas[i] = Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, ped.Handle, i); }
            for (int i = 0; i < 8; i++) { d.Aderecos[i] = Function.Call<int>(Hash.GET_PED_PROP_INDEX, ped.Handle, i); d.TexturasAderecos[i] = d.Aderecos[i] < 0 ? 0 : Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, ped.Handle, i); }
            if (!TrajeValido(d)) { mensagem = "Traje atual nao pode ser salvo."; return false; }
            Gravar(Caminho("traje", slot), d); mensagem = "Traje salvo no espaco " + slot + "."; return true;
        }
        catch (Exception) { return false; }
    }
    public static bool AplicarTraje(Ped ped, int slot, out string mensagem)
    {
        mensagem = "Traje ausente, invalido ou indisponivel.";
        if (!SlotValido(slot) || !JogadorSeguro(ped)) return false;
        try
        {
            var d = Ler<TrajeSalvoMike>(Caminho("traje", slot));
            if (!TrajeValido(d) || d.Modelo != ped.Model.Hash) { mensagem = "Traje invalido ou de outro personagem."; return false; }
            for (int i = 0; i < 12; i++) if (!Function.Call<bool>(Hash.IS_PED_COMPONENT_VARIATION_VALID, ped.Handle, i, d.Pecas[i], d.Texturas[i])) return false;
            for (int i = 0; i < 8; i++) if (d.Aderecos[i] >= 0 && (d.Aderecos[i] >= Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS, ped.Handle, i) || d.TexturasAderecos[i] >= Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS, ped.Handle, i, d.Aderecos[i]))) return false;
            if (!JogadorSeguro(ped)) return false;
            for (int i = 0; i < 12; i++) Function.Call(Hash.SET_PED_COMPONENT_VARIATION, ped.Handle, i, d.Pecas[i], d.Texturas[i], d.Paletas[i]);
            for (int i = 0; i < 8; i++) if (d.Aderecos[i] < 0) Function.Call(Hash.CLEAR_PED_PROP, ped.Handle, i); else Function.Call(Hash.SET_PED_PROP_INDEX, ped.Handle, i, d.Aderecos[i], d.TexturasAderecos[i], true);
            mensagem = "Traje do espaco " + slot + " aplicado."; return true;
        }
        catch (Exception) { return false; }
    }
    public static bool SalvarVeiculo(Vehicle v, int slot, out string mensagem)
    {
        mensagem = "Entre em um veiculo para salvar.";
        if (!SlotValido(slot) || !JogadorSeguro(Game.Player.Character) || v == null || !v.Exists() || Game.Player.Character.CurrentVehicle == null || Game.Player.Character.CurrentVehicle.Handle != v.Handle) return false;
        try
        {
            int h = v.Handle; var d = new VeiculoSalvoMike { Versao = 1, Modelo = v.Model.Hash, Mods = new int[50], Variacoes = new bool[50] };
            using (var p = new OutputArgument()) using (var s = new OutputArgument()) { Function.Call(Hash.GET_VEHICLE_COLOURS, h, p, s); d.CorPrimaria = p.GetResult<int>(); d.CorSecundaria = s.GetResult<int>(); }
            d.TipoRoda = Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE, h); d.Placa = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, h) ?? "";
            Function.Call(Hash.SET_VEHICLE_MOD_KIT, h, 0);
            for (int i = 0; i < 50; i++) { d.Mods[i] = Function.Call<int>(Hash.GET_VEHICLE_MOD, h, i); d.Variacoes[i] = (i == 23 || i == 24) && Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION, h, i); }
            d.Turbo = Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, h, 18);
            if (!VeiculoValido(d)) { mensagem = "Veiculo atual nao pode ser salvo."; return false; }
            Gravar(Caminho("veiculo", slot), d); mensagem = "Veiculo salvo no espaco " + slot + "."; return true;
        }
        catch (Exception) { mensagem = "Nao foi possivel salvar o veiculo."; return false; }
    }
    public static bool CriarVeiculo(Ped ped, int slot, out string mensagem)
    {
        mensagem = "Veiculo salvo ausente, invalido ou indisponivel.";
        if (!SlotValido(slot) || !JogadorSeguro(ped)) return false;
        Model modelo = new Model();
        try
        {
            var d = Ler<VeiculoSalvoMike>(Caminho("veiculo", slot));
            if (!VeiculoValido(d)) return false;
            modelo = new Model(d.Modelo);
            if (!modelo.IsInCdImage || !modelo.IsVehicle || !modelo.Request(1500) || !JogadorSeguro(ped)) return false;
            var v = World.CreateVehicle(modelo, ped.GetOffsetPosition(new GTA.Math.Vector3(0f, 7f, 0f)), ped.Heading);
            if (v == null || !v.Exists()) return false;
            int h = v.Handle; Function.Call(Hash.SET_VEHICLE_MOD_KIT, h, 0); Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE, h, d.TipoRoda);
            Function.Call(Hash.SET_VEHICLE_COLOURS, h, d.CorPrimaria, d.CorSecundaria);
            Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, h, d.Placa);
            int ignorados = 0;
            for (int i = 0; i < 50; i++)
            {
                if (i >= 17 && i <= 22) continue;
                int quantidade = Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, h, i);
                if (d.Mods[i] >= quantidade) { if (d.Mods[i] >= 0) ignorados++; continue; }
                if (quantidade > 0 || d.Mods[i] >= 0) Function.Call(Hash.SET_VEHICLE_MOD, h, i, d.Mods[i], d.Variacoes[i]);
            }
            Function.Call(Hash.TOGGLE_VEHICLE_MOD, h, 18, d.Turbo);
            mensagem = ignorados == 0 ? "Veiculo salvo criado perto de voce." : "Veiculo criado; algumas pecas nao estao disponiveis."; return true;
        }
        catch (Exception) { return false; }
        finally { if (modelo.Hash != 0) modelo.MarkAsNoLongerNeeded(); }
    }
#endif
}
