using MyApi.data;
using Microsoft.EntityFrameworkCore;
using MyApi.Mappings;
using MyApi.Repository.Interface;
using MyApi.Repository.Implementation;
using MyApi.Service.Interface;
using MyApi.Service.Implementation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Serilog;
using MyApi.Middleware;
using System.Data;
using Microsoft.Data.SqlClient;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MyApi", Version = "v1" });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});


//gzip compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;

    // Add Gzip
    options.Providers.Add<GzipCompressionProvider>();

    // Ensure JSON is included
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json" });
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal; // better compression
});


//dbcontext
builder.Services.AddDbContext<Dbcontext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("defaultconnection")).LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging();
});


//hangfire for background jobs
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();


//rate-limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("loginpolicy", context =>
{
    var ip = context.Connection.RemoteIpAddress?.ToString();

    return RateLimitPartition.GetFixedWindowLimiter(
        ip!,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
});
});


//snake-casing for JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCase();
    });

// logging with serilog

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] [TraceId: {TraceId}] {Message}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/app.log",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [TraceId: {TraceId}] {Message}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

//connection for dapper
builder.Services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(
        sp.GetRequiredService<IConfiguration>()
          .GetConnectionString("defaultconnection")
    ));

    
builder.Services.AddScoped<Iemployeerepository, EmployeeRepository>();
builder.Services.AddScoped<Iemployeeservice, EmployeeService>();
builder.Services.AddScoped<Iauthservice, AuthService>();
builder.Services.AddScoped<Itokenservice, TokenService>();
builder.Services.AddScoped<Irefreshtokenrepository, Refreshtokenrepository>();
builder.Services.AddScoped<Iemailservice, Emailservice>();
builder.Services.AddScoped<IHmacservice, Hmacservice>();
builder.Services.AddScoped<IDapperrepo, DapperEmployeeRepository>();
builder.Services.AddScoped<IDapperservice, DapperEmployeeService>();
builder.Services.AddMemoryCache();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured")))
        };
});

builder.Services.AddAuthorization();

builder.Services.AddAutoMapper(cfg => { }, typeof(EmployeeProfile).Assembly);
var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<Traceidmiddleware>();
app.UseMiddleware<Exceptionmiddleware>();
app.UseHttpsRedirection();
app.UseMiddleware<InputSanitizationMiddleware>();
app.UseRateLimiter();
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<HmacMiddleware>();
app.UseHangfireDashboard();
app.MapControllers();

app.Run();
