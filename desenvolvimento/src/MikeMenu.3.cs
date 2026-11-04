using System;
using System.Globalization;
#if !TEST
using System.Windows.Forms;
using GTA;
using GTA.Native;
#endif

public static class RegrasMikeMenu
{
    public static bool TentarCalcular(string entrada, int atual, bool adicionar, out int resultado)
    {
        resultado = 0;
        int quantia;
        if (string.IsNullOrEmpty(entrada) || !int.TryParse(entrada, NumberStyles.None, CultureInfo.InvariantCulture, out quantia) || quantia < 0 || atual < 0) return false;
        long total = adicionar ? (long)atual + quantia : quantia;
        if (total > int.MaxValue) return false;
        resultado = (int)total;
        return true;
    }
    public static int MoverSelecao(int atual, int deslocamento, int quantidade) { return quantidade <= 0 ? 0 : ((atual + deslocamento) % quantidade + quantidade) % quantidade; }
    public static int InicioJanela(int indiceSelecionado, int quantidade, int visiveis) { if (quantidade <= 0 || visiveis <= 0) return 0; return Math.Min(Math.Max(0, indiceSelecionado - visiveis + 1), Math.Max(0, quantidade - visiveis)); }
    public static bool DestinoValido(float x, float y, float z) { return !float.IsNaN(x) && !float.IsInfinity(x) && !float.IsNaN(y) && !float.IsInfinity(y) && !float.IsNaN(z) && !float.IsInfinity(z) && Math.Abs(x) < 16000f && Math.Abs(y) < 16000f && z > -200f && z < 3000f; }
    public static bool AceitarSolo(float solo, float esperado, bool destinoFixo) { return DestinoValido(0f, 0f, solo) && (!destinoFixo || Math.Abs(solo - esperado) <= 12f); }
    public static bool PodeEditar(bool online, bool carregando, bool trocando, bool personagemValido)
    {
        return !online && !carregando && !trocando && personagemValido;
    }
}

