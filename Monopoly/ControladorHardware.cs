using Monopoly;

namespace MonopolyDistribuido;

public class ControladorHardware
{
    private readonly ListaDados dados = new ListaDados(1, 2, 3, 4, 5, 6, 2, 5, 6, 3);

    public string EnviarComando(string comando)
    {
        return comando switch
        {
            "TIRAR_DADOS" => dados.SiguientePar(),
            "READID" => "UID:SIMULADO",
            _ => "OK"
        };
    }
}
