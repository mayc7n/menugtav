using System;
class TestesRegras {
 static void Verificar(bool valor, string mensagem) { if (!valor) throw new Exception(mensagem); }
 static void Main() {
  int valor;
  Verificar(RegrasMikeMenu.TentarCalcular("1000", 50, false, out valor) && valor == 1000, "definir");
  Verificar(RegrasMikeMenu.TentarCalcular("1000", 50, true, out valor) && valor == 1050, "adicionar");
  Verificar(!RegrasMikeMenu.TentarCalcular("-1", 0, false, out valor), "negativo");
  Verificar(!RegrasMikeMenu.TentarCalcular("1.000", 0, false, out valor), "formato");
  Verificar(!RegrasMikeMenu.TentarCalcular("2147483647", 1, true, out valor), "limite");
  Verificar(!RegrasMikeMenu.PodeEditar(true, false, false, true), "online");
  Verificar(!RegrasMikeMenu.PodeEditar(false, true, false, true), "carregando");
  Verificar(!RegrasMikeMenu.PodeEditar(false, false, true, true), "troca");
  Verificar(!RegrasMikeMenu.PodeEditar(false, false, false, false), "personagem");
  Verificar(RegrasMikeMenu.PodeEditar(false, false, false, true), "modo historia");
  Verificar(RegrasMikeMenu.MoverSelecao(0, -1, 4) == 3, "volta ao fim");
  Verificar(RegrasMikeMenu.MoverSelecao(3, 1, 4) == 0, "volta ao inicio");
  Verificar(RegrasMikeMenu.MoverSelecao(0, 1, 0) == 0, "lista vazia");
  Verificar(RegrasMikeMenu.InicioJanela(0, 23, 9) == 0, "janela inicial");
  Verificar(RegrasMikeMenu.InicioJanela(10, 23, 9) == 2, "janela acompanha selecao");
  Verificar(RegrasMikeMenu.InicioJanela(22, 23, 9) == 14, "janela final");
  Verificar(RegrasMikeMenu.InicioJanela(0, 0, 9) == 0, "janela vazia");
  Verificar(RegrasMikeMenu.DestinoValido(120f, -600f, 25f), "destino valido");
  Verificar(!RegrasMikeMenu.DestinoValido(float.NaN, 1f, 2f), "destino invalido");
  Verificar(!RegrasMikeMenu.DestinoValido(1f, 2f, float.PositiveInfinity), "altura invalida");
  Verificar(RegrasMikeMenu.AceitarSolo(352.4f, 352.4f, true), "solo do observatorio");
  Verificar(!RegrasMikeMenu.AceitarSolo(327.7f, 352.4f, true), "rejeita solo abaixo do observatorio");
  Verificar(RegrasMikeMenu.AceitarSolo(100f, 0f, false), "marcador sem altura prevista");
  Verificar(!RegrasMikeMenu.AceitarSolo(float.NaN, 0f, false), "solo invalido");
  Console.WriteLine("Testes passaram.");
 }
}
