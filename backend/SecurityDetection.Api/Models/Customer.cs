public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Importance { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<Detection> Detections { get; set; } = new List<Detection>();
}
