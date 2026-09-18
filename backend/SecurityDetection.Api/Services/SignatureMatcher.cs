using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class SignatureMatcher(AppDbContext db)
{
    public async Task<Signature?> FindMatchAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        var signatures = await db.Signatures
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .ToListAsync(cancellationToken);

        return signatures
            .Where(signature => Matches(signature, payload))
            .OrderByDescending(signature => signature.Priority)
            .ThenBy(signature => signature.Id)
            .FirstOrDefault();
    }

    private static bool Matches(Signature signature, JsonElement payload)
    {
        Dictionary<string, string>? conditions;
        try
        {
            conditions = JsonSerializer.Deserialize<Dictionary<string, string>>(signature.ConditionsJson);
        }
        catch (JsonException)
        {
            return false;
        }

        if (conditions is null || conditions.Count == 0)
            return false;

        return conditions.All(condition =>
            payload.TryGetProperty(condition.Key, out var value) &&
            string.Equals(value.ToString(), condition.Value, StringComparison.OrdinalIgnoreCase));
    }
}
