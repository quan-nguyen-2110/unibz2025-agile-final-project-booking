using Api.HostedServices;
using Application.Bookings.Commands;
using BookingService.Controllers;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Startup Initializer
builder.Services.AddHttpClient();
builder.Services.AddHostedService<StartupInitializer>();

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Convert enums to strings in JSON request/response bodies
    //options.JsonSerializerOptions.Converters.Add(
    //    new JsonStringEnumConverter(
    //        namingPolicy: null,
    //        allowIntegerValues: false
    //    )
    //);

    // Make enum parsing case-insensitive
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
}); ;

// Register MediatR handlers
//builder.Services.AddMediatR(cfg =>
//{
//    // Provide the assembly that contains your handlers
//    cfg.RegisterServicesFromAssembly(as);
//});

// Dependency Application
Application.DependencyInjection.AddApplication(builder.Services);

// Dependency Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Application & Infrastructure
//builder.Services.AddMediatR(typeof(GetAllApartmentsQuery).Assembly);
//builder.Services.AddValidatorsFromAssembly(typeof(CreateApartmentValidator).Assembly);
//builder.Services.AddInfrastructure(builder.Configuration);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("*") // frontend URL
              .AllowAnyHeader()
              .AllowAnyMethod(); // GET, POST, PUT, DELETE
    });
});

var app = builder.Build();

Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
app.UseSwagger();
app.UseSwaggerUI();
app.UseDeveloperExceptionPage();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    var retryCount = 0;
    while (retryCount < 10)
    {
        try
        {
            Console.WriteLine($"Migrate DB: {retryCount}");
            // Database.Migrate() will apply all pending migrations automatically when the app starts.
            // For development / staging environments only!
            // For production: prefer CI/CD migration step to have control and avoid accidental data loss.
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }
            break;
        }
        catch
        {
            retryCount++;
            Thread.Sleep(3000);
        }
    }
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend"); // Enable CORS

app.UseAuthorization();

app.MapControllers();

app.Run();

// docker-compose build api
// docker-compose up -d api

// dotnet ef migrations add InitialCreate -p Infrastructure -s Api
// dotnet ef database update -p Infrastructure -s Api
