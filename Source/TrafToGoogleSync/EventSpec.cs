namespace TrafToGoogleSync;

public record EventSpec(
	string Summary,
	DateTime Start,
	DateTime End,
	string? Description = "");