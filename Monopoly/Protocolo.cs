namespace MonopolyDistribuido;
 
// ============================================================================
// PROTOCOLO CLIENTE-SERVIDOR (TCP, texto, un mensaje por linea, campos con '|')
// Ver PROTOCOLO.md para la descripcion completa de cada mensaje.
// ============================================================================
public static class Protocolo
{
    public const char Sep = '|';
 
    // ---- Comandos Cliente -> Servidor ----
    public const string CONECTAR = "CONECTAR";
    public const string TIRAR_DADOS = "TIRAR_DADOS";
    public const string COMPRAR_PROPIEDAD = "COMPRAR_PROPIEDAD";
    public const string NO_COMPRAR = "NO_COMPRAR";
    public const string TERMINAR_TURNO = "TERMINAR_TURNO";
    public const string CONSULTAR_ESTADO = "CONSULTAR_ESTADO";
    public const string CONSULTAR_TRANSACCIONES = "CONSULTAR_TRANSACCIONES";
    public const string REGISTRAR_TARJETA = "REGISTRAR_TARJETA";
    public const string SALIR = "SALIR";
 
    // Arma una linea del protocolo. Los campos no pueden contener '|' ni saltos de linea.
    public static string Armar(params object[] campos)
    {
        return string.Join(Sep, campos.Select(c => Limpiar(Convert.ToString(c) ?? "")));
    }
 
    public static string Limpiar(string s)
    {
        return s.Replace(Sep, '/').Replace('\r', ' ').Replace('\n', ' ');
    }
 
    public static string[] Partir(string linea)
    {
        return linea.Split(Sep);
    }
}