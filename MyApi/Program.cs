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
builder.Services.AddDbContext<Dbcontext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("defaultconnection")).LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging();
});
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

builder.Services.AddScoped<Iemployeerepository, EmployeeRepository>();
builder.Services.AddScoped<Iemployeeservice, EmployeeService>();
builder.Services.AddScoped<Iauthservice, AuthService>();
builder.Services.AddScoped<Itokenservice, TokenService>();
builder.Services.AddScoped<Irefreshtokenrepository, Refreshtokenrepository>();
builder.Services.AddScoped<Iemailservice, Emailservice>();
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
// builder.Services.AddDbContext<TeamsDbcontext>(options =>
// {
//     options.UseSqlServer(builder.Configuration.GetConnectionString("connection"));
// });
builder.Services.AddAutoMapper(cfg => { }, typeof(EmployeeProfile).Assembly);
var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard();
app.MapControllers();

app.Run();
