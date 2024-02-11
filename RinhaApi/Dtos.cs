using System.Text.Json.Serialization;

namespace RinhaApi;



public record struct CreateTransacaoRequest(object Valor, string Descricao, string Tipo);

[JsonConverter(typeof(JsonStringEnumConverter<TipoTransacao>))]
public enum TipoTransacao
{
    Invalido,
    c,
    d
}