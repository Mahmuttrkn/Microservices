using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Ocelot.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using OcelotgatewayAPI;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Configuration.AddJsonFile($"ocelot.json", false, true);
builder.Services.AddOcelot(builder.Configuration);

var configuration = builder.Configuration; // IConfiguration nesnesini alıyoruz.

SymmetricSecurityKey signInKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Security"]));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signInKey,
            ValidateIssuer = true,
            ValidIssuer = configuration["JWT:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["JWT:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpcontext =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpcontext.Request.Headers.Host.ToString(),
        factory: partition => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 3,
            Window = TimeSpan.FromSeconds(5)
        }
    ));

    //İsteklerin engellenmesi durumunda verilence hata kodu ve mesaj bilgisi.
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            await context.HttpContext.Response.WriteAsync(
                $"İstek sınır sayısına ulaştınız. {retryAfter.TotalSeconds} saniye sonra tekrar deneyiniz. ", cancellationToken: token);
        }
        else
        {
            await context.HttpContext.Response.WriteAsync(
                "İstek sınırına ulaştınız. Daha sonra tekrar deneyin. ", cancellationToken: token);
        }
    };
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
app.UseRateLimiter();

await app.UseOcelot();

app.Run();
