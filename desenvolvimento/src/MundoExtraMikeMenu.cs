using System;
#if !TEST
using GTA;
using GTA.Native;
#endif

// Densidade afeta novas entidades, não remove as já presentes no mapa.
public sealed class MundoExtraMikeMenu
{
    private int transito, pedestres;
    private bool firmeNaMoto;
    private int pedProtegido;
    public int Transito { get { return transito; } }
    public int Pedestres { get { return pedestres; } }
    public bool FirmeNaMoto { get { return firmeNaMoto; } }
    public bool Ativo { get { return transito != 0 || pedestres != 0 || firmeNaMoto || pedProtegido != 0; } }

    public static int ProximoNivel(int nivel) { return nivel == 0 ? 1 : nivel == 1 ? 2 : 0; }
    public static float ValorDensidade(int nivel) { return nivel == 1 ? 0.5f : nivel == 2 ? 0f : 1f; }
    public static string NomeNivel(int nivel) { return nivel == 1 ? "REDUZIDO" : nivel == 2 ? "MINIMO" : "NORMAL"; }
    public void AlternarTransito() { transito = ProximoNivel(transito); }
    public void AlternarPedestres() { pedestres = ProximoNivel(pedestres); }

#if !TEST
    public void AlternarFirmeNaMoto()
    {
        firmeNaMoto = !firmeNaMoto;
        if (!firmeNaMoto) RestaurarPed();
    }

    private void RestaurarPed()
    {
        if (pedProtegido != 0 && Function.Call<bool>(Hash.DOES_ENTITY_EXIST, pedProtegido))
            Function.Call(Hash.SET_PED_CAN_BE_KNOCKED_OFF_VEHICLE, pedProtegido, 0);
        pedProtegido = 0;
    }

    // Chamar apenas com protagonista válido no modo história, em cada Tick.
    public void Atualizar(Ped jogador)
    {
        if (transito != 0)
        {
            float valor = ValorDensidade(transito);
            Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, valor);
            Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, valor);
            Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, valor);
        }
        if (pedestres != 0)
        {
            float valor = ValorDensidade(pedestres);
            Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, valor);
            Function.Call(Hash.SET_SCENARIO_PED_DENSITY_MULTIPLIER_THIS_FRAME, valor, valor);
        }
        bool naMoto = firmeNaMoto && jogador != null && jogador.Exists() && jogador.CurrentVehicle != null && jogador.CurrentVehicle.Exists() && jogador.CurrentVehicle.Model.IsBike;
        int atual = naMoto ? jogador.Handle : 0;
        if (atual != pedProtegido)
        {
            RestaurarPed();
            if (atual != 0) { Function.Call(Hash.SET_PED_CAN_BE_KNOCKED_OFF_VEHICLE, atual, 1); pedProtegido = atual; }
        }
    }

    // Suspende efeitos enquanto o personagem carrega ou troca sem perder seleções.
    public void Suspender() { RestaurarPed(); }
    public void Limpar() { RestaurarPed(); transito = 0; pedestres = 0; firmeNaMoto = false; }
#endif
}
