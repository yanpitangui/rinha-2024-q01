using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using RinhaApi;

var builder = WebApplication.CreateSlimBuilder(args);


builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();


var clientesApi = app.MapGroup("/clientes");
clientesApi.MapPost("/{id}/transacoes", (int id, [FromBody] CreateTransacao request) =>
    TypedResults.Ok(new CreateTransacaoResponse()));
clientesApi.MapGet("/{id}/extrato", (int id) =>
    TypedResults.Ok(new GetExtrato()));

app.Run();


[JsonSerializable(typeof(GetExtrato))]
[JsonSerializable(typeof(CreateTransacao))]
[JsonSerializable(typeof(CreateTransacaoResponse))]

internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}


