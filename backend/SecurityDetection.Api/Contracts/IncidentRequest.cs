using System.Text.Json;

public class IncidentRequest
{
    public int CustomerId { get; set; }
    public JsonElement Payload { get; set; }
}

public record ResolveRequest(string Resolution);
