namespace SmartNetManager.Modelos;

// Retorno padronizado das operacoes
public sealed record ResultadoOperacao(bool Sucesso, string Mensagem)
{
    public static ResultadoOperacao Ok(string mensagem) => new(true, mensagem);

    public static ResultadoOperacao Falha(string mensagem) => new(false, mensagem);
}
