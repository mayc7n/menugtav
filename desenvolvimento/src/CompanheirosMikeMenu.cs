using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using GTA.Math;

// Owns only NPCs created by this menu. The caller checks story-mode readiness
// before every action and calls Limpar() during unload and when entering network.
public sealed class CompanheirosMikeMenu
{
    private readonly List<Ped> guardas = new List<Ped>();
    private Ped motorista;
    private const int LimiteGuardas = 3;
    private const string Modelo = "s_m_m_security_01";
    public bool TemAtivos { get { return guardas.Count > 0 || motorista != null; } }

    private bool Pronto(Ped esperado)
    {
        if (esperado == null || !esperado.Exists() || esperado.IsDead) return false;
        if (Function.Call<bool>(Hash.NETWORK_IS_SESSION_ACTIVE) || Function.Call<bool>(Hash.NETWORK_IS_GAME_IN_PROGRESS) || Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS)) return false;
        if (!Function.Call<bool>(Hash.IS_PLAYER_PLAYING, Game.Player.Handle) || Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT)) return false;
        Ped atual = Game.Player.Character;
        if (atual == null || !atual.Exists() || atual.Handle != esperado.Handle) return false;
        int h = atual.Model.Hash;
        return h == unchecked((int)PedHash.Michael) || h == unchecked((int)PedHash.Franklin) || h == unchecked((int)PedHash.Trevor);
    }

    public string ChamarGuarda(Ped jogador)
    {
        for (int i = guardas.Count - 1; i >= 0; i--)
        {
            Ped anterior = guardas[i];
            if (anterior == null || !anterior.Exists()) guardas.RemoveAt(i);
            else if (anterior.IsDead) { anterior.Delete(); guardas.RemoveAt(i); }
        }
        if (!Pronto(jogador)) return "Personagem indisponivel.";
        if (jogador.IsInVehicle()) return "Saia do veiculo para chamar um guarda.";
        if (guardas.Count >= LimiteGuardas) return "Limite de 3 guardas atingido.";
        Model modelo = new Model(Modelo);
        if (!modelo.IsInCdImage || !modelo.IsPed || !modelo.Request(1000)) return "Modelo de guarda indisponivel.";
        try
        {
            if (!Pronto(jogador) || jogador.IsInVehicle()) return "Chamada cancelada: personagem mudou.";
            Ped guarda = World.CreatePed(modelo, jogador.GetOffsetPosition(new Vector3(1.5f, 1.5f, 0f)));
            if (guarda == null || !guarda.Exists()) return "Nao foi possivel criar o guarda.";
            guardas.Add(guarda);
            int grupo = Function.Call<int>(Hash.GET_PLAYER_GROUP, Game.Player.Handle);
            Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, guarda.Handle, grupo);
            Function.Call(Hash.SET_PED_NEVER_LEAVES_GROUP, guarda.Handle, true);
            Function.Call(Hash.GIVE_WEAPON_TO_PED, guarda.Handle, Game.GenerateHash("WEAPON_PISTOL"), 120, false, true);
            return "Guarda chamado. Maximo: 3.";
        }
        finally { modelo.MarkAsNoLongerNeeded(); }
    }

    public string ChamarMotorista(Ped jogador)
    {
        if (!Pronto(jogador)) return "Personagem indisponivel.";
        if (motorista != null && motorista.Exists() && !motorista.IsDead) return "Dispense o motorista atual primeiro.";
        if (motorista != null && motorista.Exists()) motorista.Delete();
        motorista = null;
        Vehicle carro = jogador.CurrentVehicle;
        if (carro == null || !carro.Exists() || jogador.SeatIndex == VehicleSeat.Driver) return "Entre como passageiro em um carro.";
        if (!carro.Model.IsCar || carro.Speed > 1f) return "Pare o carro antes de chamar o motorista.";
        if (!Function.Call<bool>(Hash.IS_VEHICLE_SEAT_FREE, carro.Handle, -1, false)) return "O banco do motorista deve estar livre.";
        if (!Game.IsWaypointActive) return "Marque um destino no mapa primeiro.";
        Vector3 destino = World.WaypointPosition;
        if (float.IsNaN(destino.X) || float.IsNaN(destino.Y) || float.IsInfinity(destino.X) || float.IsInfinity(destino.Y) || Math.Abs(destino.X) > 16000f || Math.Abs(destino.Y) > 16000f) return "Destino invalido.";
        float solo;
        if (!World.GetGroundHeight(new Vector3(destino.X, destino.Y, 1000f), out solo, GetGroundHeightMode.Normal) || float.IsNaN(solo) || float.IsInfinity(solo)) return "Nao foi possivel confirmar o terreno do destino.";
        Model modelo = new Model(Modelo);
        if (!modelo.IsInCdImage || !modelo.IsPed || !modelo.Request(1000)) return "Modelo de motorista indisponivel.";
        try
        {
            if (!Pronto(jogador) || !carro.Exists() || carro.Speed > 1f || jogador.CurrentVehicle == null || jogador.CurrentVehicle.Handle != carro.Handle || jogador.SeatIndex == VehicleSeat.Driver || !Function.Call<bool>(Hash.IS_VEHICLE_SEAT_FREE, carro.Handle, -1, false) || !Game.IsWaypointActive) return "Chamada cancelada: situacao mudou.";
            Vector3 destinoAtual = World.WaypointPosition;
            if (Math.Abs(destinoAtual.X - destino.X) > 1f || Math.Abs(destinoAtual.Y - destino.Y) > 1f) return "Chamada cancelada: marcador mudou.";
            motorista = World.CreatePed(modelo, jogador.Position);
            if (motorista == null || !motorista.Exists()) { motorista = null; return "Nao foi possivel criar o motorista."; }
            Function.Call(Hash.SET_PED_INTO_VEHICLE, motorista.Handle, carro.Handle, -1);
            Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE, motorista.Handle, carro.Handle, destino.X, destino.Y, solo, 15f, 786603, 12f);
            return "Motorista a caminho do marcador.";
        }
        finally { modelo.MarkAsNoLongerNeeded(); }
    }

    public string DispensarGuardas()
    {
        int total = guardas.Count;
        foreach (Ped guarda in guardas) if (guarda != null && guarda.Exists()) { Function.Call(Hash.REMOVE_PED_FROM_GROUP, guarda.Handle); guarda.Delete(); }
        guardas.Clear();
        return total == 0 ? "Nenhum guarda ativo." : "Guardas dispensados.";
    }
    public string DispensarMotorista()
    {
        if (motorista == null || !motorista.Exists()) { motorista = null; return "Nenhum motorista ativo."; }
        motorista.Delete(); motorista = null;
        return "Motorista dispensado.";
    }
    public void Limpar() { DispensarGuardas(); DispensarMotorista(); }
}
