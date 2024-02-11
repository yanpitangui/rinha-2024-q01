using Akka.Actor;
using Akka.Persistence;
using MessagePack;

namespace RinhaApi.Actors;

public sealed class Client : ReceivePersistentActor
{
    public override string PersistenceId { get; }
    private ClientState _state = new();

    public Client(string persistenceId)
    {
        PersistenceId = persistenceId;

        
        CommandAny(o =>
        {
            if (o is Initialize init)
            {
                PersistAsync(init, InitializeHandler);
            }
            else
            {
                Sender.Tell(ActionErrors.ClienteNaoEncontrado);
            }
        });

        RecoveryHandlers();

    }


    private void Initialized()
    {
        Command<CreateTransacao>(msg =>
        {
            if(msg.Tipo == TipoTransacao.d && _state.Saldo - msg.Valor < 0)
            {
                Sender.Tell(ActionErrors.LimiteExcedido);
                return;
            }
            
            msg.RealizadaEm = DateTimeOffset.Now;
            PersistAsync(msg, persisted =>
            {
                var sender = Sender;
                CreateTransacaoHandler(persisted);
                sender.Tell(new CreateTransacaoResponse
                {
                    Limite = _state.Limite,
                    Saldo = _state.Saldo
                });
            });

        });
        
        Command<GetExtrato>(_ =>
        {
            // ...
            Sender.Tell(new GetExtratoResponse
            {
                Saldo = new Saldo
                {
                    Total = _state.Saldo,
                    DataExtrato = DateTime.Now,
                    Limite = _state.Limite
                },
                UltimasTransacoes = _state.Transacoes.OrderByDescending(x => x.RealizadaEm).Take(10).ToArray()
            });
        });
        
        Command<Initialize>(_ => {});
        
    }

    private void RecoveryHandlers()
    {
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is ClientState state)
            {
                _state = state;
            }
        });
        
        Recover<Initialize>(InitializeHandler);

        Recover<CreateTransacao>(CreateTransacaoHandler);
    }
    
    
    private void InitializeHandler(Initialize hand)
    {
        _state = new ClientState {Saldo = hand.Saldo, Limite = hand.Limite};
        Become(Initialized);
    }

    private void CreateTransacaoHandler(CreateTransacao create)
    {
        _state.Saldo += create.Tipo switch
        {
            TipoTransacao.c => create.Valor,
            TipoTransacao.d => -create.Valor,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        _state.Transacoes.Add(new Transacao
        {
            Tipo = create.Tipo,
            Descricao = create.Descricao,
            RealizadaEm = create.RealizadaEm,
            Valor = create.Valor
        });
    }
    
    public static Props Props(string persistenceId)
    {
        return Akka.Actor.Props.Create<Client>(persistenceId);
    }
}

public sealed record ClientState
{
    public int Saldo { get; set; }
    public int Limite { get; init; }
    public List<Transacao> Transacoes { get; init; } = new();
}

public sealed record Initialize : IWithClientId
{
    public Initialize(string ClientId, int Saldo, int Limite)
    {
        this.ClientId = ClientId;
        this.Saldo = Saldo;
        this.Limite = Limite;
    }

    [Key(0)]
    public string ClientId { get; init; }
    
    [Key(1)]
    public int Saldo { get; init; }
    
    [Key(2)]
    public int Limite { get; init; }
}

[MessagePackObject]
public sealed record CreateTransacao: IWithClientId
{
    [Key(0)]
    public int Valor { get; set; }
    
    [Key(1)]
    public TipoTransacao Tipo { get; set; }
    
    [Key(2)]
    public string Descricao { get; set; } = null!;
    
    [Key(3)]
    public string ClientId { get; init; } = null!;
    
    [Key(4)]
    public DateTimeOffset RealizadaEm { get; set; }
}

[MessagePackObject]
public sealed record GetExtrato: IWithClientId
{
    protected GetExtrato(){}

    public GetExtrato(string clientId)
    {
        ClientId = clientId;
    }
    
    [Key(0)]
    public string ClientId { get; init; }
}

[MessagePackObject]
public sealed record CreateTransacaoResponse
{
    [Key(0)]
    public int Limite { get; init; }
    
    [Key(1)]
    public int Saldo { get; init; }
}

[MessagePackObject]
public sealed record GetExtratoResponse
{
    [Key(0)]
    public required Saldo Saldo { get; set; }
    
    [Key(1)]
    public required Transacao[] UltimasTransacoes { get; set; }
}

[MessagePackObject]
public sealed record Saldo
{
    [Key(0)]
    public int Total { get; set; }
    
    [Key(1)]
    public DateTime DataExtrato { get; set; }
    
    [Key(2)]
    public int Limite { get; set; }
}

[MessagePackObject]
public sealed record Transacao
{
    [Key(0)]
    public int Valor { get; set; }
    
    [Key(1)]
    public TipoTransacao Tipo { get; set; }
    
    [Key(2)]
    public required string Descricao { get; set; }
    
    [Key(3)]
    public DateTimeOffset RealizadaEm { get; set; }
}

public enum ActionErrors
{
    ClienteNaoEncontrado,
    LimiteExcedido
}