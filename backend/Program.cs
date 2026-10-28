using backend.Data;
using backend.Interfaces;
using backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.Extensions.Options;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddIdentityCore<IdentityUser<Guid>>(options =>
{
    options.Password.RequiredLength = 8;
})
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

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
        IssuerSigningKey = signingKey
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("access_token", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
builder.Services.AddHttpClient<IJobEmailClassifier, OllamaJobEmailClassifier>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(3); // first call after the model unloads, has to reload it
});

builder.Services.AddOptions<GoogleOptions>()
    .Bind(builder.Configuration.GetSection("Google"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId)
                && !string.IsNullOrWhiteSpace(o.ClientSecret)
                && !string.IsNullOrWhiteSpace(o.RedirectUri),
              "Google:ClientId, Google:ClientSecret and Google:RedirectUri must all be configured.")
    .ValidateOnStart();

builder.Services.AddOptions<SyncOptions>()
    .Bind(builder.Configuration.GetSection("Sync"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHostedService<PollingWorker>();

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IGmailConnectionService, GmailConnectionService>();
builder.Services.AddDataProtection();
builder.Services.AddSingleton<ITokenProtector, TokenProtector>();
builder.Services.AddScoped<IGmailReader, GmailReader>();
builder.Services.AddSingleton<IEmailPrefilter, EmailPrefilter>();
builder.Services.AddScoped<IEmailSyncService, EmailSyncService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddSingleton<SyncCoordinator>();
builder.Services.AddHostedService<SyncWorker>();
builder.Services.AddScoped<IMailboxService, MailboxService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
