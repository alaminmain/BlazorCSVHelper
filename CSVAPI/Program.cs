using CSVAPI.Database;
using CSVAPI.Entities;
using CSVAPI.Extensions;
using CSVAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=people.db")); // Or UseSqlServer for SQL Server

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
    {
        AbortOnConnectFail = true,
        EndPoints = { options.Configuration }
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient",
        policy =>
        {
            policy.WithOrigins("https://localhost:7169")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
var app = builder.Build();


// Apply migrations on startup (optional, for development purposes)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
   // dbContext.Database.Migrate();
    //if (!dbContext.Persons.Any())
    //{
    //    var mockData = MockDataGenerator.GenerateMockPersons(500000); // Generate 50 mock persons
    //    dbContext.Persons.AddRange(mockData);
    //    dbContext.SaveChanges();
    //}
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();


}
// ?? Use CORS before any endpoints
app.UseCors("AllowBlazorClient");
app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/persons", async (
    ApplicationDbContext dbContext,
    IDistributedCache cache,
    [FromQuery] string? search = null,
    [FromQuery] string? sortBy = null,
    [FromQuery] bool sortDesc = false,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10) =>
{
    var cacheKey = $"persons_{search}_{sortBy}_{sortDesc}_{page}_{pageSize}";

    var persons = await cache.GetOrSetAsync(
        cacheKey,
        async () =>
        {
            var query = dbContext.Persons.AsQueryable();

            // ? Global Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                query = query.Where(p =>
                    p.FirstName.Contains(keyword) ||
                    p.LastName.Contains(keyword) ||
                    p.Email.Contains(keyword)
                );
            }

            // ? Sorting
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                query = sortBy switch
                {
                    "FirstName" => sortDesc ? query.OrderByDescending(p => p.FirstName) : query.OrderBy(p => p.FirstName),
                    "LastName" => sortDesc ? query.OrderByDescending(p => p.LastName) : query.OrderBy(p => p.LastName),
                    "Email" => sortDesc ? query.OrderByDescending(p => p.Email) : query.OrderBy(p => p.Email),
                    _ => query
                };
            }

            // ? Pagination
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                Page = page,
                PageSize = pageSize,
                Items = items
            };
        });

    return Results.Ok(persons);
});

// Get person by ID
app.MapGet("/persons/{id}", async (int id, ApplicationDbContext dbContext, IDistributedCache cache) =>
{
    var cacheKey = $"person:{id}";
    var person = await cache.GetOrSetAsync(cacheKey,
        async () => await dbContext.Persons.FindAsync(id),
        new DistributedCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .SetAbsoluteExpiration(TimeSpan.FromHours(1)));
    return person is not null ? Results.Ok(person) : Results.NotFound();
});


// Create a new person
app.MapPost("/persons", async (Person person, ApplicationDbContext dbContext, IDistributedCache cache) =>
{
    dbContext.Persons.Add(person);
    await dbContext.SaveChangesAsync();
    // invalidate cache for products, as new product is added
    var cacheKey = "products";
   
    cache.Remove(cacheKey);

    return Results.Created($"/persons/{person.Id}", person);
});

// Update an existing person
app.MapPut("/persons/{id}", async (int id, Person updatedPerson, ApplicationDbContext dbContext) =>
{
    var person = await dbContext.Persons.FindAsync(id);
    if (person is null) return Results.NotFound();

    person.FirstName = updatedPerson.FirstName;
    person.LastName = updatedPerson.LastName;
    person.DateOfBirth = updatedPerson.DateOfBirth;
    person.Address = updatedPerson.Address;
    person.Email = updatedPerson.Email;
    person.PhoneNumber = updatedPerson.PhoneNumber;

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

// Delete a person
app.MapDelete("/persons/{id}", async (int id, ApplicationDbContext dbContext) =>
{
    var person = await dbContext.Persons.FindAsync(id);
    if (person is null) return Results.NotFound();

    dbContext.Persons.Remove(person);
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});


app.MapGet("/generate-csv", async (ApplicationDbContext dbContext) =>
{

    var data = await dbContext.Persons.ToListAsync();
    var csvContent = CSVGenerate.CreateCsv(data);
    var bytes = Encoding.UTF8.GetBytes(csvContent);
    return Results.File(bytes, "text/csv", "data.csv");
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
