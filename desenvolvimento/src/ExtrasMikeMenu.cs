using System.Drawing;
using GTA;
using GTA.Native;

public sealed class ExtrasMikeMenu
{
    public bool Velocimetro { get; private set; }
    public bool RadioPortatil { get; private set; }
    public bool Ativo { get { return Velocimetro || RadioPortatil; } }

    public void AlternarVelocimetro() { Velocimetro = !Velocimetro; }
    public void AlternarRadio()
    {
        RadioPortatil = !RadioPortatil;
        Function.Call(Hash.SET_MOBILE_RADIO_ENABLED_DURING_GAMEPLAY, RadioPortatil);
        Function.Call(Hash.SET_MOBILE_PHONE_RADIO_STATE, RadioPortatil);
    }
    public void Limpar()
    {
        Velocimetro = false;
        if (RadioPortatil) AlternarRadio();
    }
    public void Atualizar(Ped jogador)
    {
        if (!Velocimetro || jogador == null || !jogador.Exists()) return;
        Vehicle v = jogador.CurrentVehicle;
        if (v == null || !v.Exists()) return;
        string valor = System.Math.Max(0, (int)System.Math.Round(v.Speed * 3.6f)) + " km/h";
        new GTA.UI.TextElement(valor, new PointF(1080f, 630f), 0.45f, Color.White, GTA.UI.Font.ChaletLondon).Draw();
    }
    public string Impulso(Ped jogador)
    {
        Vehicle v = jogador == null ? null : jogador.CurrentVehicle;
        if (v == null || !v.Exists() || jogador.SeatIndex != VehicleSeat.Driver) return "Entre como motorista em um veiculo.";
        Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, v.Handle, System.Math.Min(55f, System.Math.Max(0f, v.Speed) + 8f));
        return "Impulso aplicado.";
    }
    public string Frear(Ped jogador)
    {
        Vehicle v = jogador == null ? null : jogador.CurrentVehicle;
        if (v == null || !v.Exists() || jogador.SeatIndex != VehicleSeat.Driver) return "Entre como motorista em um veiculo.";
        Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, v.Handle, 0f);
        return "Velocidade reduzida.";
    }
}
