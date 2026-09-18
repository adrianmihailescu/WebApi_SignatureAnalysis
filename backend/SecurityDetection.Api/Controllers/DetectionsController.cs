using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize]
[Route("api/detections")]
public class DetectionsController(AppDbContext db, IHubContext<DetectionHub> hub) : ControllerBase
{
    [HttpGet("queue")]
    public async Task<IActionResult> Queue(CancellationToken cancellationToken) =>
        Ok(await db.Detections
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Signature)
            .Where(x => x.Status == DetectionStatus.Open)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAtUtc)
            .Select(x => DetectionDto.From(x))
            .ToListAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> All(DetectionStatus? status, int? customerId, int? minPriority, CancellationToken cancellationToken)
    {
        var query = db.Detections
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Signature)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);
        if (minPriority.HasValue)
            query = query.Where(x => x.Priority >= minPriority.Value);

        return Ok(await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => DetectionDto.From(x))
            .ToListAsync(cancellationToken));
    }

    [Authorize(Roles = "SecurityAnalyst")]
    [HttpPost("{id:int}/claim")]
    public async Task<IActionResult> Claim(int id, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        if (await db.Detections.AnyAsync(x => x.AssignedToUserId == userId && x.Status == DetectionStatus.Assigned, cancellationToken))
            return Conflict(new { code = "ACTIVE_DETECTION_EXISTS", message = "The analyst already has an active detection." });

        int affected;
        try
        {
            affected = await db.Detections
                .Where(x => x.Id == id && x.Status == DetectionStatus.Open && x.AssignedToUserId == null)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(x => x.Status, DetectionStatus.Assigned)
                    .SetProperty(x => x.AssignedToUserId, userId)
                    .SetProperty(x => x.ClaimedAtUtc, DateTime.UtcNow), cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { code = "ACTIVE_DETECTION_EXISTS", message = "The analyst already has an active detection." });
        }

        if (affected == 0)
            return Conflict(new { code = "DETECTION_ALREADY_CLAIMED", message = "The detection has already been claimed by another analyst." });

        await hub.Clients.All.SendAsync("DetectionClaimed", new { detectionId = id, userId }, cancellationToken);
        var detection = await db.Detections
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Signature)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return Ok(DetectionDto.From(detection));
    }

    [Authorize(Roles = "SecurityAnalyst")]
    [HttpPost("{id:int}/resolve")]
    public async Task<IActionResult> Resolve(int id, ResolveRequest request, CancellationToken cancellationToken)
    {
        if (!TryUserId(out var userId))
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Resolution))
            return BadRequest(new { code = "RESOLUTION_REQUIRED", message = "Resolution is required." });

        var detection = await db.Detections
            .Include(x => x.Customer)
            .Include(x => x.Signature)
            .FirstOrDefaultAsync(x => x.Id == id && x.AssignedToUserId == userId && x.Status == DetectionStatus.Assigned, cancellationToken);
        if (detection is null)
            return NotFound(new { code = "ACTIVE_DETECTION_NOT_FOUND", message = "The active detection was not found." });

        detection.Status = DetectionStatus.Resolved;
        detection.Resolution = request.Resolution.Trim();
        detection.ResolvedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await hub.Clients.All.SendAsync("DetectionResolved", new { detectionId = id }, cancellationToken);
        return Ok(DetectionDto.From(detection));
    }

    private bool TryUserId(out int id) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
}
