using System.Text.Json;
using System.Text.Json.Serialization;
using Akka.Actor;
using Akka.Hosting;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using RinhaApi;
using RinhaApi.Actors;
[module:DapperAot]

var builder = WebApplication.CreateSlimBuilder(args);


builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Host.AddAkkaSetup();

var app = builder.Build();


var clientesApi = app.MapGroup("/clientes");
clientesApi.MapPost("/{id}/transacoes", async (string id, [FromBody] CreateTransacaoRequest request, ActorRegistry registry) =>
{
    if (!int.TryParse(id, out var _)) return Results.UnprocessableEntity();
    if (!int.TryParse(request.Valor?.ToString(), out var valor) || valor < 0) return Results.UnprocessableEntity();
    if (request.Descricao is null or "" or { Length: > 10 }) return Results.UnprocessableEntity();
    var tipo = request.Tipo switch
    {
        "d" => TipoTransacao.d,
        "c" => TipoTransacao.c,
        _ => TipoTransacao.Invalido
    };
    if (tipo == TipoTransacao.Invalido) return Results.UnprocessableEntity();
    var region = registry.Get<Client>();
    var result = await region.Ask(new CreateTransacao
    {
        ClientId = id,
        RealizadaEm = DateTimeOffset.Now,
        Valor = valor,
        Tipo = tipo,
        Descricao = request.Descricao
    });
    if (result is ActionErrors resultadoTransacao)
    {
        switch (resultadoTransacao)
        {
            case ActionErrors.ClienteNaoEncontrado:
                return Results.NotFound();
            case ActionErrors.LimiteExcedido:
                return Results.UnprocessableEntity();
        }
    }
    return Results.Ok(result as CreateTransacaoResponse);
});
clientesApi.MapGet("/{id}/extrato", async (string id, ActorRegistry registry) =>
{
    var region = registry.Get<Client>();
    var result = await region.Ask(new GetExtrato(id));
    if (result is ActionErrors) return Results.NotFound();
    return Results.Ok(result as GetExtratoResponse);
});


app.Run();


[JsonSerializable(typeof(GetExtrato))]
[JsonSerializable(typeof(CreateTransacao))]
[JsonSerializable(typeof(CreateTransacaoResponse))]

internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}


