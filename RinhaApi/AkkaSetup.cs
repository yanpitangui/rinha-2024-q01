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
            
            var defaultShardOptions = new ShardOptions()
            {
                Role = actorSystemName,
            };

            var connString = ctx.Configuration.GetConnectionString("Db")!;
            services.AddAkka(actorSystemName, (akkaBuilder) =>
            {
                var remoteOptions = new RemoteOptions
                {
                    HostName = "localhost",
                    Port = 5213, 
                };
                var clusterOptions = new Akka.Cluster.Hosting.ClusterOptions
                {
                    MinimumNumberOfMembers = 1,
                    SeedNodes = new[] { $"akka.tcp://{actorSystemName}@localhost:5213" },
                    Roles = new[] { actorSystemName }
                };
                var clientMessageExtractor = new ClientMessageExtractor(50);
                akkaBuilder
                    .ConfigureLoggers(setup =>
                    {
#if !DEBUG
                        setup.ClearLoggers();
#endif
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
                    },
                    snapshot =>
                    {
                        snapshot.ConnectionString = connString;
                    });


            });
        });
    }
}