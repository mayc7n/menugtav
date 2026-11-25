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
    public static bool CorrespondeBusca(string nome, string busca) { return !string.IsNullOrWhiteSpace(nome) && !string.IsNullOrWhiteSpace(busca) && CultureInfo.InvariantCulture.CompareInfo.IndexOf(nome, busca.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0; }
    public static int[] RemoverDuplicados(int[] valores)
    {
        var vistos = new System.Collections.Generic.HashSet<int>();
        var resultado = new System.Collections.Generic.List<int>();
        foreach (int valor in valores) if (vistos.Add(valor)) resultado.Add(valor);
        return resultado.ToArray();
    }
    public static string PaginaPrincipal(int indice)
    {
        string[] paginas = { "jogador", "armas", "veiculos", "roupas", "policia", "teletransporte", "mundo", "pedestres", "dinheiro" };
        return indice >= 0 && indice < paginas.Length ? paginas[indice] : null;
    }
    public static int ProximaVariacao(int atual, int quantidade) { return quantidade <= 0 ? -1 : atual < 0 || atual >= quantidade ? 0 : (atual + 1) % quantidade; }
    public static int InicioJanela(int indiceSelecionado, int quantidade, int visiveis) { if (quantidade <= 0 || visiveis <= 0) return 0; return Math.Min(Math.Max(0, indiceSelecionado - visiveis + 1), Math.Max(0, quantidade - visiveis)); }
    public static bool DestinoValido(float x, float y, float z) { return !float.IsNaN(x) && !float.IsInfinity(x) && !float.IsNaN(y) && !float.IsInfinity(y) && !float.IsNaN(z) && !float.IsInfinity(z) && Math.Abs(x) < 16000f && Math.Abs(y) < 16000f && z > -200f && z < 3000f; }
    public static bool AceitarSolo(float solo, float esperado, bool destinoFixo) { return DestinoValido(0f, 0f, solo) && (!destinoFixo || Math.Abs(solo - esperado) <= 12f); }
    public static bool PodeEditar(bool online, bool carregando, bool trocando, bool personagemValido)
    {
        return !online && !carregando && !trocando && personagemValido;
    }
}

[Serializable]
public sealed class LocalMikeMenu { public int Versao; public float X, Y, Z; }

[Serializable]
public sealed class ModelosFavoritosMikeMenu { public int Versao; public int[] Modelos; }

