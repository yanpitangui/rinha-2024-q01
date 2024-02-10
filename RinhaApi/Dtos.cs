using System.Text.Json.Serialization;

namespace RinhaApi;

public record GetExtrato
{
    public Saldo Saldo { get; set; }
    [JsonPropertyName("ultimas_transacoes")]
    public List<Transacao> UltimasTransacoes { get; set; }
}

public record Saldo
{
    public int Total { get; set; }
    public DateTime DataExtrato { get; set; }
    public int Limite { get; set; }
}

public record Transacao
{
    public int Valor { get; set; }
    public string Tipo { get; set; }
    public string Descricao { get; set; }
    public DateTime RealizadaEm { get; set; }
}

public record CreateTransacao
{
    public int Valor { get; set; }
    public TipoTransacao Tipo { get; set; }
    public string Descricao { get; set; }
}


[JsonConverter(typeof(JsonStringEnumConverter<TipoTransacao>))]
public enum TipoTransacao
{
    NaoEspecificado,
    c,
    d
}

public record CreateTransacaoResponse
{
    public int Limite { get; init; }
    public int Saldo { get; init; }
}