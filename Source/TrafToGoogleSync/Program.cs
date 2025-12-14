using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Util.Store;
using TrafToGoogleSync;

Console.WriteLine(value: "TrafToGoogleSync - Google Calendar event creator");

const string appName = "TrafToGoogleSync";

var credPaths = new[]
{
	Path.Combine(
		path1: Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		appName,
		path3: "credentials.json"),

	Path.Combine(path1: Directory.GetCurrentDirectory(), path2: "credentials.json"),
};

var credentialsPath = credPaths.FirstOrDefault(File.Exists);

if (credentialsPath == null)
{
	Console.Error.WriteLine(
		value:
		"credentials.json not found. Please download your OAuth 2.0 Desktop credentials from Google Cloud Console and place the file at one of these locations:");

	foreach (var p in credPaths)
		Console.Error.WriteLine(value: "  " + p);

	return 2;
}

Console.WriteLine(value: "Using credentials: " + credentialsPath);

var scopes = new[] { CalendarService.Scope.Calendar };

var credFolder = Path.Combine(
	path1: Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
	appName,
	path3: "token-store");

Directory.CreateDirectory(credFolder);

UserCredential credential;

await using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
{
	var secrets = GoogleClientSecrets.FromStream(stream).Secrets;

	credential = await GoogleWebAuthorizationBroker
		.AuthorizeAsync(
			secrets,
			scopes,
			user: "user",
			CancellationToken.None,
			dataStore: new FileDataStore(credFolder, fullPath: true))
		.ConfigureAwait(continueOnCapturedContext: false);
}

using var service = new CalendarService(
	initializer: new()
	{
		HttpClientInitializer = credential,
		ApplicationName = appName,
	});

// Build a sample event (next hour)
var now = DateTime.UtcNow;
var start = now.AddHours(value: 1);
var end = start.AddHours(value: 1);

var spec = new EventSpec(
	Summary: "Sample sync event",
	start,
	end,
	Description: "Created by TrafToGoogleSync");

try
{
	var created = await GoogleCalendarHelper.CreateEventAsync(service, calendarId: "primary", spec);
	Console.WriteLine(value: "Event created: id=" + created.Id);
	Console.WriteLine(value: "Open in browser: " + created.HtmlLink);

	return 0;
}
catch (GoogleApiException gae)
{
	Console.Error.WriteLine(value: "Google API error: " + gae.Message);

	return 3;
}
catch (Exception ex)
{
	Console.Error.WriteLine(value: "Error creating event: " + ex.Message);

	return 4;
}
