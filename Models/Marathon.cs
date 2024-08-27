namespace OengusWatcher.Models;

public record MarathonsList(
    Marathon[] Live,
    Marathon[] Next,
    Marathon[] Open
);

public record Marathon(
    string Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    DateTime? SubmissionsEndDate,
    bool Onsite,
    string Location,
    string Country,
    string Language,
    bool Private
);
