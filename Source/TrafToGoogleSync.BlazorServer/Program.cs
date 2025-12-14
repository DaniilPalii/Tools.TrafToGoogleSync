using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrafToGoogleSync.BlazorServer.Components;
using TrafToGoogleSync.BlazorServer.Components.Account;
using TrafToGoogleSync.BlazorServer.Data;

var builder = WebApplication.CreateBuilder(args);

builder
	.Services
	.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder
	.Services
	.AddAuthentication(
		configureOptions: options =>
		{
			options.DefaultScheme = IdentityConstants.ApplicationScheme;
			options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
		})
	.AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString(name: "DefaultConnection")
	?? throw new InvalidOperationException(message: "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(
	optionsAction: options =>
		options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder
	.Services
	.AddIdentityCore<ApplicationUser>(
		setupAction: options =>
		{
			options.SignIn.RequireConfirmedAccount = true;
			options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
		})
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddSignInManager()
	.AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler(errorHandlingPath: "/Error", createScopeForErrors: true);

	// The default HSTS value is 30 days. You may want to change this for production scenarios, see
	// https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(pathFormat: "/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app
	.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
