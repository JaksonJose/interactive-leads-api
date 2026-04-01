using InteractiveLeads.Application.Interfaces;
using StackExchange.Redis;

namespace InteractiveLeads.Infrastructure.Chat;

public sealed class RedisAutoAssignRoundRobinStore(IConnectionMultiplexer mux) : IAutoAssignRoundRobinStore
{
    private readonly IDatabase _db = mux.GetDatabase();

    public async Task<int> GetNextSlotIndexAsync(Guid teamId, int candidateCount, CancellationToken cancellationToken)
    {
        if (candidateCount <= 0)
            return 0;

        var key = (RedisKey)$"autoassign:rr:{teamId:D}";
        var v = await _db.StringIncrementAsync(key);
        return (int)((v - 1) % candidateCount);
    }
}
