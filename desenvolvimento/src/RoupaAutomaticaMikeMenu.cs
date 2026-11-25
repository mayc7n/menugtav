using System;
using System.Collections.Generic;
using System.IO;
#if !TEST
using GTA;
using GTA.Native;
#endif

// A morte arma apenas o protagonista observado; uma troca normal nao aplica roupa.
public sealed class EstadoRoupaAutomatica
{
    private readonly HashSet<int> pendentes = new HashSet<int>();
    private int aguardando, inicio;
    public static bool Protagonista(int modelo)
    {
        uint id = unchecked((uint)modelo);
        return id == 0x0D7114C9u || id == 0x9B22DBAFu || id == 0x9B810FA2u;
    }
    public void Limpar() { pendentes.Clear(); aguardando = 0; inicio = 0; }
    public bool Observar(int modelo, bool morto, bool pronto, int tempo)
    {
        if (!Protagonista(modelo)) { aguardando = 0; return false; }
        if (morto) { pendentes.Add(modelo); aguardando = 0; return false; }
        if (!pendentes.Contains(modelo) || !pronto) { aguardando = 0; return false; }
        if (aguardando != modelo) { aguardando = modelo; inicio = tempo; return false; }
        if (unchecked(tempo - inicio) < 3000) return false;
        pendentes.Remove(modelo); aguardando = 0; return true;
    }
}

#if !TEST
public sealed class RoupaAutomaticaMikeMenu
{
    private readonly EstadoRoupaAutomatica estado = new EstadoRoupaAutomatica();
    private static string Caminho(int modelo)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "MikeMenu", "roupa-auto-" + unchecked((uint)modelo).ToString("X8") + ".xml");
    }
    private static bool Seguro(Ped ped)
    {
        if (ped == null || !ped.Exists() || ped.IsDead || !EstadoRoupaAutomatica.Protagonista(ped.Model.Hash)) return false;
        if (Function.Call<bool>(Hash.NETWORK_IS_SESSION_ACTIVE) || Function.Call<bool>(Hash.NETWORK_IS_GAME_IN_PROGRESS) || Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT) || Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS) || !Function.Call<bool>(Hash.IS_PLAYER_PLAYING, Game.Player.Handle)) return false;
        Ped atual = Game.Player.Character;
        return atual != null && atual.Exists() && atual.Handle == ped.Handle;
    }

    // Chamar apenas depois de uma escolha de roupa ou de aplicar um traje manual.
    public bool Registrar(Ped ped, out string mensagem)
    {
        mensagem = "Nao foi possivel guardar a roupa para depois da morte.";
        if (!Seguro(ped)) return false;
        try
        {
            int h = ped.Handle;
            var d = new TrajeSalvoMike { Versao = 1, Modelo = ped.Model.Hash, Pecas = new int[12], Texturas = new int[12], Paletas = new int[12], Aderecos = new int[8], TexturasAderecos = new int[8] };
            for (int i = 0; i < 12; i++) { d.Pecas[i] = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, h, i); d.Texturas[i] = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, h, i); d.Paletas[i] = Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, h, i); }
            for (int i = 0; i < 8; i++) { d.Aderecos[i] = Function.Call<int>(Hash.GET_PED_PROP_INDEX, h, i); d.TexturasAderecos[i] = d.Aderecos[i] < 0 ? 0 : Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, h, i); }
            if (!FavoritosMikeMenu.TrajeValido(d) || !Seguro(ped)) return false;
            FavoritosMikeMenu.Gravar(Caminho(d.Modelo), d);
            mensagem = "Roupa guardada para depois da morte."; return true;
        }
        catch (Exception) { return false; }
    }
    private static bool Aplicar(Ped ped, TrajeSalvoMike d)
    {
        if (!Seguro(ped) || !FavoritosMikeMenu.TrajeValido(d) || d.Modelo != ped.Model.Hash) return false;
        int h = ped.Handle;
        for (int i = 0; i < 12; i++)
        {
            if (Function.Call<bool>(Hash.IS_PED_COMPONENT_VARIATION_VALID, h, i, d.Pecas[i], d.Texturas[i])) continue;
            if (Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, h, i) != d.Pecas[i] || Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, h, i) != d.Texturas[i]) return false;
        }
        for (int i = 0; i < 8; i++) if (d.Aderecos[i] >= 0 && (d.Aderecos[i] >= Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS, h, i) || d.TexturasAderecos[i] >= Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS, h, i, d.Aderecos[i]))) return false;
        if (!Seguro(ped)) return false;
        for (int i = 0; i < 12; i++) Function.Call(Hash.SET_PED_COMPONENT_VARIATION, h, i, d.Pecas[i], d.Texturas[i], d.Paletas[i]);
        for (int i = 0; i < 8; i++) if (d.Aderecos[i] < 0) Function.Call(Hash.CLEAR_PED_PROP, h, i); else Function.Call(Hash.SET_PED_PROP_INDEX, h, i, d.Aderecos[i], d.TexturasAderecos[i], true);
        return true;
    }
    // Chamar em todo Tick, antes dos retornos de carregamento/troca do menu.
    // Retorna mensagem apenas quando uma tentativa efetiva ocorre.
    public string Atualizar()
    {
        bool tentandoRestaurar = false;
        try
        {
            if (Function.Call<bool>(Hash.NETWORK_IS_SESSION_ACTIVE) || Function.Call<bool>(Hash.NETWORK_IS_GAME_IN_PROGRESS)) { estado.Limpar(); return null; }
            Ped ped = Game.Player.Character;
            if (ped == null || !ped.Exists()) { estado.Observar(0, false, false, Game.GameTime); return null; }
            int modelo = ped.Model.Hash;
            if (!estado.Observar(modelo, ped.IsDead, Seguro(ped), Game.GameTime)) return null;
            string caminho = Caminho(modelo);
            if (!File.Exists(caminho)) return null;
            tentandoRestaurar = true;
            return Aplicar(ped, FavoritosMikeMenu.Ler<TrajeSalvoMike>(caminho)) ? "Roupa restaurada apos a morte." : "Nao foi possivel restaurar a roupa salva.";
        }
        catch (Exception) { return tentandoRestaurar ? "Nao foi possivel restaurar a roupa salva." : null; }
    }
}
#endif
