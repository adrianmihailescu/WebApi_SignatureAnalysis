public record DetectionDto(
    int Id,
    int CustomerId,
    string CustomerName,
    int SignatureId,
    string SignatureName,
    int Priority,
    DetectionStatus Status,
    int? AssignedToUserId,
    string IncidentPayload,
    DateTime CreatedAtUtc,
    DateTime? ClaimedAtUtc,
    DateTime? ResolvedAtUtc,
    string? Resolution)
{
    public static DetectionDto From(Detection detection) => new(
        detection.Id,
        detection.CustomerId,
        detection.Customer.Name,
        detection.SignatureId,
        detection.Signature.Name,
        detection.Priority,
        detection.Status,
        detection.AssignedToUserId,
        detection.IncidentPayload,
        detection.CreatedAtUtc,
        detection.ClaimedAtUtc,
        detection.ResolvedAtUtc,
        detection.Resolution);
}
