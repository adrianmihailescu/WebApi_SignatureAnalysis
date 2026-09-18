using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Security Detection API",
        Version = "v1"
    });
});

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR();
builder.Services.AddScoped<SignatureMatcher>();

builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/detections"))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.InitializeAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<DetectionHub>("/hubs/detections");
app.Run();

public enum DetectionStatus { Open = 0, Assigned = 1, Resolved = 2 }

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Importance { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<Detection> Detections { get; set; } = new List<Detection>();
}

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

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
    public ICollection<Detection> AssignedDetections { get; set; } = new List<Detection>();
}

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

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, int UserId, string Username, string Role);
public class IncidentRequest { public int CustomerId { get; set; } public JsonElement Payload { get; set; } }
public record ResolveRequest(string Resolution);
public record DetectionDto(int Id, int CustomerId, string CustomerName, int SignatureId, string SignatureName, int Priority, DetectionStatus Status, int? AssignedToUserId, string IncidentPayload, DateTime CreatedAtUtc, DateTime? ClaimedAtUtc, DateTime? ResolvedAtUtc, string? Resolution)
{
    public static DetectionDto From(Detection x) => new(x.Id, x.CustomerId, x.Customer.Name, x.SignatureId, x.Signature.Name, x.Priority, x.Status, x.AssignedToUserId, x.IncidentPayload, x.CreatedAtUtc, x.ClaimedAtUtc, x.ResolvedAtUtc, x.Resolution);
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Signature> Signatures => Set<Signature>();
    public DbSet<Detection> Detections => Set<Detection>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(x => x.Username).IsUnique();
        b.Entity<Detection>().HasIndex(x => new { x.Status, x.Priority, x.CreatedAtUtc });
        b.Entity<Detection>().HasIndex(x => new { x.AssignedToUserId, x.Status })
            .IsUnique().HasFilter("[AssignedToUserId] IS NOT NULL AND [Status] = 1");
        b.Entity<Detection>().HasOne(x => x.Customer).WithMany(x => x.Detections).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Detection>().HasOne(x => x.Signature).WithMany(x => x.Detections).HasForeignKey(x => x.SignatureId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Detection>().HasOne(x => x.AssignedToUser).WithMany(x => x.AssignedDetections).HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class DetectionHub : Hub { }

public class SignatureMatcher(AppDbContext db)
{
    public async Task<Signature?> FindMatchAsync(JsonElement payload, CancellationToken ct)
    {
        var signatures = await db.Signatures.AsNoTracking().Where(x => x.IsEnabled).ToListAsync(ct);
        return signatures.Where(s => Matches(s, payload)).OrderByDescending(s => s.Priority).ThenBy(s => s.Id).FirstOrDefault();
    }
    private static bool Matches(Signature signature, JsonElement payload)
    {
        Dictionary<string, string>? conditions;
        try { conditions = JsonSerializer.Deserialize<Dictionary<string, string>>(signature.ConditionsJson); }
        catch (JsonException)
        { return false; }
        if (conditions is null || conditions.Count == 0)
            return false;
        return conditions.All(c => payload.TryGetProperty(c.Key, out var value) && string.Equals(value.ToString(), c.Value, StringComparison.OrdinalIgnoreCase));
    }
}

public static class DbSeeder
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User { Username="admin", PasswordHash=BCrypt.Net.BCrypt.HashPassword("Admin123!"), Role="Admin" },
                new User { Username="support", PasswordHash=BCrypt.Net.BCrypt.HashPassword("Support123!"), Role="ReadOnly" },
                new User { Username="analyst", PasswordHash=BCrypt.Net.BCrypt.HashPassword("Analyst123!"), Role="SecurityAnalyst" });
            await db.SaveChangesAsync();
        }
        if (!await db.Customers.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.Customers.AddRange(
                new Customer { Name="Acme Corporation", Importance=10, CreatedAtUtc=now },
                new Customer { Name="Contoso", Importance=7, CreatedAtUtc=now },
                new Customer { Name="Small Customer", Importance=3, CreatedAtUtc=now });
            await db.SaveChangesAsync();
        }
        if (!await db.Signatures.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.Signatures.AddRange(
                new Signature { Name="Critical Malware", Priority=10, ConditionsJson="""{"eventType":"malware","severity":"critical"}""", IsEnabled=true, CreatedAtUtc=now },
                new Signature { Name="Suspicious Process", Priority=7, ConditionsJson="""{"eventType":"suspicious_process"}""", IsEnabled=true, CreatedAtUtc=now },
                new Signature { Name="Network Attack", Priority=8, ConditionsJson="""{"eventType":"network_attack","severity":"high"}""", IsEnabled=true, CreatedAtUtc=now });
            await db.SaveChangesAsync();
        }
    }
}