#if !TEST
public class MikeMenu : Script
{
    private bool ocupado, aberto, jogadorInvencivel;
    private Vehicle veiculoProtegido;
    private Ped personagemProtegido;
    private string pagina = "principal";
    private int indiceSelecionado, categoria;
    private VehicleHash[] modelos = new VehicleHash[0];
    private readonly string[] categorias = { "Compactos", "Sedans", "SUVs", "Cupes", "Muscle", "Classicos", "Esportivos", "Super", "Motos", "Off-road", "Industriais", "Utilitarios", "Vans", "Bicicletas", "Barcos", "Helicopteros", "Avioes", "Servico", "Emergencia", "Militares", "Comerciais", "Trens", "Formula" };
    public MikeMenu() { KeyDown += AoPressionarTecla; Tick += AoAtualizar; Aborted += AoEncerrar; }
    private void ExibirMensagem(string texto) { GTA.UI.Screen.ShowSubtitle(texto, 6000); }
    private bool EstaPronto(out int modelo)
    {
        modelo = 0;
        bool online = Function.Call<bool>(Hash.NETWORK_IS_SESSION_ACTIVE) || Function.Call<bool>(Hash.NETWORK_IS_GAME_IN_PROGRESS);
        bool carregando = Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT) || !Function.Call<bool>(Hash.IS_PLAYER_PLAYING, Game.Player.Handle);
        bool trocando = Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS);
        var personagem = Game.Player.Character;
        if (personagem != null && personagem.Exists()) modelo = personagem.Model.Hash;
        bool personagemValido = modelo == unchecked((int)PedHash.Michael) || modelo == unchecked((int)PedHash.Franklin) || modelo == unchecked((int)PedHash.Trevor);
        return RegrasMikeMenu.PodeEditar(online, carregando, trocando, personagemValido);
    }
    private void LimparProtecoes()
    {
        if (jogadorInvencivel) { Function.Call(Hash.SET_PLAYER_INVINCIBLE, Game.Player.Handle, false); jogadorInvencivel = false; }
        if (personagemProtegido != null && personagemProtegido.Exists()) personagemProtegido.IsInvincible = false;
        personagemProtegido = null;
        if (veiculoProtegido != null && veiculoProtegido.Exists()) veiculoProtegido.IsInvincible = false;
        veiculoProtegido = null;
    }
    private void AoEncerrar(object remetente, EventArgs e) { LimparProtecoes(); }
    private void AplicarProtecaoJogador()
    {
        Ped atual = Game.Player.Character;
        if (atual == null || !atual.Exists()) return;
        if (personagemProtegido != null && personagemProtegido.Exists() && personagemProtegido.Handle != atual.Handle) personagemProtegido.IsInvincible = false;
        personagemProtegido = atual;
        Function.Call(Hash.SET_PLAYER_INVINCIBLE, Game.Player.Handle, true);
        atual.IsInvincible = true;
    }
    private void AoAtualizar(object remetente, EventArgs e)
    {
        int m;
        bool precisaValidar = aberto || jogadorInvencivel || veiculoProtegido != null;
        if (precisaValidar && !EstaPronto(out m))
        {
            aberto = false;
            if (Function.Call<bool>(Hash.NETWORK_IS_SESSION_ACTIVE) || Function.Call<bool>(Hash.NETWORK_IS_GAME_IN_PROGRESS)) LimparProtecoes();
            return;
        }
        if (jogadorInvencivel) AplicarProtecaoJogador();
        if (!aberto || ocupado) return;
        string[] itens = ObterItens();
        const float x = 36f, largura = 338f, topo = 42f, alturaCabecalho = 58f, alturaSecao = 31f, alturaLinha = 32f;
        const int linhasVisiveis = 9;
        int inicio = RegrasMikeMenu.InicioJanela(indiceSelecionado, itens.Length, linhasVisiveis);
        int linhas = Math.Min(linhasVisiveis, itens.Length - inicio);
        DesenharFundo(x, topo, largura, alturaCabecalho, System.Drawing.Color.FromArgb(240, 26, 67, 88));
        DesenharFundo(x, topo + alturaCabecalho - 4, largura, 4, System.Drawing.Color.FromArgb(255, 89, 174, 192));
        DesenharTexto("MIKE", x + 14, topo + 5, 0.67f, System.Drawing.Color.White, GTA.UI.Font.ChaletComprimeCologne);
        DesenharTexto("TRAINER  /  MODO HISTORIA", x + 115, topo + 26, 0.24f, System.Drawing.Color.FromArgb(210, 225, 237, 240), GTA.UI.Font.ChaletLondon);
        float topoSecao = topo + alturaCabecalho;
        DesenharFundo(x, topoSecao, largura, alturaSecao, System.Drawing.Color.FromArgb(240, 12, 16, 20));
        DesenharTexto(TituloPagina(), x + 12, topoSecao + 4, 0.34f, System.Drawing.Color.White, GTA.UI.Font.ChaletLondon);
        string contador = itens.Length == 0 ? "0 / 0" : (indiceSelecionado + 1) + " / " + itens.Length;
        DesenharTexto(contador, x + largura - 62, topoSecao + 6, 0.27f, System.Drawing.Color.FromArgb(210, 195, 203, 207), GTA.UI.Font.ChaletLondon);
        float topoLista = topoSecao + alturaSecao;
        if (linhas == 0) { DesenharFundo(x, topoLista, largura, alturaLinha, System.Drawing.Color.FromArgb(210, 12, 15, 19)); DesenharTexto("Nenhuma opcao disponivel", x + 12, topoLista + 5, 0.30f, System.Drawing.Color.White, GTA.UI.Font.ChaletLondon); linhas = 1; }
        else for (int i = 0; i < linhas; i++)
        {
            bool ativo = i + inicio == indiceSelecionado;
            float y = topoLista + i * alturaLinha;
            DesenharFundo(x, y, largura, alturaLinha, ativo ? System.Drawing.Color.FromArgb(245, 240, 242, 239) : System.Drawing.Color.FromArgb(220, 12, 15, 19));
            if (ativo) DesenharFundo(x, y, 4, alturaLinha, System.Drawing.Color.FromArgb(255, 76, 158, 180));
            DesenharTexto(itens[i + inicio], x + 12, y + 4, 0.32f, ativo ? System.Drawing.Color.FromArgb(255, 22, 28, 31) : System.Drawing.Color.White, GTA.UI.Font.ChaletLondon);
            if (ativo && (pagina == "principal" || pagina == "categorias")) DesenharTexto(">", x + largura - 19, y + 4, 0.32f, System.Drawing.Color.FromArgb(255, 22, 28, 31), GTA.UI.Font.ChaletLondon);
        }
        float topoRodape = topoLista + linhas * alturaLinha;
        DesenharFundo(x, topoRodape, largura, 27, System.Drawing.Color.FromArgb(235, 24, 29, 32));
        DesenharTexto("SETAS  NAVEGAR     ENTER  SELECIONAR", x + 11, topoRodape + 3, 0.24f, System.Drawing.Color.FromArgb(235, 220, 226, 228), GTA.UI.Font.ChaletLondon);
        DesenharTexto("BACKSPACE  VOLTAR     F9  FECHAR", x + 11, topoRodape + 16, 0.21f, System.Drawing.Color.FromArgb(200, 190, 200, 205), GTA.UI.Font.ChaletLondon);
    }
    private string TituloPagina()
    {
        switch (pagina) { case "principal": return "MENU PRINCIPAL"; case "categorias": return "ESCOLHER VEICULO"; case "modelos": return categorias[categoria].ToUpperInvariant(); case "tunagem": return "VEICULO / TUNAGEM"; case "pecas": return "PERSONALIZAR PECAS"; default: return pagina.ToUpperInvariant(); }
    }
    private void DesenharFundo(float x, float y, float largura, float altura, System.Drawing.Color cor) { new GTA.UI.ContainerElement(new System.Drawing.PointF(x, y), new System.Drawing.SizeF(largura, altura), cor).Draw(); }
    private void DesenharTexto(string texto, float x, float y, float escala, System.Drawing.Color cor, GTA.UI.Font fonte) { new GTA.UI.TextElement(texto, new System.Drawing.PointF(x, y), escala, cor, fonte).Draw(); }
    private string[] ObterItens()
    {
        switch (pagina)
        {
            case "principal": return new[] { "Dinheiro", "Jogador", "Policia", "Armas", "Veiculos: escolher", "Veiculo atual: tunagem", "Teletransporte", "Mundo" };
            case "dinheiro": return new[] { "Definir saldo", "Adicionar valor", "Adicionar $100.000", "Adicionar $1.000.000" };
            case "jogador": return new[] { "Restaurar vida e colete", "Invencibilidade: " + (jogadorInvencivel ? "LIGADA" : "DESLIGADA"), "Recuperar habilidade especial" };
            case "policia": return new[] { "Limpar procurado", "Nivel 1", "Nivel 2", "Nivel 3", "Nivel 4", "Nivel 5" };
            case "armas": return new[] { "Receber todas as armas disponiveis", "Completar municao", "Remover armas" };
            case "categorias": { var a = new string[categorias.Length + 1]; categorias.CopyTo(a, 0); a[a.Length - 1] = "Buscar por nome interno"; return a; }
            case "modelos": { var a = new string[modelos.Length]; for (int i = 0; i < a.Length; i++) { string nome = Function.Call<string>(Hash.GET_DISPLAY_NAME_FROM_VEHICLE_MODEL, (int)modelos[i]); a[i] = string.IsNullOrEmpty(nome) ? modelos[i].ToString() : nome; } return a; }
            case "tunagem": return new[] { "Reparar e limpar", "Melhorar desempenho ao maximo", "Turbo ligado", "Turbo desligado", "Cor: preto", "Cor: branco", "Cor: vermelho", "Cor: azul", "Cor: dourado", "Invencibilidade do veiculo", "Personalizar pecas" };
            case "pecas": return new[] { "Aerofolio", "Para-choque dianteiro", "Para-choque traseiro", "Saias", "Escapamento", "Gaiola", "Grade", "Capo", "Paralama esquerdo", "Paralama direito", "Teto", "Buzina", "Suspensao", "Rodas" };
            case "teletransporte": return new[] { "Marcador do mapa", "Aeroporto de Los Santos", "Praia de Vespucci", "Observatorio Galileo", "Pier de Del Perro", "Monte Chiliad", "Aerodromo de Sandy Shores" };
            case "mundo": return new[] { "Manha: 08:00", "Meio-dia: 12:00", "Noite: 22:00", "Ensolarado", "Limpo", "Nublado", "Chuva", "Tempestade" };
            default: return new string[0];
        }
    }
    private void AbrirPagina(string alvo) { pagina = alvo; indiceSelecionado = 0; }
    private void Voltar() { if (pagina == "principal") aberto = false; else if (pagina == "modelos") AbrirPagina("categorias"); else if (pagina == "pecas") AbrirPagina("tunagem"); else AbrirPagina("principal"); }
    private void AoPressionarTecla(object remetente, KeyEventArgs e)
    {
        if (ocupado) return;
        int antes;
        if (e.KeyCode == Keys.F9) { if (aberto) { aberto = false; return; } if (EstaPronto(out antes)) { aberto = true; AbrirPagina("principal"); } else ExibirMensagem("Menu disponivel apenas no modo historia, com um protagonista ativo."); return; }
        if (e.KeyCode == Keys.F10 || e.KeyCode == Keys.F11) { AlterarDinheiro(e.KeyCode == Keys.F11); return; }
        if (!aberto) return;
        if (!EstaPronto(out antes)) { aberto = false; LimparProtecoes(); return; }
        string[] itens = ObterItens();
        if (e.KeyCode == Keys.Up) indiceSelecionado = RegrasMikeMenu.MoverSelecao(indiceSelecionado, -1, itens.Length);
        else if (e.KeyCode == Keys.Down) indiceSelecionado = RegrasMikeMenu.MoverSelecao(indiceSelecionado, 1, itens.Length);
        else if (e.KeyCode == Keys.Back) Voltar();
        else if (e.KeyCode == Keys.Enter && itens.Length > 0) { ocupado = true; try { AtivarOpcao(antes); } catch (Exception) { ExibirMensagem("Acao indisponivel. Consulte ScriptHookVDotNet.log."); } finally { ocupado = false; } }
    }
    private void AlterarDinheiro(bool adicionar)
    {
        if (ocupado) return;
        ocupado = true;
        try { int antes, depois, resultado; if (!EstaPronto(out antes)) { ExibirMensagem("Disponivel apenas no modo historia."); return; }
            ExibirMensagem(adicionar ? "Digite quanto adicionar." : "Digite o novo saldo.");
            string entrada = Game.GetUserInput("");
            if (string.IsNullOrEmpty(entrada)) { ExibirMensagem("Operacao cancelada."); return; }
            if (!EstaPronto(out depois) || antes != depois) { ExibirMensagem("Operacao cancelada: jogo ou personagem mudou."); return; }
            if (!RegrasMikeMenu.TentarCalcular(entrada, Game.Player.Money, adicionar, out resultado)) { ExibirMensagem("Valor invalido ou acima do limite."); return; }
            Game.Player.Money = resultado; if (Game.Player.Money != resultado) { ExibirMensagem("Nao foi possivel confirmar o saldo."); return; } ExibirMensagem("Saldo: $" + resultado.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")) + ". Salve o jogo.");
        } catch (Exception) { ExibirMensagem("Nao foi possivel alterar o saldo."); } finally { ocupado = false; }
    }
    private void AtivarOpcao(int antes)
    {
        var personagem = Game.Player.Character;
        if (pagina == "principal") { string[] paginas = { "dinheiro", "jogador", "policia", "armas", "categorias", "tunagem", "teletransporte", "mundo" }; AbrirPagina(paginas[indiceSelecionado]); return; }
        if (pagina == "dinheiro") { if (indiceSelecionado < 2) { ocupado = false; AlterarDinheiro(indiceSelecionado == 1); ocupado = true; } else { int total; string quantia = indiceSelecionado == 2 ? "100000" : "1000000"; if (RegrasMikeMenu.TentarCalcular(quantia, Game.Player.Money, true, out total)) { Game.Player.Money = total; ExibirMensagem("Saldo atualizado. Salve o jogo."); } else ExibirMensagem("Limite de dinheiro atingido."); } return; }
        if (pagina == "jogador") { if (indiceSelecionado == 0) { personagem.Health = personagem.MaxHealth; personagem.Armor = 100; } else if (indiceSelecionado == 1) { jogadorInvencivel = !jogadorInvencivel; if (jogadorInvencivel) { AplicarProtecaoJogador(); ExibirMensagem("Invencibilidade ligada."); } else { Function.Call(Hash.SET_PLAYER_INVINCIBLE, Game.Player.Handle, false); if (personagemProtegido != null && personagemProtegido.Exists()) personagemProtegido.IsInvincible = false; personagemProtegido = null; ExibirMensagem("Invencibilidade desligada."); } } else Function.Call(Hash.SPECIAL_ABILITY_FILL_METER, Game.Player.Handle, true); return; }
        if (pagina == "policia") { Game.Player.WantedLevel = indiceSelecionado; return; }
        if (pagina == "armas") { if (indiceSelecionado == 2) Function.Call(Hash.REMOVE_ALL_PED_WEAPONS, personagem.Handle, true); else if (indiceSelecionado == 1) Function.Call(Hash.SET_PED_INFINITE_AMMO_CLIP, personagem.Handle, false); else { int recebidas = 0; var vistos = new System.Collections.Generic.HashSet<int>(); foreach (WeaponHash arma in Enum.GetValues(typeof(WeaponHash))) { int codigo = unchecked((int)arma); if (arma == WeaponHash.Unarmed || !vistos.Add(codigo) || !Function.Call<bool>(Hash.IS_WEAPON_VALID, codigo)) continue; Function.Call(Hash.GIVE_WEAPON_TO_PED, personagem.Handle, codigo, 999, false, false); if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, personagem.Handle, codigo, false)) recebidas++; } ExibirMensagem(recebidas + " armas aceitas pelo personagem."); } if (indiceSelecionado == 1) { foreach (WeaponHash w in Enum.GetValues(typeof(WeaponHash))) if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, personagem.Handle, (int)w, false)) Function.Call(Hash.SET_PED_AMMO, personagem.Handle, (int)w, 999); } return; }
        if (pagina == "categorias") { if (indiceSelecionado == categorias.Length) { string nome = Game.GetUserInput(""); int depois; if (!string.IsNullOrWhiteSpace(nome) && EstaPronto(out depois) && antes == depois) CriarVeiculo(new Model(nome)); } else { categoria = indiceSelecionado; modelos = Vehicle.GetAllModelsOfClass((VehicleClass)categoria); AbrirPagina("modelos"); if (modelos.Length == 0) ExibirMensagem("Nenhum modelo disponivel nesta categoria."); } return; }
        if (pagina == "modelos") { CriarVeiculo(new Model((int)modelos[indiceSelecionado])); return; }
        if (pagina == "tunagem") { if (indiceSelecionado == 10) AbrirPagina("pecas"); else TunarVeiculo(personagem.CurrentVehicle); return; }
        if (pagina == "pecas") { Personalizar(personagem.CurrentVehicle); return; }
        if (pagina == "teletransporte") { Teletransportar(antes); return; }
        if (pagina == "mundo") { if (indiceSelecionado < 3) Function.Call(Hash.SET_CLOCK_TIME, indiceSelecionado == 0 ? 8 : indiceSelecionado == 1 ? 12 : 22, 0, 0); else { string[] weather = { "EXTRASUNNY", "CLEAR", "CLOUDS", "RAIN", "THUNDER" }; Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, weather[indiceSelecionado - 3]); } }
    }
    private void Teletransportar(int antes)
    {
        GTA.Math.Vector3[] destinos = {
            new GTA.Math.Vector3(0f, 0f, 0f),
            new GTA.Math.Vector3(-1034.6f, -2733.6f, 20.2f),
            new GTA.Math.Vector3(-1221.6f, -1498.1f, 4.4f),
            new GTA.Math.Vector3(-438.804f, 1076.097f, 352.411f),
            new GTA.Math.Vector3(-1605.4f, -1037.0f, 13.0f),
            new GTA.Math.Vector3(501.8f, 5604.8f, 797.9f),
            new GTA.Math.Vector3(1735.0f, 3294.0f, 41.2f)
        };
        Ped personagem = Game.Player.Character;
        Vehicle veiculo = personagem.CurrentVehicle;
        if (veiculo != null && veiculo.Exists() && personagem.SeatIndex != VehicleSeat.Driver) { ExibirMensagem("Para levar o veiculo, fique no banco do motorista."); return; }
        GTA.Math.Vector3 destino = destinos[indiceSelecionado];
        bool destinoFixo = indiceSelecionado != 0;
        if (!destinoFixo)
        {
            if (!Game.IsWaypointActive) { ExibirMensagem("Marque um destino no mapa primeiro."); return; }
            destino = World.WaypointPosition;
        }
        if (!RegrasMikeMenu.DestinoValido(destino.X, destino.Y, destinoFixo ? destino.Z : 0f)) { ExibirMensagem("Destino invalido."); return; }
        float alturaEsperada = destino.Z;
        float alturaFoco = destinoFixo ? alturaEsperada : 100f;
        bool encontrado = false;
        Entity alvo = veiculo != null && veiculo.Exists() ? (Entity)veiculo : personagem;
        GTA.Math.Vector3 posicaoOriginal = alvo.Position;
        bool estavaCongelado = alvo.IsPositionFrozen;
        bool moveu = false;
        bool colisaoPronta = false;
        try
        {
            Function.Call(Hash.SET_FOCUS_POS_AND_VEL, destino.X, destino.Y, alturaFoco, 0f, 0f, 0f);
            for (int tentativa = 0; tentativa < 30; tentativa++)
            {
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, destino.X, destino.Y, alturaFoco);
                float solo;
                if (World.GetGroundHeight(new GTA.Math.Vector3(destino.X, destino.Y, 1000f), out solo, GetGroundHeightMode.Normal) && RegrasMikeMenu.AceitarSolo(solo, alturaEsperada, destinoFixo)) { destino.Z = solo + 1.5f; encontrado = true; break; }
                Script.Wait(100);
                int depois; if (!EstaPronto(out depois) || depois != antes) { ExibirMensagem("Teletransporte cancelado."); return; }
            }
            if (!encontrado) { ExibirMensagem("Solo nao confirmado. Teletransporte cancelado."); return; }
            int atual;
            if (!EstaPronto(out atual) || atual != antes || personagem == null || !personagem.Exists() || personagem.Handle != Game.Player.Character.Handle || !alvo.Exists()) { ExibirMensagem("Teletransporte cancelado."); return; }
            if (veiculo != null && veiculo.Exists()) { if (personagem.CurrentVehicle == null || personagem.CurrentVehicle.Handle != veiculo.Handle || personagem.SeatIndex != VehicleSeat.Driver) { ExibirMensagem("Teletransporte cancelado: veiculo mudou."); return; } }
            else if (personagem.CurrentVehicle != null) { ExibirMensagem("Teletransporte cancelado: veiculo mudou."); return; }
            alvo.IsPositionFrozen = true;
            alvo.Position = destino;
            moveu = true;
            for (int tentativa = 0; tentativa < 30; tentativa++)
            {
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, destino.X, destino.Y, destino.Z);
                if (Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, alvo.Handle)) { colisaoPronta = true; break; }
                Script.Wait(100);
                int depois; if (!EstaPronto(out depois) || depois != antes || !alvo.Exists()) break;
            }
            if (!colisaoPronta)
            {
                if (alvo.Exists()) alvo.Position = posicaoOriginal;
                moveu = false;
                ExibirMensagem("Terreno nao carregou. Retornando ao local anterior.");
                return;
            }
        }
        finally
        {
            if (alvo != null && alvo.Exists()) { if (moveu && !colisaoPronta) alvo.Position = posicaoOriginal; alvo.IsPositionFrozen = estavaCongelado; }
            Function.Call(Hash.CLEAR_FOCUS);
        }
        if (!moveu) return;
        ExibirMensagem("Destino alcancado.");
    }
    private void CriarVeiculo(Model modelo)
    {
        if (!modelo.IsInCdImage || !modelo.IsVehicle) { ExibirMensagem("Modelo de veiculo nao encontrado."); return; }
        int antes, depois; if (!EstaPronto(out antes) || !modelo.Request(1500) || !EstaPronto(out depois) || antes != depois) { ExibirMensagem("Nao foi possivel carregar o veiculo."); return; }
        var personagem = Game.Player.Character;
        var veiculo = World.CreateVehicle(modelo, personagem.GetOffsetPosition(new GTA.Math.Vector3(0, 7, 0)), personagem.Heading);
        modelo.MarkAsNoLongerNeeded();
        ExibirMensagem(veiculo != null && veiculo.Exists() ? "Veiculo criado perto de voce." : "Nao foi possivel criar o veiculo neste local.");
    }
    private void Personalizar(Vehicle veiculo)
    {
        if (veiculo == null || !veiculo.Exists()) { ExibirMensagem("Entre em um veiculo primeiro."); return; }
        int[] tipos = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 14, 15, 23 };
        int h = veiculo.Handle, tipo = tipos[indiceSelecionado]; Function.Call(Hash.SET_VEHICLE_MOD_KIT, h, 0);
        int quantidade = Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, h, tipo);
        if (quantidade <= 0) { ExibirMensagem("Este veiculo nao oferece esta peca."); return; }
        int proximo = Function.Call<int>(Hash.GET_VEHICLE_MOD, h, tipo) + 1; if (proximo >= quantidade) proximo = -1;
        Function.Call(Hash.SET_VEHICLE_MOD, h, tipo, proximo, false); ExibirMensagem(proximo < 0 ? "Peca original selecionada." : "Peca " + (proximo + 1) + " de " + quantidade + " aplicada.");
    }
    private void TunarVeiculo(Vehicle veiculo)
    {
        if (veiculo == null || !veiculo.Exists()) { ExibirMensagem("Entre em um veiculo primeiro."); return; }
        int h = veiculo.Handle;
        Function.Call(Hash.SET_VEHICLE_MOD_KIT, h, 0);
        if (indiceSelecionado == 0) { veiculo.Repair(); Function.Call(Hash.SET_VEHICLE_DIRT_LEVEL, h, 0f); }
        else if (indiceSelecionado == 1) { int[] mods = { 11, 12, 13, 15, 16 }; foreach (int modificacao in mods) { int quantidade = Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, h, modificacao); if (quantidade > 0) Function.Call(Hash.SET_VEHICLE_MOD, h, modificacao, quantidade - 1, false); } Function.Call(Hash.TOGGLE_VEHICLE_MOD, h, 18, true); ExibirMensagem("Melhorias disponiveis aplicadas."); }
        else if (indiceSelecionado == 2 || indiceSelecionado == 3) Function.Call(Hash.TOGGLE_VEHICLE_MOD, h, 18, indiceSelecionado == 2);
        else if (indiceSelecionado <= 8) { int[] cores = { 0, 111, 27, 64, 158 }; Function.Call(Hash.SET_VEHICLE_COLOURS, h, cores[indiceSelecionado - 4], cores[indiceSelecionado - 4]); }
        else { if (veiculoProtegido != null && veiculoProtegido.Exists()) { bool mesmo = veiculoProtegido.Handle == h; veiculoProtegido.IsInvincible = false; veiculoProtegido = null; if (mesmo) { ExibirMensagem("Invencibilidade do veiculo desligada."); return; } } veiculoProtegido = veiculo; veiculo.IsInvincible = true; ExibirMensagem("Invencibilidade do veiculo ligada."); }
    }
}
#endif
