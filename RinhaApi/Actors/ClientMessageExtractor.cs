using Akka.Cluster.Sharding;

namespace RinhaApi.Actors;

public sealed class ClientMessageExtractor : HashCodeMessageExtractor
{
    public ClientMessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
    {
    }

    public override string? EntityId(object message)
    {
        return message switch
        {

            IWithClientId e => e.ClientId,
            _ => null
        };
    }
}

public interface IWithClientId
{
    public string ClientId { get; }
}