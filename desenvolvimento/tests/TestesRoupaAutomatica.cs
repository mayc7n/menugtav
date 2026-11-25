using System;
class TestesRoupaAutomatica
{
    static void Verificar(bool valor, string caso) { if (!valor) throw new Exception(caso); }
    static void Main()
    {
        int michael = unchecked((int)0x0D7114C9u), franklin = unchecked((int)0x9B22DBAFu);
        var estado = new EstadoRoupaAutomatica();
        Verificar(!estado.Observar(michael, false, true, 100), "nao aplica na entrada normal");
        Verificar(!estado.Observar(michael, true, false, 200), "detecta morte sem aplicar");
        Verificar(!estado.Observar(franklin, false, true, 300), "outro protagonista nao recebe traje");
        Verificar(!estado.Observar(michael, false, true, 500), "espera inicio do respawn");
        Verificar(!estado.Observar(michael, false, false, 2000), "interrompe espera durante carregamento");
        Verificar(!estado.Observar(michael, false, true, 2500), "reinicia estabilizacao");
        Verificar(!estado.Observar(michael, false, true, 5499), "aguarda intervalo completo");
        Verificar(estado.Observar(michael, false, true, 5500), "solicita restauracao");
        Verificar(!estado.Observar(michael, false, true, 9000), "nao repete restauracao");
        Verificar(!estado.Observar(7, true, false, 10000), "ignora personagens nao suportados");
        Console.WriteLine("Testes de roupa automatica passaram.");
    }
}