public static class JwtTokens
{
    public static string Create(User user, IConfiguration config)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Username), new Claim(ClaimTypes.Role, user.Role) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, expires: DateTime.UtcNow.AddHours(config.GetValue("Jwt:ExpirationHours", 8)), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Username == request.Username, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message="Invalid username or password." });
        return Ok(new LoginResponse(JwtTokens.Create(user, config), user.Id, user.Username, user.Role));
    }
}

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await db.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct));

    [Authorize(Roles="Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(Customer customer, CancellationToken ct)
    {
        customer.Id=0; customer.CreatedAtUtc=DateTime.UtcNow; db.Customers.Add(customer); await db.SaveChangesAsync(ct); return Created($"/api/customers/{customer.Id}", customer);
    }
}

[ApiController]
[Authorize]
[Route("api/signatures")]
public class SignaturesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await db.Signatures.AsNoTracking().OrderByDescending(x=>x.Priority).ThenBy(x=>x.Name).ToListAsync(ct));

    [Authorize(Roles="Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(Signature signature, CancellationToken ct)
    {
        signature.Id=0; signature.CreatedAtUtc=DateTime.UtcNow; db.Signatures.Add(signature); await db.SaveChangesAsync(ct); return Created($"/api/signatures/{signature.Id}", signature);
    }
}

[ApiController]
[Authorize]
[Route("api/incidents")]
public class IncidentsController(AppDbContext db, SignatureMatcher matcher, IHubContext<DetectionHub> hub) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(IncidentRequest request, CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x=>x.Id==request.CustomerId, ct);
        if (customer is null)
            return NotFound(new { code="CUSTOMER_NOT_FOUND", message="Customer not found." });
        
        var signature = await matcher.FindMatchAsync(request.Payload, ct);
        if (signature is null)
            return NoContent();

        var detection = new Detection { CustomerId=customer.Id, SignatureId=signature.Id, Priority=customer.Importance+signature.Priority, Status=DetectionStatus.Open, IncidentPayload=request.Payload.GetRawText(), CreatedAtUtc=DateTime.UtcNow };
        db.Detections.Add(detection);
        await db.SaveChangesAsync(ct);
        await db.Entry(detection).Reference(x=>x.Customer).LoadAsync(ct);
        await db.Entry(detection).Reference(x=>x.Signature).LoadAsync(ct);
        await hub.Clients.All.SendAsync("DetectionCreated", DetectionDto.From(detection), ct);
        
        return Ok(DetectionDto.From(detection));
    }
}

[ApiController]
[Authorize]
[Route("api/detections")]
public class DetectionsController(AppDbContext db, IHubContext<DetectionHub> hub) : ControllerBase
{
    [HttpGet("queue")]
    public async Task<IActionResult> Queue(CancellationToken ct) => Ok(await db.Detections.AsNoTracking().Include(x=>x.Customer).Include(x=>x.Signature).Where(x=>x.Status==DetectionStatus.Open).OrderByDescending(x=>x.Priority).ThenBy(x=>x.CreatedAtUtc).Select(x=>DetectionDto.From(x)).ToListAsync(ct));

    [HttpGet]
    public async Task<IActionResult> All(DetectionStatus? status, int? customerId, int? minPriority, CancellationToken ct)
    {
        var q=db.Detections.AsNoTracking().Include(x=>x.Customer).Include(x=>x.Signature).AsQueryable();
        if(status.HasValue)
            q=q.Where(x=>x.Status==status.Value);

        if(customerId.HasValue)
            q=q.Where(x=>x.CustomerId==customerId.Value);

        if(minPriority.HasValue)
            q=q.Where(x=>x.Priority>=minPriority.Value);

        return Ok(await q.OrderByDescending(x=>x.CreatedAtUtc).Select(x=>DetectionDto.From(x)).ToListAsync(ct));
    }

    [Authorize(Roles="SecurityAnalyst")]
    [HttpPost("{id:int}/claim")]
    public async Task<IActionResult> Claim(int id, CancellationToken ct)
    {
        if(!TryUserId(out var userId)) return Unauthorized();
        if(await db.Detections.AnyAsync(x=>x.AssignedToUserId==userId && x.Status==DetectionStatus.Assigned, ct))
            return Conflict(new { code="ACTIVE_DETECTION_EXISTS", message="The analyst already has an active detection." });

        int affected;
        try
        {
            affected=await db.Detections.Where(x=>x.Id==id && x.Status==DetectionStatus.Open && x.AssignedToUserId==null)
                .ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,DetectionStatus.Assigned).SetProperty(x=>x.AssignedToUserId,userId).SetProperty(x=>x.ClaimedAtUtc,DateTime.UtcNow),ct);
        }
        catch (DbUpdateException)
        {
            // The filtered unique index protects the one-active-detection-per-analyst invariant under concurrency.
            return Conflict(new { code="ACTIVE_DETECTION_EXISTS", message="The analyst already has an active detection." });
        }
        if(affected==0)
            return Conflict(new { code="DETECTION_ALREADY_CLAIMED", message="The detection has already been claimed by another analyst." });

        await hub.Clients.All.SendAsync("DetectionClaimed", new { detectionId=id, userId }, ct);
        var detection=await db.Detections.AsNoTracking().Include(x=>x.Customer).Include(x=>x.Signature).FirstAsync(x=>x.Id==id,ct);
        return Ok(DetectionDto.From(detection));
    }

    [Authorize(Roles="SecurityAnalyst")]
    [HttpPost("{id:int}/resolve")]
    public async Task<IActionResult> Resolve(int id, ResolveRequest request, CancellationToken ct)
    {
        if(!TryUserId(out var userId))
            return Unauthorized();

        if(string.IsNullOrWhiteSpace(request.Resolution))
            return BadRequest(new { code="RESOLUTION_REQUIRED", message="Resolution is required." });
        var detection=await db.Detections.Include(x=>x.Customer).Include(x=>x.Signature).FirstOrDefaultAsync(x=>x.Id==id && x.AssignedToUserId==userId && x.Status==DetectionStatus.Assigned,ct);
        if(detection is null)
            return NotFound(new { code="ACTIVE_DETECTION_NOT_FOUND", message="The active detection was not found." });
        detection.Status=DetectionStatus.Resolved; detection.Resolution=request.Resolution.Trim(); detection.ResolvedAtUtc=DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await hub.Clients.All.SendAsync("DetectionResolved", new { detectionId=id }, ct);
        return Ok(DetectionDto.From(detection));
    }

    private bool TryUserId(out int id) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
}
