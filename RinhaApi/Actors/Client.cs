using Akka.Actor;
using Akka.Persistence;

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
                Persist(init, InitializeHandler);
            }
            else
            {
                Sender.Tell(ActionErrors.ClienteNaoEncontrado);
            }
        });

        RecoveryHandlers();

    }


    public void Initialized()
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
        
        Command<SaveSnapshotSuccess>(_ => { });
        
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

public record ClientState
{
    public int Saldo { get; set; }
    public int Limite { get; init; }
    public List<Transacao> Transacoes { get; init; } = new();
}

public record Initialize(string ClientId, int Saldo, int Limite) : IWithClientId;

public record CreateTransacao: IWithClientId
{
    public int Valor { get; set; }
    public TipoTransacao Tipo { get; set; }
    public string Descricao { get; set; } = null!;
    
    public string ClientId { get; init; } = null!;
    public DateTimeOffset RealizadaEm { get; set; }
}

public record GetExtrato(string ClientId) : IWithClientId;


public record CreateTransacaoResponse
{
    public int Limite { get; init; }
    public int Saldo { get; init; }
}

public record GetExtratoResponse
{
    public required Saldo Saldo { get; set; }
    public required Transacao[] UltimasTransacoes { get; set; }
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
    public TipoTransacao Tipo { get; set; }
    public required string Descricao { get; set; }
    public DateTimeOffset RealizadaEm { get; set; }
}

public enum ActionErrors
{
    ClienteNaoEncontrado,
    LimiteExcedido
}