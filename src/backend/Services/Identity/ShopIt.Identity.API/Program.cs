using ShopIt.Framework.Core.CQRS;
using ShopIt.Identity.API.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// request handlers
builder.Services.AddDispatcher();
builder.Services.AddRequestHandlers(typeof(Program).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/users", async (
    CreateUserCommand command,
    IDispatcher dispatcher,
    CancellationToken cancellationToken) =>
{
    var userId = await dispatcher.SendAsync(command, cancellationToken);
    return Results.Created($"/users/{userId}", new { Id = userId });
});

app.MapPost("/users/roles", async (
    IDispatcher dispatcher,
    CancellationToken cancellationToken) =>
{
    var message = await dispatcher.SendAsync(new AssignUserToRoleCommand(), cancellationToken);
    return Results.Ok(new { Message = message });
});

app.MapGet("/users/{userId}", async (
    Guid userId,
    IDispatcher dispatcher,
    CancellationToken cancellationToken) =>
{
    var user = await dispatcher.QueryAsync(new GetUserQuery(userId), cancellationToken);
    return Results.Ok(user);
});

app.Run();

