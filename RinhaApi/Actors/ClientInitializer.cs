using Akka.Actor;
using Dapper;
using Npgsql;

namespace RinhaApi.Actors;

public sealed class ClientInitializer(IActorRef shardRegion, string connString) : ReceiveActor
{
    protected override void PreStart()
    {
        base.PreStart();

        async Task InitializeClients()
        {
            await using var conn = new NpgsqlConnection(connString);
            var clients = await conn.QueryAsync<InitializeInfo>("SELECT * FROM clients");

            foreach (var client in clients)
            {
                shardRegion.Tell(new Initialize(client.Id.ToString(), client.Saldo, client.Limite));
            }
        }

        InitializeClients().PipeTo(Self);
    }

    public static Props Props(IActorRef shardRegion, string connString)
    {
        return Akka.Actor.Props.Create<ClientInitializer>(shardRegion, connString);
    }
}


public record InitializeInfo(int Id, int Limite, int Saldo);