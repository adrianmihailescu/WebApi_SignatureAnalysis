public class Signature
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Priority { get; set; }
    public string ConditionsJson { get; set; } = "{}";
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<Detection> Detections { get; set; } = new List<Detection>();
}
