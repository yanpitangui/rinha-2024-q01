using System.Text.Json.Serialization;

namespace RinhaApi;



public sealed record CreateTransacaoRequest
{
    public object Valor { get; init; } = null!;
    public string Descricao { get; init; } = null!;
    public string Tipo { get; init; } = null!;
}


[JsonConverter(typeof(JsonStringEnumConverter<TipoTransacao>))]
public enum TipoTransacao
{
    Invalido,
    c,
    d
}