namespace KiranaHub.StreamingCep.Cep;

public sealed record UserCartStepEvent(
    string CustomerId,
    string ItemSku,
    string Action, // "Search", "AddToCart", "Checkout"
    long Timestamp);

public sealed record CartAbandonmentAlert(
    string CustomerId,
    string ItemSku,
    long AddedToCartTime,
    long AlertTriggeredTime,
    double RecommendedVoucherDiscountPercent);

/// <summary>
/// Complex Event Processing (CEP) Pattern State Machine (Flink CEP / Esper pattern).
/// Matches non-deterministic temporal sequence patterns:
/// Search(item) -> AddToCart(item) -> [No Checkout within 5 minutes] -> Alert(PushSurgeDiscount).
/// </summary>
public sealed class PatternStateMachine
{
    private enum CepState
    {
        Idle = 0,
        ItemSearched = 1,
        AddedToCart = 2
    }

    private sealed class UserSessionContext
    {
        public CepState State = CepState.Idle;
        public string ItemSku = "";
        public long ActionTimestamp = 0;
    }

    private readonly Dictionary<string, UserSessionContext> _userSessions = new();
    private readonly long _abandonmentTimeoutMs;

    public PatternStateMachine(long abandonmentTimeoutMs = 300_000) // 5 minutes default
    {
        _abandonmentTimeoutMs = abandonmentTimeoutMs;
    }

    // [KYA KARTA HAI]: User clickstream action event ingest karta hai aur CEP DFA state machine transition karta hai.
    // [KYU ZAROORI HAI]: Complex behavioral patterns (Search -> AddToCart -> Checkout) track karna.
    // [KAISE CHALTA HAI]: Evaluates action against current user state; updates transition and timestamp.
    // [OUTPUT / SIDE EFFECT]: State context updated.
    // [VARIATIONS TO TRY]: Multi-item parallel patterns.
    public void IngestUserAction(UserCartStepEvent action)
    {
        if (!_userSessions.TryGetValue(action.CustomerId, out var ctx))
        {
            ctx = new UserSessionContext();
            _userSessions[action.CustomerId] = ctx;
        }

        switch (action.Action)
        {
            case "Search":
                ctx.State = CepState.ItemSearched;
                ctx.ItemSku = action.ItemSku;
                ctx.ActionTimestamp = action.Timestamp;
                break;

            case "AddToCart":
                if (ctx.State == CepState.ItemSearched && ctx.ItemSku == action.ItemSku)
                {
                    ctx.State = CepState.AddedToCart;
                    ctx.ActionTimestamp = action.Timestamp;
                }
                break;

            case "Checkout":
                // Successful purchase: reset state machine
                ctx.State = CepState.Idle;
                ctx.ItemSku = "";
                ctx.ActionTimestamp = 0;
                break;
        }
    }

    // [KYA KARTA HAI]: Current event time watermark ke hisab se abandoned carts detect karta hai.
    // [KYU ZAROORI HAI]: Cart add hone ke 5 minute baad bhi checkout na hone par instant intervention discount bhejna.
    // [KAISE CHALTA HAI]: Checks if State == AddedToCart and (currentWatermark - ActionTimestamp >= timeout).
    // [OUTPUT / SIDE EFFECT]: Emits list of CartAbandonmentAlert objects.
    // [VARIATIONS TO TRY]: Progressive discount escalation (10% -> 20%).
    public List<CartAbandonmentAlert> EvaluateTimeouts(long currentWatermark)
    {
        var alerts = new List<CartAbandonmentAlert>();
        var sessionsToReset = new List<string>();

        foreach (var (customerId, ctx) in _userSessions)
        {
            if (ctx.State == CepState.AddedToCart)
            {
                long elapsedSinceCart = currentWatermark - ctx.ActionTimestamp;
                if (elapsedSinceCart >= _abandonmentTimeoutMs)
                {
                    // Trigger CEP Alert!
                    alerts.Add(new CartAbandonmentAlert(
                        CustomerId: customerId,
                        ItemSku: ctx.ItemSku,
                        AddedToCartTime: ctx.ActionTimestamp,
                        AlertTriggeredTime: currentWatermark,
                        RecommendedVoucherDiscountPercent: 15.0));

                    sessionsToReset.Add(customerId);
                }
            }
        }

        foreach (var cid in sessionsToReset)
        {
            _userSessions[cid].State = CepState.Idle;
        }

        return alerts;
    }
}
