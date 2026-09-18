using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize]
[Route("api/incidents")]
public class IncidentsController(AppDbContext db, SignatureMatcher matcher, IHubContext<DetectionHub> hub) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(IncidentRequest request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == request.CustomerId, cancellationToken);
        if (customer is null)
            return NotFound(new { code = "CUSTOMER_NOT_FOUND", message = "Customer not found." });

        var signature = await matcher.FindMatchAsync(request.Payload, cancellationToken);
        if (signature is null)
            return NoContent();

        var detection = new Detection
        {
            CustomerId = customer.Id,
            SignatureId = signature.Id,
            Priority = customer.Importance + signature.Priority,
            Status = DetectionStatus.Open,
            IncidentPayload = request.Payload.GetRawText(),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Detections.Add(detection);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(detection).Reference(x => x.Customer).LoadAsync(cancellationToken);
        await db.Entry(detection).Reference(x => x.Signature).LoadAsync(cancellationToken);
        await hub.Clients.All.SendAsync("DetectionCreated", DetectionDto.From(detection), cancellationToken);

        return Ok(DetectionDto.From(detection));
    }
}
