using System;
using System.IO;
using System.Collections.Generic;
#if !TEST
using GTA;
using GTA.Native;
#endif

[Serializable]
public sealed class KitArmasSalvoMike
{
    public int Versao;
    public int Personagem;
    public int[] Armas;
    public int[] Municoes;
}

public static class KitArmasMikeMenu
{
    private static bool PersonagemValido(int personagem)
    {
        uint id = unchecked((uint)personagem);
        return id == 0x0D7114C9u || id == 0x9B22DBAFu || id == 0x9B810FA2u;
    }

    public static bool Valido(KitArmasSalvoMike kit)
    {
        if (kit == null || kit.Versao != 1 || !PersonagemValido(kit.Personagem) || kit.Armas == null || kit.Municoes == null || kit.Armas.Length != kit.Municoes.Length || kit.Armas.Length > 512) return false;
        var vistos = new HashSet<int>();
        for (int i = 0; i < kit.Armas.Length; i++)
            if (kit.Armas[i] == 0 || !vistos.Add(kit.Armas[i]) || kit.Municoes[i] < 0 || kit.Municoes[i] > 1000000) return false;
        return true;
    }

    private static string Caminho(int personagem, int slot)
    {
        if (!PersonagemValido(personagem) || !FavoritosMikeMenu.SlotValido(slot)) throw new ArgumentOutOfRangeException();
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "MikeMenu", "armas-" + unchecked((uint)personagem).ToString("X8") + "-" + slot + ".xml");
    }

#if !TEST
    private static bool Pronto(Ped ped)
    {
        if (ped == null || !ped.Exists() || ped.IsDead || !PersonagemValido(ped.Model.Hash)) return false;
        if (Function.Call<bool>(Hash.NETWORK_IS_SESSION_ACTIVE) || Function.Call<bool>(Hash.NETWORK_IS_GAME_IN_PROGRESS) || Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT) || Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS) || !Function.Call<bool>(Hash.IS_PLAYER_PLAYING, Game.Player.Handle)) return false;
        Ped atual = Game.Player.Character;
        return atual != null && atual.Exists() && atual.Handle == ped.Handle;
    }

    public static bool Salvar(Ped ped, int slot, out string mensagem)
    {
        mensagem = "Nao foi possivel salvar o kit.";
        if (!FavoritosMikeMenu.SlotValido(slot) || !Pronto(ped)) return false;
        try
        {
            var armas = new List<int>(); var municoes = new List<int>(); var vistos = new HashSet<int>();
            foreach (WeaponHash arma in Enum.GetValues(typeof(WeaponHash)))
            {
                int hash = unchecked((int)arma);
                if (arma == WeaponHash.Unarmed || !vistos.Add(hash) || !Function.Call<bool>(Hash.IS_WEAPON_VALID, hash) || !Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped.Handle, hash, false)) continue;
                armas.Add(hash); municoes.Add(Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, ped.Handle, hash));
            }
            var kit = new KitArmasSalvoMike { Versao = 1, Personagem = ped.Model.Hash, Armas = armas.ToArray(), Municoes = municoes.ToArray() };
            if (!Valido(kit) || !Pronto(ped)) return false;
            FavoritosMikeMenu.Gravar(Caminho(kit.Personagem, slot), kit);
            mensagem = "Kit salvo no espaco " + slot + ": " + armas.Count + " armas."; return true;
        }
        catch (Exception) { return false; }
    }

    public static bool Aplicar(Ped ped, int slot, out string mensagem)
    {
        mensagem = "Kit ausente, invalido ou indisponivel.";
        if (!FavoritosMikeMenu.SlotValido(slot) || !Pronto(ped)) return false;
        try
        {
            int personagem = ped.Model.Hash;
            var kit = FavoritosMikeMenu.Ler<KitArmasSalvoMike>(Caminho(personagem, slot));
            if (!Valido(kit) || kit.Personagem != personagem || !Pronto(ped)) return false;
            int aplicadas = 0, ignoradas = 0;
            for (int i = 0; i < kit.Armas.Length; i++)
            {
                int hash = kit.Armas[i];
                if (!Function.Call<bool>(Hash.IS_WEAPON_VALID, hash)) { ignoradas++; continue; }
                Function.Call(Hash.GIVE_WEAPON_TO_PED, ped.Handle, hash, kit.Municoes[i], false, false);
                if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped.Handle, hash, false)) { Function.Call(Hash.SET_PED_AMMO, ped.Handle, hash, kit.Municoes[i]); aplicadas++; } else ignoradas++;
            }
            mensagem = "Kit aplicado: " + aplicadas + " armas" + (ignoradas > 0 ? "; " + ignoradas + " indisponiveis." : "."); return true;
        }
        catch (Exception) { return false; }
    }
#endif
}
