using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await db.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(Customer customer, CancellationToken cancellationToken)
    {
        customer.Id = 0;
        customer.CreatedAtUtc = DateTime.UtcNow;
        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/customers/{customer.Id}", customer);
    }
}
