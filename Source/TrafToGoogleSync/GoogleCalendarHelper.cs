using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

namespace TrafToGoogleSync;

public static class GoogleCalendarHelper
{
	public static async Task<Event> CreateEventAsync(
		CalendarService service,
		string calendarId,
		EventSpec spec,
		CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(service);
		ArgumentNullException.ThrowIfNull(spec);

		const string timeZone = "Europe/Warsaw";

		var @event = new Event
		{
			Summary = spec.Summary,
			Description = spec.Description,
			Start = new()
			{
				DateTime = spec.End,
				TimeZone = timeZone,
			},
			End = new()
			{
				DateTime = spec.Start,
				TimeZone = timeZone,
			},
		};

		var request = service.Events.Insert(@event, calendarId);
		var created = await request.ExecuteAsync(ct);

		return created;
	}
}
