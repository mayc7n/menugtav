using GTA;
using GTA.Native;

// O chamador valida modo história e protagonista antes de Atualizar.
public sealed class EfeitosExtrasMikeMenu
{
    private bool socosExplosivos, folegoInfinito, cameraLenta, tempoAplicado;

    public bool SocosExplosivos { get { return socosExplosivos; } }
    public bool FolegoInfinito { get { return folegoInfinito; } }
    public bool CameraLenta { get { return cameraLenta; } }
    public bool Ativo { get { return socosExplosivos || folegoInfinito || cameraLenta || tempoAplicado; } }

    public void AlternarSocos() { socosExplosivos = !socosExplosivos; }
    public void AlternarFolego() { folegoInfinito = !folegoInfinito; }
    public void AlternarCameraLenta()
    {
        cameraLenta = !cameraLenta;
        if (!cameraLenta) RestaurarTempo();
    }

    public void Atualizar()
    {
        int jogador = Game.Player.Handle;
        if (socosExplosivos) Function.Call(Hash.SET_EXPLOSIVE_MELEE_THIS_FRAME, jogador);
        if (folegoInfinito) Function.Call(Hash.RESTORE_PLAYER_STAMINA, jogador, 1f);
        if (cameraLenta && !tempoAplicado) { Function.Call(Hash.SET_TIME_SCALE, 0.5f); tempoAplicado = true; }
    }

    private void RestaurarTempo()
    {
        if (tempoAplicado) { Function.Call(Hash.SET_TIME_SCALE, 1f); tempoAplicado = false; }
    }

    // Durante carregamento ou troca, evita manter a velocidade global alterada.
    public void Suspender() { RestaurarTempo(); }
    public void Limpar()
    {
        RestaurarTempo();
        socosExplosivos = false;
        folegoInfinito = false;
        cameraLenta = false;
    }
}
