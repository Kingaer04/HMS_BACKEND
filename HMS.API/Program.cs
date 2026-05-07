using HMS.Service.Hubs;
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
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity ──────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ── JWT Authentication ────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero
    };

    // Pass JWT via query string for SignalR WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ── SignalR ───────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Dependency Injection (All Services) ───────────────────────────────

// Auth & Infrastructure
builder.Services.AddScoped<ILoggerService, LoggerService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExtendedAuthService, ExtendedAuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<INhisVerificationService, NhisVerificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Core HMS Logic
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IVisitService, VisitService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();
builder.Services.AddScoped<ILabService, LabService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();

// Management & Profile
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAdminService, AdminService>();

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
              .AllowCredentials());
});

// ── Controllers ───────────────────────────────────────────────────────
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(HMS.Presentation.Controllers.AuthController).Assembly);

// ── Swagger / OpenAPI Documentation ───────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HMS — Healthcare Management System API",
        Version = "v1",
        Description = """
            ## 🏥 System Overview
            Centralized hospital management API for Nigeria — **C# .NET 8**, Onion Architecture.

            ---

            ### 🧪 Developer Testing Guide
            1. **Authentication:** Call Login → Copy `token` → **Authorize** button → `Bearer {token}`.
            2. **SignalR:** Hub at `/hubs/notifications`. Pass token in query: `?access_token=...`
            3. **Clinical Flow:** Register Patient → Book Appointment → Check-in (Vitals) → Consultation → Billing.

            ---

            ### Login Endpoints by Role
            | Role | Endpoint | Credentials |
            |------|----------|-------------|
            | Hospital Admin | `POST /api/auth/login/hospital` | UID + Password |
            | Doctor | `POST /api/auth/login/doctor` | Email + Password |
            | Receptionist | `POST /api/auth/login/receptionist` | Email + Password |
            | Patient (App) | `POST /api/auth/login/patient` | Email + Password |

            ---

            ### Medical Record Access Rules
            | Section | Write | Read |
            |---------|-------|------|
            | Vital Signs | Receptionist | All staff |
            | Doctor Notes | Assigned Doctor | Doctors only |
            | Lab Results | Lab Technician | Doctor, Admin |

            ---

            ### Mock NHIS UIDs for Testing
            `NHIS-0001-LG` · `NHIS-0002-AB` · `NHIS-0003-KN`
            """
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    options.TagActionsBy(api => new[] { api.ActionDescriptor.RouteValues["controller"]! });

    var xml = Path.Combine(AppContext.BaseDirectory, "HMS.Presentation.xml");
    if (File.Exists(xml)) options.IncludeXmlComments(xml);
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
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "HMS API Documentation";
        c.DefaultModelsExpandDepth(-1);
        c.DisplayRequestDuration();
        c.EnableFilter();
    });
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

// ── Routes ────────────────────────────────────────────────────────────
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapControllers();

// ── Auto-Migration ────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();