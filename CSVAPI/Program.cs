using CSVAPI.Database;
using CSVAPI.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=people.db")); // Or UseSqlServer for SQL Server
var app = builder.Build();


// Apply migrations on startup (optional, for development purposes)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
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

// Get all persons
app.MapGet("/persons", async (ApplicationDbContext dbContext) =>
    await dbContext.Persons.ToListAsync());

// Get person by ID
app.MapGet("/persons/{id}", async (int id, ApplicationDbContext dbContext) =>
    await dbContext.Persons.FindAsync(id) is Person person
        ? Results.Ok(person)
        : Results.NotFound());

// Create a new person
app.MapPost("/persons", async (Person person, ApplicationDbContext dbContext) =>
{
    dbContext.Persons.Add(person);
    await dbContext.SaveChangesAsync();
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

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
