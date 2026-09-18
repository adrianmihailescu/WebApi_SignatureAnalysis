using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize]
[Route("api/signatures")]
public class SignaturesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await db.Signatures.AsNoTracking().OrderByDescending(x => x.Priority).ThenBy(x => x.Name).ToListAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(Signature signature, CancellationToken cancellationToken)
    {
        signature.Id = 0;
        signature.CreatedAtUtc = DateTime.UtcNow;
        db.Signatures.Add(signature);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/signatures/{signature.Id}", signature);
    }
}
