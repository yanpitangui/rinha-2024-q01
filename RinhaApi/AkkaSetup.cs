using System.Net;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Persistence.PostgreSql.Hosting;
using Akka.Remote.Hosting;
using RinhaApi.Actors;

namespace RinhaApi;

public static class AkkaSetup
{
    public static void AddAkkaSetup(this IHostBuilder hostBuilder)
    {
        const string actorSystemName = "Rinha";

        hostBuilder.ConfigureServices((ctx, services) =>
        {
            var clusterConfig = ctx.Configuration.GetSection("Cluster");

            var clusterConfigOptions = clusterConfig.Get<ClusterOptions>();
            
            var defaultShardOptions = new ShardOptions()
            {
                Role = actorSystemName,
            };

            var connString = ctx.Configuration.GetConnectionString("Db")!;
            services.AddAkka(actorSystemName, (akkaBuilder) =>
            {
                var remoteOptions = new RemoteOptions
                {
                    HostName = clusterConfigOptions!.Ip,
                    Port = clusterConfigOptions.Port, 
                };
                var clusterOptions = new Akka.Cluster.Hosting.ClusterOptions
                {
                    MinimumNumberOfMembers = 1,
                    SeedNodes = clusterConfigOptions.Seeds,
                    Roles = new[] { actorSystemName }
                };
                var clientMessageExtractor = new ClientMessageExtractor(50);
                akkaBuilder
                    .AddHocon(
                        $@"
akka {{
  actor {{
    serializers {{
      messagepack = ""Akka.Serialization.MessagePack.MsgPackSerializer, Akka.Serialization.MessagePack""
    }}
    serialization-bindings {{
      ""System.Object"" = messagepack
    }}
  }}
}}".ToHocon(), HoconAddMode.Prepend)
                    .ConfigureLoggers(setup =>
                    {
// #if !DEBUG
//                         setup.ClearLoggers();
// #endif
                    })
                    .WithClustering(clusterOptions)
                    .WithRemoting(remoteOptions)
                    .WithShardRegion<Client>(nameof(Client),
                        Client.Props,
                        clientMessageExtractor,
                        defaultShardOptions
                    )
                    .WithSingleton<ClientInitializer>("client-initializer", 
                        (_,ar,_) =>
                            ClientInitializer.Props(ar.Get<Client>(), connString), new ClusterSingletonOptions
                        {
                            Role = actorSystemName,
                        });

                akkaBuilder
                    .WithPostgreSqlPersistence(journal =>
                    {
                        journal.ConnectionString = connString;
                        journal.Serializer = "messagepack";
                    },
                    snapshot =>
                    {
                        snapshot.ConnectionString = connString;
                        snapshot.Serializer = "messagepack";
                    });


            });
        });
    }
}