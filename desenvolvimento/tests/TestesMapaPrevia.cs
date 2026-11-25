using System;

class TestesMapaPrevia
{
    static void Verificar(bool condicao, string mensagem) { if (!condicao) throw new Exception(mensagem); }
    static void Main()
    {
        Verificar(MapaPreviaMikeMenu.ChaveVeiculo(unchecked((int)0xB779A091)) == "veiculo_B779A091", "hash negativo do veiculo");
        Verificar(MapaPreviaMikeMenu.ChaveVeiculo(0) == null, "modelo ausente");
        Verificar(MapaPreviaMikeMenu.ChaveRoupa(unchecked((int)0x9B22DBAF), 11, 4, 2) == "roupa_9B22DBAF_11_4_2", "chave inclui personagem e variacoes");
        Verificar(MapaPreviaMikeMenu.ChaveRoupa(unchecked((int)0x0D7114C9), 11, 4, 2) == "roupa_0D7114C9_11_4_2", "Michael separado");
        Verificar(MapaPreviaMikeMenu.ChaveRoupa(unchecked((int)0x9B810FA2), 11, 4, 2) == "roupa_9B810FA2_11_4_2", "Trevor separado");
        Verificar(MapaPreviaMikeMenu.ChaveRoupa(1, 0, 0, 0) == null && MapaPreviaMikeMenu.ChaveRoupa(unchecked((int)0x0D7114C9), 12, 0, 0) == null && MapaPreviaMikeMenu.ChaveRoupa(unchecked((int)0x0D7114C9), 0, -1, 0) == null, "personagem e indices invalidos");
        string dicionario, textura;
        Verificar(MapaPreviaMikeMenu.TentarPreviaNativa("ADDER", out dicionario, out textura) && dicionario == "lgm_default" && textura == "adder", "previa interna conhecida");
        Verificar(!MapaPreviaMikeMenu.TentarPreviaNativa("desconhecido", out dicionario, out textura) && dicionario == null && textura == null, "sem correspondencia");
        Console.WriteLine("Testes de mapa de previas passaram.");
    }
}
