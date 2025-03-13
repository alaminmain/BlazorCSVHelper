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

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/persons", async (ApplicationDbContext dbContext, IDistributedCache cache,
    [FromQuery] string? searchFields = null,
    [FromQuery] string? searchValues = null,
    [FromQuery] string? sortBy = null,
    [FromQuery] string? sortDesc = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10) =>
{
    var searchFieldsList = searchFields?.Split(',').ToList();
    var searchValuesList = searchValues?.Split(',').ToList();
    var sortByList = sortBy?.Split(',').ToList();
    var sortDescList = sortDesc?.Split(',').Select(bool.Parse).ToList();

    var cacheKey = $"persons_{string.Join("_", searchFieldsList ?? new List<string>())}_{string.Join("_", searchValuesList ?? new List<string>())}_{string.Join("_", sortByList ?? new List<string>())}_{string.Join("_", sortDescList ?? new List<bool>())}_{page}_{pageSize}";
    var persons = await cache.GetOrSetAsync(
        cacheKey,
        async () =>
        {
            var query = dbContext.Persons.AsQueryable();

            // Apply search filters
            if (searchFieldsList != null && searchValuesList != null && searchFieldsList.Count == searchValuesList.Count)
            {
                for (int i = 0; i < searchFieldsList.Count; i++)
                {
                    var field = searchFieldsList[i];
                    var value = searchValuesList[i];
                    query = field switch
                    {
                        "FirstName" => query.Where(p => p.FirstName.Contains(value)),
                        "LastName" => query.Where(p => p.LastName.Contains(value)),
                        "Email" => query.Where(p => p.Email.Contains(value)),
                        _ => query
                    };
                }
            }

            // Apply sorting
            if (sortByList != null && sortDescList != null && sortByList.Count == sortDescList.Count)
            {
                for (int i = 0; i < sortByList.Count; i++)
                {
                    var field = sortByList[i];
                    var desc = sortDescList[i];
                    query = field switch
                    {
                        "FirstName" => desc ? query.OrderByDescending(p => p.FirstName) : query.OrderBy(p => p.FirstName),
                        "LastName" => desc ? query.OrderByDescending(p => p.LastName) : query.OrderBy(p => p.LastName),
                        "Email" => desc ? query.OrderByDescending(p => p.Email) : query.OrderBy(p => p.Email),
                        _ => query
                    };
                }
            }

            // Apply pagination
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            //return items;
            return new
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = items
            };
        }
    )!;

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
