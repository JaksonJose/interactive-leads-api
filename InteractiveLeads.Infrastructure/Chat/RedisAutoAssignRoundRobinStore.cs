using InteractiveLeads.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace InteractiveLeads.Infrastructure.Chat;

public sealed class RedisAutoAssignRoundRobinStore(IConnectionMultiplexer mux, ILogger<RedisAutoAssignRoundRobinStore> logger)
    : IAutoAssignRoundRobinStore
{
    private readonly IDatabase _db = mux.GetDatabase();

    public async Task<int> GetNextSlotIndexAsync(Guid teamId, int candidateCount, CancellationToken cancellationToken)
    {
        if (candidateCount <= 0)
            return 0;

        try
        {
            var key = (RedisKey)$"autoassign:rr:{teamId:D}";
            var v = await _db.StringIncrementAsync(key);
            return (int)((v - 1) % candidateCount);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis error in round-robin counter for team {TeamId}; using random slot", teamId);
            return Random.Shared.Next(candidateCount);
        }
    }
}
