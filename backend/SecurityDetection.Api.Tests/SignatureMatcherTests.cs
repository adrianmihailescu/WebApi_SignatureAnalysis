using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class SignatureMatcherTests
{
    [Fact]
    public async Task FindMatchAsync_ChoosesHighestPriorityMatchingSignature()
    {
        await using var db = CreateDatabase();
        db.Signatures.AddRange(
            Signature(1, "Lower priority", 5, "{\"eventType\":\"malware\"}"),
            Signature(2, "Higher priority", 10, "{\"eventType\":\"malware\"}"));
        await db.SaveChangesAsync();

        var matcher = new SignatureMatcher(db);
        var result = await matcher.FindMatchAsync(Payload("{\"eventType\":\"malware\"}"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
    }

    [Fact]
    public async Task FindMatchAsync_UsesLowestIdWhenMatchingPrioritiesTie()
    {
        await using var db = CreateDatabase();
        db.Signatures.AddRange(
            Signature(2, "Second", 10, "{\"eventType\":\"malware\"}"),
            Signature(1, "First", 10, "{\"eventType\":\"malware\"}"));
        await db.SaveChangesAsync();

        var matcher = new SignatureMatcher(db);
        var result = await matcher.FindMatchAsync(Payload("{\"eventType\":\"malware\"}"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task FindMatchAsync_RequiresEveryConditionToMatch()
    {
        await using var db = CreateDatabase();
        db.Signatures.Add(Signature(1, "Critical malware", 10, "{\"eventType\":\"malware\",\"severity\":\"critical\"}"));
        await db.SaveChangesAsync();

        var matcher = new SignatureMatcher(db);
        var result = await matcher.FindMatchAsync(Payload("{\"eventType\":\"malware\",\"severity\":\"high\"}"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatchAsync_MatchesValuesCaseInsensitively()
    {
        await using var db = CreateDatabase();
        db.Signatures.Add(Signature(1, "Malware", 10, "{\"eventType\":\"MALWARE\"}"));
        await db.SaveChangesAsync();

        var matcher = new SignatureMatcher(db);
        var result = await matcher.FindMatchAsync(Payload("{\"eventType\":\"malware\"}"), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task FindMatchAsync_IgnoresDisabledAndInvalidSignatures()
    {
        await using var db = CreateDatabase();
        db.Signatures.AddRange(
            Signature(1, "Disabled", 100, "{\"eventType\":\"malware\"}", false),
            Signature(2, "Invalid", 100, "not-json"),
            Signature(3, "Valid", 1, "{\"eventType\":\"malware\"}"));
        await db.SaveChangesAsync();

        var matcher = new SignatureMatcher(db);
        var result = await matcher.FindMatchAsync(Payload("{\"eventType\":\"malware\"}"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
    }

    private static AppDbContext CreateDatabase() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Signature Signature(int id, string name, int priority, string conditionsJson, bool isEnabled = true) =>
        new() { Id = id, Name = name, Priority = priority, ConditionsJson = conditionsJson, IsEnabled = isEnabled };

    private static JsonElement Payload(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
