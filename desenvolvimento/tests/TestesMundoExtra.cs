using System;
class TestesMundoExtra
{
    static void Main()
    {
        if (MundoExtraMikeMenu.ProximoNivel(0) != 1 || MundoExtraMikeMenu.ProximoNivel(1) != 2 || MundoExtraMikeMenu.ProximoNivel(2) != 0 || MundoExtraMikeMenu.ProximoNivel(-1) != 0) throw new Exception("ciclo de densidade");
        if (MundoExtraMikeMenu.ValorDensidade(0) != 1f || MundoExtraMikeMenu.ValorDensidade(1) != 0.5f || MundoExtraMikeMenu.ValorDensidade(2) != 0f) throw new Exception("valores de densidade");
        Console.WriteLine("Testes de mundo extra passaram.");
    }
}
