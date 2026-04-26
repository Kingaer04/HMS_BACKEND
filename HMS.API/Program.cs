using HMS.API.Hubs;
using HMS.API.Middleware;
using HMS.Entities.Models;
using HMS.LoggerService;
using HMS.Repository.Data;
using HMS.Service;
using HMS.Service.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity ──────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequireLowercase       = true;
    options.Password.RequireUppercase       = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength         = 8;
    options.User.RequireUniqueEmail         = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ── JWT Authentication ────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidAudience            = jwtSettings["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(secretKey),
        ClockSkew                = TimeSpan.Zero
    };

    // Allow SignalR to use JWT from query string (websocket can't set headers)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path        = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ── SignalR ───────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Application Services ──────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,            AuthService>();
builder.Services.AddScoped<INhisVerificationService, NhisVerificationService>();
builder.Services.AddScoped<ITokenService,           TokenService>();
builder.Services.AddScoped<ILoggerService,          LoggerService>();
builder.Services.AddScoped<IPatientService,         PatientService>();
builder.Services.AddScoped<IVisitService,           VisitService>();
builder.Services.AddScoped<INotificationService,    NotificationService>();
builder.Services.AddScoped<IAuditService,           AuditService>();

// ── CORS ──────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
        policy.WithOrigins(
                builder.Configuration
                       .GetSection("AllowedOrigins")
                       .Get<string[]>() ?? new[] { "http://localhost:3000" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()); // Required for SignalR
});

// ── Controllers from HMS.Presentation ────────────────────────────────
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(HMS.Presentation.Controllers.AuthController).Assembly);

// ── Swagger / OpenAPI ─────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "HMS — Healthcare Management System API",
        Version     = "v1",
        Description = """
            ## Healthcare Management System

            A centralized hospital management API for Nigeria, built with C# .NET 8 and Onion Architecture.

            ### Authentication
            - **Hospital Admin** → logs in with `UID + Password` via `/api/auth/login/hospital`
            - **Doctor** → logs in with `Email + Password` via `/api/auth/login/doctor`
            - **Receptionist** → logs in with `Email + Password` via `/api/auth/login/receptionist`

            After login, copy the `accessToken` and click **Authorize** above → paste `Bearer {token}`

            ### Real-time Notifications
            Connect to SignalR at `/hubs/notifications?access_token={your_jwt}`

            ### Patient Identity
            Every patient gets a system-generated `HMS-YYYY-NNNNNN` ID on registration.
            This ID works across all hospitals in the system.
            """
    });

    // JWT security definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Paste your JWT: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Group endpoints by tag
    options.TagActionsBy(api =>
    {
        if (api.GroupName != null) return new[] { api.GroupName };
        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        return new[] { controllerName! };
    });

    options.DocInclusionPredicate((_, _) => true);

    // Include XML comments for rich Swagger descriptions
    var presentationXml = Path.Combine(AppContext.BaseDirectory, "HMS.Presentation.xml");
    if (File.Exists(presentationXml))
        options.IncludeXmlComments(presentationXml);
});

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────
app.UseGlobalExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HMS API v1");
        c.RoutePrefix        = "swagger";
        c.DocumentTitle      = "HMS API Documentation";
        c.DefaultModelsExpandDepth = -1; // Collapse schemas by default
        c.DisplayRequestDuration   = true;
    });
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

// ── SignalR Hub Route ─────────────────────────────────────────────────
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapControllers();

// ── Auto-apply migrations ─────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
