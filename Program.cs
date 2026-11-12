using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using pantry_identity;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.Audience = builder.Configuration["Keycloak:Audience"];

    // Discovery normally requires https; Keycloak is plain http locally.
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

    // Keep Keycloak's claim names ("sub", "preferred_username", "email")
    // instead of renaming them to long Microsoft claim-type URIs.
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,          // default
        ValidateAudience = true,        // default
        ValidateLifetime = true,        // default
        ValidateIssuerSigningKey = true,
        NameClaimType = "preferred_username", // makes User.Identity.Name work
        ClockSkew = TimeSpan.FromSeconds(30)  // tighter than the 5-minute default
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            ctx.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>()
                .LogWarning(ctx.Exception, "JWT rejected");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

builder.Services.AddDbContext<IdentityDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb"))
    .UseSnakeCaseNamingConvention()
);

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("IdentityDb")!
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.MapGet("/me", (ClaimsPrincipal user) => new
{
    Id = user.FindFirstValue("sub"),
    Username = user.FindFirstValue("preferred_username"),
    Email = user.FindFirstValue("email")
});

app.Run();
