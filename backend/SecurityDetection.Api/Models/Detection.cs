public class Detection
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int SignatureId { get; set; }
    public Signature Signature { get; set; } = null!;
    public int Priority { get; set; }
    public DetectionStatus Status { get; set; }
    public int? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public string IncidentPayload { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? Resolution { get; set; }
}
