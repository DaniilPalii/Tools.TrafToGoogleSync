using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using TrafToGoogleSync.BlazorServer.Components.Account.Pages;
using TrafToGoogleSync.BlazorServer.Components.Account.Pages.Manage;
using TrafToGoogleSync.BlazorServer.Data;

namespace Microsoft.AspNetCore.Routing;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
	// These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory
	// of this project.
	public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
	{
		ArgumentNullException.ThrowIfNull(endpoints);

		var accountGroup = endpoints.MapGroup(prefix: "/Account");

		accountGroup.MapPost(
			pattern: "/PerformExternalLogin",
			handler: (
				HttpContext context,
				[FromServices] SignInManager<ApplicationUser> signInManager,
				[FromForm] string provider,
				[FromForm] string returnUrl) =>
			{
				IEnumerable<KeyValuePair<string, StringValues>> query =
				[
					new(key: "ReturnUrl", returnUrl),
					new(key: "Action", ExternalLogin.LoginCallbackAction),
				];

				var redirectUrl = UriHelper.BuildRelative(
					context.Request.PathBase,
					path: "/Account/ExternalLogin",
					query: QueryString.Create(query));

				var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

				return TypedResults.Challenge(properties, authenticationSchemes: [provider]);
			});

		accountGroup.MapPost(
			pattern: "/Logout",
			handler: async (
				ClaimsPrincipal user,
				[FromServices] SignInManager<ApplicationUser> signInManager,
				[FromForm] string returnUrl) =>
			{
				await signInManager.SignOutAsync();

				return TypedResults.LocalRedirect(localUrl: $"~/{returnUrl}");
			});

		accountGroup.MapPost(
			pattern: "/PasskeyCreationOptions",
			handler: async (
				HttpContext context,
				[FromServices] UserManager<ApplicationUser> userManager,
				[FromServices] SignInManager<ApplicationUser> signInManager,
				[FromServices] IAntiforgery antiforgery) =>
			{
				await antiforgery.ValidateRequestAsync(context);

				var user = await userManager.GetUserAsync(context.User);

				if (user is null)
				{
					return Results.NotFound(
						value: $"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
				}

				var userId = await userManager.GetUserIdAsync(user);
				var userName = await userManager.GetUserNameAsync(user) ?? "User";

				var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(
					userEntity: new()
					{
						Id = userId,
						Name = userName,
						DisplayName = userName,
					});

				return TypedResults.Content(optionsJson, contentType: "application/json");
			});

		accountGroup.MapPost(
			pattern: "/PasskeyRequestOptions",
			handler: async (
				HttpContext context,
				[FromServices] UserManager<ApplicationUser> userManager,
				[FromServices] SignInManager<ApplicationUser> signInManager,
				[FromServices] IAntiforgery antiforgery,
				[FromQuery] string? username) =>
			{
				await antiforgery.ValidateRequestAsync(context);

				var user = string.IsNullOrEmpty(username) ? null : await userManager.FindByNameAsync(username);
				var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);

				return TypedResults.Content(optionsJson, contentType: "application/json");
			});

		var manageGroup = accountGroup.MapGroup(prefix: "/Manage").RequireAuthorization();

		manageGroup.MapPost(
			pattern: "/LinkExternalLogin",
			handler: async (
				HttpContext context,
				[FromServices] SignInManager<ApplicationUser> signInManager,
				[FromForm] string provider) =>
			{
				// Clear the existing external cookie to ensure a clean login process
				await context.SignOutAsync(IdentityConstants.ExternalScheme);

				var redirectUrl = UriHelper.BuildRelative(
					context.Request.PathBase,
					path: "/Account/Manage/ExternalLogins",
					query: QueryString.Create(name: "Action", ExternalLogins.LinkLoginCallbackAction));

				var properties = signInManager.ConfigureExternalAuthenticationProperties(
					provider,
					redirectUrl,
					userId: signInManager.UserManager.GetUserId(context.User));

				return TypedResults.Challenge(properties, authenticationSchemes: [provider]);
			});

		var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
		var downloadLogger = loggerFactory.CreateLogger(categoryName: "DownloadPersonalData");

		manageGroup.MapPost(
			pattern: "/DownloadPersonalData",
			handler: async (
				HttpContext context,
				[FromServices] UserManager<ApplicationUser> userManager,
				[FromServices] AuthenticationStateProvider authenticationStateProvider) =>
			{
				var user = await userManager.GetUserAsync(context.User);

				if (user is null)
				{
					return Results.NotFound(
						value: $"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
				}

				var userId = await userManager.GetUserIdAsync(user);

				downloadLogger.LogInformation(
					message: "User with ID '{UserId}' asked for their personal data.",
					userId);

				// Only include personal data for download
				var personalData = new Dictionary<string, string>();

				var personalDataProps = typeof(ApplicationUser)
					.GetProperties()
					.Where(predicate: prop => Attribute.IsDefined(prop, attributeType: typeof(PersonalDataAttribute)));

				foreach (var p in personalDataProps)
				{
					personalData.Add(p.Name, value: p.GetValue(user)?.ToString() ?? "null");
				}

				var logins = await userManager.GetLoginsAsync(user);

				foreach (var l in logins)
				{
					personalData.Add(key: $"{l.LoginProvider} external login provider key", l.ProviderKey);
				}

				personalData.Add(key: "Authenticator Key", value: (await userManager.GetAuthenticatorKeyAsync(user))!);
				var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

				context.Response.Headers.TryAdd(
					key: "Content-Disposition",
					value: "attachment; filename=PersonalData.json");

				return TypedResults.File(
					fileBytes,
					contentType: "application/json",
					fileDownloadName: "PersonalData.json");
			});

		return accountGroup;
	}
}
