using Cinematch.Components;
using Cinematch.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
        options.DetailedErrors = builder.Environment.IsDevelopment());

builder.Services.AddSingleton<MovieCatalog>();
builder.Services.AddSingleton<GridStore>();

var app = builder.Build();

// Load the data now, so a missing or broken CSV fails at startup, not on a visitor's first click
app.Services.GetRequiredService<MovieCatalog>();
app.Services.GetRequiredService<GridStore>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. See https://aka.ms/aspnetcore-hsts for production.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();