#if !TEST
public class MikeMenu : Script
{
    private bool ocupado, aberto, jogadorInvencivel, nuncaProcurado;
    private readonly CompanheirosMikeMenu companheiros = new CompanheirosMikeMenu();
    private readonly ExtrasMikeMenu extras = new ExtrasMikeMenu();
    private readonly MundoExtraMikeMenu mundoExtra = new MundoExtraMikeMenu();
    private readonly AtalhosMikeMenu atalhos;
    private bool superPulo, corridaRapida, natacaoRapida, municaoInfinita, semRecarga, relogioParado, blecaute;
    private bool tirosExplosivos, tirosIncendiarios;
    private int pedAnimacao;
    private string dicionarioAnimacao, nomeAnimacao;
    private int pedMunicao, armaMunicao, pedSemRecarga;
    private Vehicle veiculoProtegido;
    private Ped personagemProtegido;
    private string pagina = "principal";
    private int indiceSelecionado, categoria;
    private VehicleHash[] modelos = new VehicleHash[0];
    private string origemModelos = "categorias", tituloModelos = "";
    private string dicionarioPrevia;
    private int inicioPrevia;
    private string[] nomesModelos = new string[0];
    private int componenteRoupa, roupaOriginal, texturaOriginal, pedRoupa;
    private readonly int[] componentesRoupa = { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
    private readonly string[] nomesRoupa = { "Mascara", "Bracos", "Calcas", "Bolsa / paraquedas", "Calcados", "Acessorios", "Camisa interna", "Colete", "Detalhes", "Parte superior" };
    private readonly string[] armasNomes = { "Pistola", "Pistola de combate", "Micro SMG", "SMG", "Rifle de assalto", "Carabina", "Escopeta", "Rifle de precisao", "Granada", "Paraquedas" };
    private readonly string[] armasCodigos = { "WEAPON_PISTOL", "WEAPON_COMBATPISTOL", "WEAPON_MICROSMG", "WEAPON_SMG", "WEAPON_ASSAULTRIFLE", "WEAPON_CARBINERIFLE", "WEAPON_PUMPSHOTGUN", "WEAPON_SNIPERRIFLE", "WEAPON_GRENADE", "GADGET_PARACHUTE" };
    private readonly string[] iconesArmas = { "w_pi_pistol", "w_pi_combatpistol", "w_sb_microsmg", "w_sb_smg", "w_ar_assaultrifle", "w_ar_carbinerifle", "w_sg_pumpshotgun", "w_sr_sniperrifle", null, null };
    private readonly string[] dicionariosArmas = { "MPWeaponsGang1", "MPWeaponsCommon", "MPWeaponsCommon", "MPWeaponsGang0", "MPWeaponsGang1", "MPWeaponsGang0", "MPWeaponsCommon", "MPWeaponsGang0", null, null };
    private readonly string[] categorias = { "Compactos", "Sedans", "SUVs", "Cupes", "Muscle", "Classicos", "Esportivos", "Super", "Motos", "Off-road", "Industriais", "Utilitarios", "Vans", "Bicicletas", "Barcos", "Helicopteros", "Avioes", "Servico", "Emergencia", "Militares", "Comerciais", "Trens", "Formula" };
    public MikeMenu() { string aviso; atalhos = AtalhosMikeMenu.Carregar(out aviso); KeyDown += AoPressionarTecla; Tick += AoAtualizar; Aborted += AoEncerrar; if (aviso != null) ExibirMensagem(aviso); }
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
        companheiros.Limpar();
        extras.Limpar();
        mundoExtra.Limpar();
        LiberarPrevia();
        nuncaProcurado = false;
        LimparEfeitosAdicionais();
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
        bool precisaValidar = aberto || jogadorInvencivel || nuncaProcurado || veiculoProtegido != null || superPulo || corridaRapida || natacaoRapida || municaoInfinita || semRecarga || relogioParado || blecaute || tirosExplosivos || tirosIncendiarios || pedAnimacao != 0 || companheiros.TemAtivos || extras.Ativo || mundoExtra.Ativo;
        if (precisaValidar && !EstaPronto(out m))
        {
            aberto = false;
            LiberarPrevia();
            mundoExtra.Suspender();
            if (Function.Call<bool>(Hash.NETWORK_IS_SESSION_ACTIVE) || Function.Call<bool>(Hash.NETWORK_IS_GAME_IN_PROGRESS)) LimparProtecoes();
            return;
        }
        if (jogadorInvencivel) AplicarProtecaoJogador();
        if (nuncaProcurado) Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL, Game.Player.Handle);
        AtualizarEfeitos();
        if (extras.Ativo) extras.Atualizar(Game.Player.Character);
        if (mundoExtra.Ativo) mundoExtra.Atualizar(Game.Player.Character);
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
        DesenharTexto(pagina == "modelos" ? "F5  FAVORITAR     BACKSPACE  VOLTAR" : "BACKSPACE  VOLTAR     DEL  FECHAR", x + 11, topoRodape + 16, 0.21f, System.Drawing.Color.FromArgb(200, 190, 200, 205), GTA.UI.Font.ChaletLondon);
        DesenharPrevia();
    }
    private string TituloPagina()
    {
        switch (pagina) { case "principal": return "MENU PRINCIPAL"; case "categorias": return "ESCOLHER VEICULO"; case "modelos": return tituloModelos; case "tunagem": return "VEICULO / TUNAGEM"; case "pecas": return "PERSONALIZAR PECAS"; default: return pagina.ToUpperInvariant(); }
    }
    private void DesenharFundo(float x, float y, float largura, float altura, System.Drawing.Color cor) { new GTA.UI.ContainerElement(new System.Drawing.PointF(x, y), new System.Drawing.SizeF(largura, altura), cor).Draw(); }
    private void DesenharTexto(string texto, float x, float y, float escala, System.Drawing.Color cor, GTA.UI.Font fonte) { new GTA.UI.TextElement(texto, new System.Drawing.PointF(x, y), escala, cor, fonte).Draw(); }
    private void LiberarPrevia()
    {
        if (dicionarioPrevia != null) { Function.Call(Hash.SET_STREAMED_TEXTURE_DICT_AS_NO_LONGER_NEEDED, dicionarioPrevia); dicionarioPrevia = null; }
    }
    private void DesenharPrevia()
    {
        if (pagina != "modelos" && pagina != "lista_armas" && pagina != "roupas" && pagina != "roupa_peca") { LiberarPrevia(); return; }
        string dicionario = null, textura = null;
        bool roupa = pagina == "roupas" || pagina == "roupa_peca";
        bool veiculo = pagina == "modelos";
        if (pagina == "lista_armas" && indiceSelecionado < iconesArmas.Length) { dicionario = dicionariosArmas[indiceSelecionado]; textura = iconesArmas[indiceSelecionado]; }
        else if (veiculo && indiceSelecionado < modelos.Length)
        {
            int codigo = (int)modelos[indiceSelecionado];
            string[] carros = { "adder", "cheetah", "banshee", "zentorno", "bmx" };
            string[] dicionarios = { "lgm_default", "lgm_default", "lgm_default", "lgm_dlc_business2", "pandm_default" };
            for (int i = 0; i < carros.Length; i++) if (codigo == Game.GenerateHash(carros[i])) { dicionario = dicionarios[i]; textura = carros[i]; break; }
        }
        if (dicionarioPrevia != dicionario)
        {
            LiberarPrevia();
            if (dicionario != null) { dicionarioPrevia = dicionario; inicioPrevia = Game.GameTime; }
        }
        bool pronta = false;
        if (dicionario != null)
        {
            if (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, dicionario))
                Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, dicionario, false);
            pronta = Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, dicionario);
        }
        const float x = 382f, y = 100f;
        DesenharFundo(x, y, 230f, 160f, System.Drawing.Color.FromArgb(225, 12, 16, 20));
        DesenharTexto("PREVIA", x + 12f, y + 7f, 0.31f, System.Drawing.Color.White, GTA.UI.Font.ChaletLondon);
        string imagemItem = null;
        if (veiculo && indiceSelecionado < modelos.Length) imagemItem = MapaPreviaMikeMenu.ChaveVeiculo((int)modelos[indiceSelecionado]);
        else if (pagina == "roupa_peca" && Game.Player.Character != null && Game.Player.Character.Exists() && Game.Player.Character.Handle == pedRoupa)
            imagemItem = MapaPreviaMikeMenu.ChaveRoupa(Game.Player.Character.Model.Hash, componenteRoupa, Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, pedRoupa, componenteRoupa), Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, pedRoupa, componenteRoupa));
        if (imagemItem != null && PreviaMikeMenu.Desenhar(imagemItem, x + 12f, y + 31f, 206f, 98f)) { DesenharTexto("IMAGEM DO ITEM", x + 12f, y + 134f, 0.22f, System.Drawing.Color.White, GTA.UI.Font.ChaletLondon); return; }
        if (pronta && !roupa)
        {
            if (veiculo) { PreviaMikeMenu.Desenhar("veiculo", x + 12f, y + 31f, 206f, 98f); DesenharTexto("IMAGEM PODE SER ILUSTRATIVA", x + 12f, y + 134f, 0.22f, System.Drawing.Color.White, GTA.UI.Font.ChaletLondon); }
            Function.Call(Hash.DRAW_SPRITE, dicionario, textura, (x + 12f + 103f) / 1280f, (y + 38f + 55f) / 720f, 206f / 1280f, 110f / 720f, 0f, 255, 255, 255, 255);
        }
        else if (roupa || veiculo)
        {
            bool desenhou = PreviaMikeMenu.Desenhar(roupa ? "roupa" : "veiculo", x + 12f, y + 31f, 206f, 98f);
            if (!desenhou) DesenharTexto("Ilustracao indisponivel", x + 12f, y + 67f, 0.26f, System.Drawing.Color.White, GTA.UI.Font.ChaletLondon);
            DesenharTexto(roupa ? "ILUSTRATIVA - veja no personagem" : "ILUSTRACAO GENERICA", x + 12f, y + 134f, 0.22f, System.Drawing.Color.White, GTA.UI.Font.ChaletLondon);
        }
        else DesenharTexto(dicionario != null && Game.GameTime - inicioPrevia <= 3000 ? "Carregando imagem..." : "Imagem indisponivel", x + 12f, y + 72f, 0.27f, System.Drawing.Color.White, GTA.UI.Font.ChaletLondon);
    }
    private string[] ObterItens()
    {
        switch (pagina)
        {
            case "principal": return new[] { "Jogador", "Armas", "Veiculos", "Roupas", "Policia", "Teletransporte", "Mundo", "Pedestres", "Dinheiro", "Desligar efeitos ativos" };
            case "dinheiro": return new[] { "Definir saldo", "Adicionar valor", "Adicionar $100.000", "Adicionar $1.000.000" };
            case "jogador": return new[] { "Restaurar vida e colete", "Invencibilidade: " + Estado(jogadorInvencivel), "Recuperar habilidade especial", "Limpar sangue e sujeira", "Habilidades de movimento", "Animacoes", "Receber paraquedas" };
            case "animacoes": return new[] { "Flexoes", "Acenar", "Parar animacao" };
            case "habilidades": return new[] { "Superpulo: " + Estado(superPulo), "Corrida rapida: " + Estado(corridaRapida), "Natacao rapida: " + Estado(natacaoRapida), "Firme na moto: " + Estado(mundoExtra.FirmeNaMoto) };
            case "veiculos": return new[] { "Escolher veiculo", "Tunagem e reparos", "Portas, vidros e motor", "Garagem: salvar e carregar" };
            case "garagem": return new[] { "Salvar veiculo: espaco 1", "Salvar veiculo: espaco 2", "Salvar veiculo: espaco 3", "Criar salvo: espaco 1", "Criar salvo: espaco 2", "Criar salvo: espaco 3" };
            case "controles_veiculo": return new[] { "Abrir portas", "Fechar portas", "Abaixar vidros", "Subir vidros", "Ligar motor", "Desligar motor", "Velocimetro: " + Estado(extras.Velocimetro), "Impulso", "Frear" };
            case "policia": return new[] { "Limpar procurado", "Nivel 1", "Nivel 2", "Nivel 3", "Nivel 4", "Nivel 5", "Nunca procurado: " + (nuncaProcurado ? "LIGADO" : "DESLIGADO") };
            case "pedestres": return new[] { "Atrair pedestres proximos", "Chamar guarda-costas", "Dispensar guarda-costas", "Chamar motorista ao marcador", "Dispensar motorista" };
            case "armas": return new[] { "Receber todas as armas disponiveis", "Completar municao", "Remover armas", "Escolher arma", "Municao infinita: " + Estado(municaoInfinita), "Sem recarga: " + Estado(semRecarga), "Equipar acessorios compativeis", "Tiros explosivos: " + Estado(tirosExplosivos), "Tiros incendiarios: " + Estado(tirosIncendiarios), "Kits de armas" };
            case "kits_armas": return new[] { "Salvar kit: espaco 1", "Salvar kit: espaco 2", "Salvar kit: espaco 3", "Aplicar kit: espaco 1", "Aplicar kit: espaco 2", "Aplicar kit: espaco 3" };
            case "lista_armas": return armasNomes;
            case "roupas": { var itens = new string[nomesRoupa.Length + 1]; nomesRoupa.CopyTo(itens, 0); itens[nomesRoupa.Length] = "Trajes salvos"; return itens; }
            case "trajes_salvos": return new[] { "Salvar traje: espaco 1", "Salvar traje: espaco 2", "Salvar traje: espaco 3", "Aplicar traje: espaco 1", "Aplicar traje: espaco 2", "Aplicar traje: espaco 3" };
            case "roupa_peca": return new[] { "Proximo modelo", "Proxima textura", "Restaurar peca anterior" };
            case "categorias": { var a = new string[categorias.Length + 3]; categorias.CopyTo(a, 0); a[categorias.Length] = "Buscar por nome exibido"; a[categorias.Length + 1] = "Veiculos favoritos"; a[categorias.Length + 2] = "Criar por nome interno"; return a; }
            case "modelos": return nomesModelos;
            case "tunagem": return new[] { "Reparar e limpar", "Melhorar desempenho ao maximo", "Turbo ligado", "Turbo desligado", "Cor: preto", "Cor: branco", "Cor: vermelho", "Cor: azul", "Cor: dourado", "Invencibilidade do veiculo", "Personalizar pecas", "Desvirar veiculo", "Ligar motor" };
            case "pecas": return new[] { "Aerofolio", "Para-choque dianteiro", "Para-choque traseiro", "Saias", "Escapamento", "Gaiola", "Grade", "Capo", "Paralama esquerdo", "Paralama direito", "Teto", "Buzina", "Suspensao", "Rodas" };
            case "teletransporte": return new[] { "Marcador do mapa", "Aeroporto de Los Santos", "Praia de Vespucci", "Observatorio Galileo", "Pier de Del Perro", "Monte Chiliad", "Aerodromo de Sandy Shores", "Locais salvos" };
            case "locais_salvos": return new[] { "Salvar local: espaco 1", "Salvar local: espaco 2", "Salvar local: espaco 3", "Ir ao local: espaco 1", "Ir ao local: espaco 2", "Ir ao local: espaco 3" };
            case "mundo": return new[] { "Manha: 08:00", "Meio-dia: 12:00", "Noite: 22:00", "Ensolarado", "Limpo", "Nublado", "Chuva", "Tempestade", "Relogio parado: " + Estado(relogioParado), "Blecaute: " + Estado(blecaute), "Radio portatil: " + Estado(extras.RadioPortatil), "Transito: " + MundoExtraMikeMenu.NomeNivel(mundoExtra.Transito), "Pedestres: " + MundoExtraMikeMenu.NomeNivel(mundoExtra.Pedestres) };
            default: return new string[0];
        }
    }
    private void AbrirPagina(string alvo) { LiberarPrevia(); pagina = alvo; indiceSelecionado = 0; }
    private void Voltar() { if (pagina == "principal") { aberto = false; LiberarPrevia(); } else if (pagina == "modelos") AbrirPagina("categorias"); else if (pagina == "pecas") AbrirPagina("tunagem"); else if (pagina == "habilidades" || pagina == "animacoes") AbrirPagina("jogador"); else if (pagina == "categorias" || pagina == "tunagem" || pagina == "controles_veiculo" || pagina == "garagem") AbrirPagina("veiculos"); else if (pagina == "locais_salvos") AbrirPagina("teletransporte"); else if (pagina == "lista_armas" || pagina == "kits_armas") AbrirPagina("armas"); else if (pagina == "roupa_peca" || pagina == "trajes_salvos") AbrirPagina("roupas"); else AbrirPagina("principal"); }
    private void AoPressionarTecla(object remetente, KeyEventArgs e)
    {
        if (ocupado) return;
        int antes;
        if (e.KeyCode == Keys.Delete) { if (aberto) { aberto = false; LiberarPrevia(); return; } if (EstaPronto(out antes)) { aberto = true; AbrirPagina("principal"); } else ExibirMensagem("Menu disponivel apenas no modo historia, com um protagonista ativo."); return; }
        if (e.KeyCode == Keys.F10 || e.KeyCode == Keys.F11) { AlterarDinheiro(e.KeyCode == Keys.F11); return; }
        int acaoAtalho = !aberto ? atalhos.Identificar(e.KeyData) : -1;
        if (acaoAtalho >= 0)
        {
            if (!EstaPronto(out antes)) { ExibirMensagem("Atalho disponivel apenas no modo historia."); return; }
            try
            {
                if (acaoAtalho == 0) { Game.Player.Character.Health = Game.Player.Character.MaxHealth; Game.Player.Character.Armor = 100; ExibirMensagem("Vida e colete restaurados."); }
                else if (acaoAtalho == 1) { Vehicle v = Game.Player.Character.CurrentVehicle; if (v == null || !v.Exists()) ExibirMensagem("Entre em um veiculo primeiro."); else { v.Repair(); Function.Call(Hash.SET_VEHICLE_DIRT_LEVEL, v.Handle, 0f); ExibirMensagem("Veiculo reparado e limpo."); } }
                else AlternarNuncaProcurado();
            }
            catch (Exception) { ExibirMensagem("Atalho indisponivel neste momento."); }
            return;
        }
        if (!aberto) return;
        if (!EstaPronto(out antes)) { aberto = false; LimparProtecoes(); return; }
        string[] itens = ObterItens();
        if (e.KeyCode == Keys.Up) indiceSelecionado = RegrasMikeMenu.MoverSelecao(indiceSelecionado, -1, itens.Length);
        else if (e.KeyCode == Keys.Down) indiceSelecionado = RegrasMikeMenu.MoverSelecao(indiceSelecionado, 1, itens.Length);
        else if (e.KeyCode == Keys.F5 && pagina == "modelos" && itens.Length > 0) { try { AlternarFavorito(); } catch (Exception) { ExibirMensagem("Nao foi possivel alterar os favoritos."); } }
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
        if (pagina == "principal") { if (indiceSelecionado == 9) { LimparProtecoes(); ExibirMensagem("Efeitos ativos do menu desligados."); return; } string alvo = RegrasMikeMenu.PaginaPrincipal(indiceSelecionado); if (alvo != null) AbrirPagina(alvo); return; }
        if (pagina == "veiculos") { string[] paginas = { "categorias", "tunagem", "controles_veiculo", "garagem" }; AbrirPagina(paginas[indiceSelecionado]); return; }
        if (pagina == "garagem") { string mensagem; int slot = indiceSelecionado % 3 + 1; if (indiceSelecionado < 3) FavoritosMikeMenu.SalvarVeiculo(personagem.CurrentVehicle, slot, out mensagem); else FavoritosMikeMenu.CriarVeiculo(personagem, slot, out mensagem); ExibirMensagem(mensagem); return; }
        if (pagina == "trajes_salvos") { string mensagem; int slot = indiceSelecionado % 3 + 1; if (indiceSelecionado < 3) FavoritosMikeMenu.SalvarTraje(personagem, slot, out mensagem); else FavoritosMikeMenu.AplicarTraje(personagem, slot, out mensagem); ExibirMensagem(mensagem); return; }
        if (pagina == "kits_armas") { string mensagem; int slot = indiceSelecionado % 3 + 1; if (indiceSelecionado < 3) KitArmasMikeMenu.Salvar(personagem, slot, out mensagem); else KitArmasMikeMenu.Aplicar(personagem, slot, out mensagem); ExibirMensagem(mensagem); return; }
        if (pagina == "controles_veiculo") { if (indiceSelecionado == 6) extras.AlternarVelocimetro(); else if (indiceSelecionado == 7) ExibirMensagem(extras.Impulso(personagem)); else if (indiceSelecionado == 8) ExibirMensagem(extras.Frear(personagem)); else ControlarVeiculo(personagem.CurrentVehicle); return; }
        if (pagina == "dinheiro") { if (indiceSelecionado < 2) { ocupado = false; AlterarDinheiro(indiceSelecionado == 1); ocupado = true; } else { int total; string quantia = indiceSelecionado == 2 ? "100000" : "1000000"; if (RegrasMikeMenu.TentarCalcular(quantia, Game.Player.Money, true, out total)) { Game.Player.Money = total; ExibirMensagem("Saldo atualizado. Salve o jogo."); } else ExibirMensagem("Limite de dinheiro atingido."); } return; }
        if (pagina == "habilidades") { if (indiceSelecionado == 0) superPulo = !superPulo; else if (indiceSelecionado == 1) { corridaRapida = !corridaRapida; if (!corridaRapida) Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, Game.Player.Handle, 1f); } else if (indiceSelecionado == 2) { natacaoRapida = !natacaoRapida; if (!natacaoRapida) Function.Call(Hash.SET_SWIM_MULTIPLIER_FOR_PLAYER, Game.Player.Handle, 1f); } else mundoExtra.AlternarFirmeNaMoto(); return; }
        if (pagina == "animacoes") { if (indiceSelecionado == 2) PararAnimacao(); else IniciarAnimacao(personagem, indiceSelecionado); return; }
        if (pagina == "jogador") { if (indiceSelecionado == 5) { AbrirPagina("animacoes"); return; } if (indiceSelecionado == 6) { int paraquedas = Game.GenerateHash("GADGET_PARACHUTE"); if (Function.Call<bool>(Hash.IS_WEAPON_VALID, paraquedas)) { Function.Call(Hash.GIVE_WEAPON_TO_PED, personagem.Handle, paraquedas, 1, false, false); ExibirMensagem("Paraquedas solicitado."); } else ExibirMensagem("Paraquedas indisponivel."); return; } if (indiceSelecionado == 4) { AbrirPagina("habilidades"); return; } if (indiceSelecionado == 0) { personagem.Health = personagem.MaxHealth; personagem.Armor = 100; } else if (indiceSelecionado == 1) { jogadorInvencivel = !jogadorInvencivel; if (jogadorInvencivel) { AplicarProtecaoJogador(); ExibirMensagem("Invencibilidade ligada."); } else { Function.Call(Hash.SET_PLAYER_INVINCIBLE, Game.Player.Handle, false); if (personagemProtegido != null && personagemProtegido.Exists()) personagemProtegido.IsInvincible = false; personagemProtegido = null; ExibirMensagem("Invencibilidade desligada."); } } else if (indiceSelecionado == 2) Function.Call(Hash.SPECIAL_ABILITY_FILL_METER, Game.Player.Handle, true); else { Function.Call(Hash.CLEAR_PED_BLOOD_DAMAGE, personagem.Handle); Function.Call(Hash.CLEAR_PED_ENV_DIRT, personagem.Handle); ExibirMensagem("Personagem limpo."); } return; }
        if (pagina == "policia") { if (indiceSelecionado == 6) AlternarNuncaProcurado(); else { if (indiceSelecionado > 0 && nuncaProcurado) { nuncaProcurado = false; ExibirMensagem("Nunca procurado desligado."); } Game.Player.WantedLevel = indiceSelecionado; } return; }
        if (pagina == "pedestres") { if (indiceSelecionado == 0) AtrairPedestres(personagem); else if (indiceSelecionado == 1) ExibirMensagem(companheiros.ChamarGuarda(personagem)); else if (indiceSelecionado == 2) ExibirMensagem(companheiros.DispensarGuardas()); else if (indiceSelecionado == 3) ExibirMensagem(companheiros.ChamarMotorista(personagem)); else ExibirMensagem(companheiros.DispensarMotorista()); return; }
        if (pagina == "armas") { if (indiceSelecionado == 9) { AbrirPagina("kits_armas"); return; } if (indiceSelecionado == 7) { tirosExplosivos = !tirosExplosivos; if (tirosExplosivos) tirosIncendiarios = false; return; } if (indiceSelecionado == 8) { tirosIncendiarios = !tirosIncendiarios; if (tirosIncendiarios) tirosExplosivos = false; return; } if (indiceSelecionado == 4) { municaoInfinita = !municaoInfinita; if (!municaoInfinita) DesativarMunicao(); return; } if (indiceSelecionado == 5) { semRecarga = !semRecarga; if (!semRecarga) DesativarSemRecarga(); return; } if (indiceSelecionado == 6) { EquiparAcessorios(personagem); return; } if (indiceSelecionado == 3) { AbrirPagina("lista_armas"); return; } if (indiceSelecionado == 2) Function.Call(Hash.REMOVE_ALL_PED_WEAPONS, personagem.Handle, true); else if (indiceSelecionado == 0) { int recebidas = 0; var vistos = new System.Collections.Generic.HashSet<int>(); foreach (WeaponHash arma in Enum.GetValues(typeof(WeaponHash))) { int codigo = unchecked((int)arma); if (arma == WeaponHash.Unarmed || !vistos.Add(codigo) || !Function.Call<bool>(Hash.IS_WEAPON_VALID, codigo)) continue; Function.Call(Hash.GIVE_WEAPON_TO_PED, personagem.Handle, codigo, 999, false, false); if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, personagem.Handle, codigo, false)) recebidas++; } ExibirMensagem(recebidas + " armas aceitas pelo personagem."); } if (indiceSelecionado == 1) { foreach (WeaponHash w in Enum.GetValues(typeof(WeaponHash))) if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, personagem.Handle, (int)w, false)) Function.Call(Hash.SET_PED_AMMO, personagem.Handle, (int)w, 999); } return; }
        if (pagina == "lista_armas") { int arma = Game.GenerateHash(armasCodigos[indiceSelecionado]); if (!Function.Call<bool>(Hash.IS_WEAPON_VALID, arma)) ExibirMensagem("Arma indisponivel nesta versao."); else { Function.Call(Hash.GIVE_WEAPON_TO_PED, personagem.Handle, arma, 250, false, true); ExibirMensagem("Arma solicitada."); } return; }
        if (pagina == "roupas") { if (indiceSelecionado == nomesRoupa.Length) { AbrirPagina("trajes_salvos"); return; } componenteRoupa = componentesRoupa[indiceSelecionado]; pedRoupa = personagem.Handle; roupaOriginal = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, pedRoupa, componenteRoupa); texturaOriginal = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, pedRoupa, componenteRoupa); AbrirPagina("roupa_peca"); return; }
        if (pagina == "roupa_peca") { AlterarRoupa(personagem); return; }
        if (pagina == "categorias") { if (indiceSelecionado == categorias.Length + 2) { string nome = Game.GetUserInput(""); int depois; if (!string.IsNullOrWhiteSpace(nome) && EstaPronto(out depois) && antes == depois) CriarVeiculo(new Model(nome.Trim())); } else if (indiceSelecionado == categorias.Length) { string busca = Game.GetUserInput(""); int depois; if (!string.IsNullOrWhiteSpace(busca) && EstaPronto(out depois) && antes == depois) { CarregarBusca(busca); origemModelos = "busca"; tituloModelos = "RESULTADOS DA BUSCA"; AbrirPagina("modelos"); if (modelos.Length == 0) ExibirMensagem("Nenhum veiculo encontrado."); } } else if (indiceSelecionado == categorias.Length + 1) { CarregarFavoritos(); origemModelos = "favoritos"; tituloModelos = "VEICULOS FAVORITOS"; AbrirPagina("modelos"); if (modelos.Length == 0) ExibirMensagem("Nenhum favorito salvo. Use F5 sobre um modelo."); } else { categoria = indiceSelecionado; CarregarModelos(); origemModelos = "categorias"; tituloModelos = categorias[categoria].ToUpperInvariant(); AbrirPagina("modelos"); if (modelos.Length == 0) ExibirMensagem("Nenhum modelo disponivel nesta categoria."); } return; }
        if (pagina == "modelos") { CriarVeiculo(new Model((int)modelos[indiceSelecionado])); return; }
        if (pagina == "tunagem") { if (indiceSelecionado == 10) AbrirPagina("pecas"); else TunarVeiculo(personagem.CurrentVehicle); return; }
        if (pagina == "pecas") { Personalizar(personagem.CurrentVehicle); return; }
        if (pagina == "teletransporte") { if (indiceSelecionado == 7) AbrirPagina("locais_salvos"); else Teletransportar(antes, null); return; }
        if (pagina == "locais_salvos") { int slot = indiceSelecionado % 3 + 1; if (indiceSelecionado < 3) SalvarLocal(personagem, slot); else IrAoLocal(antes, slot); return; }
        if (pagina == "mundo") { if (indiceSelecionado == 11) { mundoExtra.AlternarTransito(); return; } if (indiceSelecionado == 12) { mundoExtra.AlternarPedestres(); return; } if (indiceSelecionado == 10) { extras.AlternarRadio(); return; } if (indiceSelecionado == 8) { relogioParado = !relogioParado; Function.Call(Hash.PAUSE_CLOCK, relogioParado); return; } if (indiceSelecionado == 9) { blecaute = !blecaute; Function.Call(Hash.SET_ARTIFICIAL_LIGHTS_STATE, blecaute); return; } if (indiceSelecionado < 3) Function.Call(Hash.SET_CLOCK_TIME, indiceSelecionado == 0 ? 8 : indiceSelecionado == 1 ? 12 : 22, 0, 0); else { string[] weather = { "EXTRASUNNY", "CLEAR", "CLOUDS", "RAIN", "THUNDER" }; Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, weather[indiceSelecionado - 3]); } }
    }
    private void AlterarRoupa(Ped personagem)
    {
        if (personagem == null || !personagem.Exists() || personagem.Handle != pedRoupa) { ExibirMensagem("Personagem mudou; selecione a peca novamente."); AbrirPagina("roupas"); return; }
        int modelo = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, pedRoupa, componenteRoupa);
        int textura = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, pedRoupa, componenteRoupa);
        if (indiceSelecionado == 0)
        {
            int quantidade = Function.Call<int>(Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, pedRoupa, componenteRoupa);
            bool encontrada = false;
            for (int i = 0; i < quantidade; i++)
            {
                modelo = RegrasMikeMenu.ProximaVariacao(modelo, quantidade);
                int texturas = Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, pedRoupa, componenteRoupa, modelo);
                for (int t = 0; t < texturas; t++)
                    if (Function.Call<bool>(Hash.IS_PED_COMPONENT_VARIATION_VALID, pedRoupa, componenteRoupa, modelo, t)) { textura = t; encontrada = true; break; }
                if (encontrada) break;
            }
            if (!encontrada) { ExibirMensagem("Nenhum modelo valido para este personagem."); return; }
        }
        else if (indiceSelecionado == 1)
        {
            int quantidade = Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, pedRoupa, componenteRoupa, modelo);
            bool encontrada = false;
            for (int i = 0; i < quantidade; i++)
            {
                textura = RegrasMikeMenu.ProximaVariacao(textura, quantidade);
                if (Function.Call<bool>(Hash.IS_PED_COMPONENT_VARIATION_VALID, pedRoupa, componenteRoupa, modelo, textura)) { encontrada = true; break; }
            }
            if (!encontrada) { ExibirMensagem("Nenhuma textura valida para este modelo."); return; }
        }
        else { modelo = roupaOriginal; textura = texturaOriginal; }
        if (modelo < 0 || textura < 0 || !Function.Call<bool>(Hash.IS_PED_COMPONENT_VARIATION_VALID, pedRoupa, componenteRoupa, modelo, textura)) { ExibirMensagem("Variacao indisponivel para este personagem."); return; }
        Function.Call(Hash.SET_PED_COMPONENT_VARIATION, pedRoupa, componenteRoupa, modelo, textura, 0);
        ExibirMensagem("Modelo " + modelo + " / textura " + textura + ". Visualize no personagem.");
    }
    private string Estado(bool ligado) { return ligado ? "LIGADO" : "DESLIGADO"; }
    private void DesativarMunicao()
    {
        if (pedMunicao != 0 && Function.Call<bool>(Hash.DOES_ENTITY_EXIST, pedMunicao)) Function.Call(Hash.SET_PED_INFINITE_AMMO, pedMunicao, false, armaMunicao);
        pedMunicao = 0; armaMunicao = 0;
    }
    private void DesativarSemRecarga()
    {
        if (pedSemRecarga != 0 && Function.Call<bool>(Hash.DOES_ENTITY_EXIST, pedSemRecarga)) Function.Call(Hash.SET_PED_INFINITE_AMMO_CLIP, pedSemRecarga, false);
        pedSemRecarga = 0;
    }
    private void LimparEfeitosAdicionais()
    {
        superPulo = false; tirosExplosivos = false; tirosIncendiarios = false; PararAnimacao();
        if (corridaRapida) Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, Game.Player.Handle, 1f);
        if (natacaoRapida) Function.Call(Hash.SET_SWIM_MULTIPLIER_FOR_PLAYER, Game.Player.Handle, 1f);
        corridaRapida = false; natacaoRapida = false;
        DesativarMunicao(); municaoInfinita = false;
        DesativarSemRecarga(); semRecarga = false;
        if (relogioParado) Function.Call(Hash.PAUSE_CLOCK, false);
        if (blecaute) Function.Call(Hash.SET_ARTIFICIAL_LIGHTS_STATE, false);
        relogioParado = false; blecaute = false;
    }
    private void AtualizarEfeitos()
    {
        int jogador = Game.Player.Handle;
        if (superPulo) Function.Call(Hash.SET_SUPER_JUMP_THIS_FRAME, jogador);
        if (tirosExplosivos) Function.Call(Hash.SET_EXPLOSIVE_AMMO_THIS_FRAME, jogador);
        if (tirosIncendiarios) Function.Call(Hash.SET_FIRE_AMMO_THIS_FRAME, jogador);
        if (corridaRapida) Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, jogador, 1.49f);
        if (natacaoRapida) Function.Call(Hash.SET_SWIM_MULTIPLIER_FOR_PLAYER, jogador, 1.49f);
        Ped p = Game.Player.Character;
        if (p == null || !p.Exists()) return;
        if (semRecarga && pedSemRecarga != p.Handle) { DesativarSemRecarga(); pedSemRecarga = p.Handle; Function.Call(Hash.SET_PED_INFINITE_AMMO_CLIP, pedSemRecarga, true); }
        if (municaoInfinita)
        {
            int arma = Function.Call<int>(Hash.GET_SELECTED_PED_WEAPON, p.Handle);
            if (pedMunicao != p.Handle || armaMunicao != arma) { DesativarMunicao(); pedMunicao = p.Handle; armaMunicao = arma; Function.Call(Hash.SET_PED_INFINITE_AMMO, pedMunicao, true, armaMunicao); }
        }
    }
    private void PararAnimacao()
    {
        if (pedAnimacao != 0 && dicionarioAnimacao != null && Function.Call<bool>(Hash.DOES_ENTITY_EXIST, pedAnimacao))
            Function.Call(Hash.STOP_ANIM_TASK, pedAnimacao, dicionarioAnimacao, nomeAnimacao, 1f);
        if (dicionarioAnimacao != null) Function.Call(Hash.REMOVE_ANIM_DICT, dicionarioAnimacao);
        pedAnimacao = 0; dicionarioAnimacao = null; nomeAnimacao = null;
    }
    private void IniciarAnimacao(Ped personagem, int opcao)
    {
        if (personagem == null || !personagem.Exists() || personagem.IsDead || personagem.IsInVehicle()) { ExibirMensagem("Saia do veiculo para usar animacoes."); return; }
        string dicionario = opcao == 0 ? "amb@world_human_push_ups@male@base" : "anim@mp_player_intcelebrationmale@wave";
        string animacao = opcao == 0 ? "base" : "wave";
        int antes = personagem.Handle;
        PararAnimacao();
        Function.Call(Hash.REQUEST_ANIM_DICT, dicionario);
        bool carregou = false;
        try
        {
            for (int i = 0; i < 24; i++)
            {
                if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dicionario)) { carregou = true; break; }
                Script.Wait(50);
                int modelo;
                if (!EstaPronto(out modelo) || Game.Player.Character == null || Game.Player.Character.Handle != antes) break;
            }
            int atual;
            if (!carregou || !EstaPronto(out atual) || Game.Player.Character == null || Game.Player.Character.Handle != antes || !personagem.Exists() || personagem.IsDead || personagem.IsInVehicle()) { ExibirMensagem("Animacao indisponivel ou cancelada."); return; }
            pedAnimacao = antes; dicionarioAnimacao = dicionario; nomeAnimacao = animacao;
            Function.Call(Hash.TASK_PLAY_ANIM, antes, dicionario, animacao, 8f, -8f, opcao == 0 ? -1 : 3000, opcao == 0 ? 1 : 0, 0f, false, false, false);
            ExibirMensagem("Animacao iniciada; selecione Parar animacao para encerrar.");
        }
        finally { if (dicionarioAnimacao != dicionario) Function.Call(Hash.REMOVE_ANIM_DICT, dicionario); }
    }
    private void EquiparAcessorios(Ped personagem)
    {
        int arma = Function.Call<int>(Hash.GET_SELECTED_PED_WEAPON, personagem.Handle);
        int aplicados = 0;
        foreach (WeaponComponentHash componente in Enum.GetValues(typeof(WeaponComponentHash)))
        {
            int codigo = unchecked((int)componente);
            if (!Function.Call<bool>(Hash.DOES_WEAPON_TAKE_WEAPON_COMPONENT, arma, codigo)) continue;
            Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, personagem.Handle, arma, codigo);
            aplicados++;
        }
        ExibirMensagem(aplicados == 0 ? "Nenhum acessorio compativel encontrado." : "Acessorios compativeis aplicados: " + aplicados + ".");
    }
    private void ControlarVeiculo(Vehicle veiculo)
    {
        if (veiculo == null || !veiculo.Exists()) { ExibirMensagem("Entre em um veiculo primeiro."); return; }
        int h = veiculo.Handle;
        if (indiceSelecionado == 0) for (int i = 0; i < 6; i++) Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, h, i, false, false);
        else if (indiceSelecionado == 1) Function.Call(Hash.SET_VEHICLE_DOORS_SHUT, h, false);
        else if (indiceSelecionado == 2) for (int i = 0; i < 4; i++) Function.Call(Hash.ROLL_DOWN_WINDOW, h, i);
        else if (indiceSelecionado == 3) for (int i = 0; i < 4; i++) Function.Call(Hash.ROLL_UP_WINDOW, h, i);
        else Function.Call(Hash.SET_VEHICLE_ENGINE_ON, h, indiceSelecionado == 4, true, true);
    }
    private void AlternarNuncaProcurado()
    {
        nuncaProcurado = !nuncaProcurado;
        if (nuncaProcurado) Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL, Game.Player.Handle);
        ExibirMensagem("Nunca procurado " + (nuncaProcurado ? "ligado." : "desligado."));
    }
    private void CarregarModelos()
    {
        VehicleHash[] disponiveis = Vehicle.GetAllModelsOfClass((VehicleClass)categoria);
        int[] codigos = new int[disponiveis.Length];
        for (int i = 0; i < disponiveis.Length; i++) codigos[i] = (int)disponiveis[i];
        DefinirModelos(RegrasMikeMenu.RemoverDuplicados(codigos));
    }
    private string NomeModelo(int codigo)
    {
        string rotulo = Function.Call<string>(Hash.GET_DISPLAY_NAME_FROM_VEHICLE_MODEL, codigo);
        string nome = string.IsNullOrEmpty(rotulo) ? null : Game.GetLocalizedString(rotulo);
        return string.IsNullOrEmpty(nome) || nome == "NULL" ? (string.IsNullOrEmpty(rotulo) ? ((VehicleHash)codigo).ToString() : rotulo) : nome;
    }
    private void DefinirModelos(int[] codigos)
    {
        modelos = new VehicleHash[codigos.Length]; nomesModelos = new string[codigos.Length];
        var contagem = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < codigos.Length; i++)
        {
            modelos[i] = (VehicleHash)codigos[i];
            string nome = NomeModelo(codigos[i]);
            nomesModelos[i] = nome;
            int quantidade; contagem.TryGetValue(nome, out quantidade); contagem[nome] = quantidade + 1;
        }
        for (int i = 0; i < codigos.Length; i++) if (contagem[nomesModelos[i]] > 1) nomesModelos[i] += " [" + unchecked((uint)codigos[i]).ToString("X8") + "]";
    }
    private void CarregarBusca(string busca)
    {
        var todos = new System.Collections.Generic.HashSet<int>();
        var encontrados = new System.Collections.Generic.List<int>();
        for (int classe = 0; classe < categorias.Length; classe++)
            foreach (VehicleHash modelo in Vehicle.GetAllModelsOfClass((VehicleClass)classe))
            {
                int codigo = (int)modelo;
                if (todos.Add(codigo) && RegrasMikeMenu.CorrespondeBusca(NomeModelo(codigo), busca)) encontrados.Add(codigo);
            }
        DefinirModelos(encontrados.ToArray());
    }
    private string CaminhoDados(string arquivo) { return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "MikeMenu", arquivo); }
    private System.Collections.Generic.List<int> LerFavoritos()
    {
        string caminho = CaminhoDados("favoritos-veiculos.xml");
        if (!System.IO.File.Exists(caminho)) return new System.Collections.Generic.List<int>();
        ModelosFavoritosMikeMenu dados = FavoritosMikeMenu.Ler<ModelosFavoritosMikeMenu>(caminho);
        if (dados == null || dados.Versao != 1 || dados.Modelos == null || dados.Modelos.Length > 2048) throw new System.IO.InvalidDataException("Favoritos invalidos.");
        return new System.Collections.Generic.List<int>(RegrasMikeMenu.RemoverDuplicados(dados.Modelos));
    }
    private void CarregarFavoritos() { DefinirModelos(LerFavoritos().ToArray()); }
    private void AlternarFavorito()
    {
        int codigo = (int)modelos[indiceSelecionado];
        var lista = LerFavoritos();
        bool removido = lista.Remove(codigo);
        if (!removido) { if (lista.Count >= 2048) { ExibirMensagem("Limite de favoritos atingido."); return; } lista.Add(codigo); }
        FavoritosMikeMenu.Gravar(CaminhoDados("favoritos-veiculos.xml"), new ModelosFavoritosMikeMenu { Versao = 1, Modelos = lista.ToArray() });
        if (origemModelos == "favoritos") { CarregarFavoritos(); indiceSelecionado = Math.Min(indiceSelecionado, Math.Max(0, modelos.Length - 1)); }
        ExibirMensagem(removido ? "Veiculo removido dos favoritos." : "Veiculo adicionado aos favoritos.");
    }
    private void SalvarLocal(Ped personagem, int slot)
    {
        if (personagem == null || !personagem.Exists() || personagem.IsDead) { ExibirMensagem("Personagem indisponivel."); return; }
        GTA.Math.Vector3 pos = personagem.Position;
        if (!RegrasMikeMenu.DestinoValido(pos.X, pos.Y, pos.Z)) { ExibirMensagem("Local invalido."); return; }
        FavoritosMikeMenu.Gravar(CaminhoDados("local-" + slot + ".xml"), new LocalMikeMenu { Versao = 1, X = pos.X, Y = pos.Y, Z = pos.Z });
        ExibirMensagem("Local salvo no espaco " + slot + ".");
    }
    private void IrAoLocal(int antes, int slot)
    {
        try
        {
            LocalMikeMenu local = FavoritosMikeMenu.Ler<LocalMikeMenu>(CaminhoDados("local-" + slot + ".xml"));
            if (local == null || local.Versao != 1 || !RegrasMikeMenu.DestinoValido(local.X, local.Y, local.Z)) { ExibirMensagem("Local salvo invalido."); return; }
            Teletransportar(antes, new GTA.Math.Vector3(local.X, local.Y, local.Z));
        }
        catch (Exception) { ExibirMensagem("Local salvo ausente ou invalido."); }
    }
    private void AtrairPedestres(Ped jogador)
    {
        int quantidade = 0;
        foreach (Ped ped in World.GetNearbyPeds(jogador, 25f))
        {
            if (ped == null || !ped.Exists() || ped.Handle == jogador.Handle || ped.IsDead || ped.IsInVehicle()) continue;
            int h = ped.Handle;
            int tipo = Function.Call<int>(Hash.GET_PED_TYPE, h);
            if ((tipo != 4 && tipo != 5) || Function.Call<bool>(Hash.IS_PED_A_PLAYER, h) || Function.Call<bool>(Hash.IS_ENTITY_A_MISSION_ENTITY, h) || Function.Call<bool>(Hash.IS_PED_IN_COMBAT, h, 0) || Function.Call<bool>(Hash.IS_PED_FLEEING, h)) continue;
            Function.Call(Hash.TASK_GO_TO_ENTITY, h, jogador.Handle, 10000, 2.5f, 1f, 1073741824, 0);
            if (++quantidade >= 10) break;
        }
        ExibirMensagem(quantidade == 0 ? "Nenhum pedestre disponivel por perto." : quantidade + " pedestres convidados a se aproximar.");
    }
    private void Teletransportar(int antes, GTA.Math.Vector3? salvo)
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
        GTA.Math.Vector3 destino = salvo.HasValue ? salvo.Value : destinos[indiceSelecionado];
        bool destinoFixo = salvo.HasValue || indiceSelecionado != 0;
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
        else if (indiceSelecionado == 11) { Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, h, 5.0f); ExibirMensagem("Tentativa de posicionar o veiculo no solo."); }
        else if (indiceSelecionado == 12) { Function.Call(Hash.SET_VEHICLE_ENGINE_ON, h, true, true, false); ExibirMensagem("Motor ligado."); }
        else { if (veiculoProtegido != null && veiculoProtegido.Exists()) { bool mesmo = veiculoProtegido.Handle == h; veiculoProtegido.IsInvincible = false; veiculoProtegido = null; if (mesmo) { ExibirMensagem("Invencibilidade do veiculo desligada."); return; } } veiculoProtegido = veiculo; veiculo.IsInvincible = true; ExibirMensagem("Invencibilidade do veiculo ligada."); }
    }
}
#endif
