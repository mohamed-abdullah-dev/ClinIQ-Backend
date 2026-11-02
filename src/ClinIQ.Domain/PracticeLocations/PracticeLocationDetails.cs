using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.PracticeLocations;

// Physical details are separate from the doctor's fee and the location's review state.
public sealed record PracticeLocationDetails
{
    public string Name { get; }
    public string Address { get; }
    public decimal? Latitude { get; }
    public decimal? Longitude { get; }

    private PracticeLocationDetails(string name, string address, decimal? latitude, decimal? longitude)
    {
        Name = name; Address = address; Latitude = latitude; Longitude = longitude;
    }

    public static Result<PracticeLocationDetails> Create(string? name, string? address, decimal? latitude, decimal? longitude)
    {
        var trimmedName = name?.Trim() ?? ""; var trimmedAddress = address?.Trim() ?? "";
        var errors = new List<Error>();
        if (trimmedName.Length is < 1 or > 200)
            errors.Add(Error.Validation("PracticeLocation.Name", "Provide a place name of 1 to 200 characters.", "Name"));
        if (trimmedAddress.Length is < 1 or > 500)
            errors.Add(Error.Validation("PracticeLocation.Address", "Provide an address of 1 to 500 characters.", "Address"));
        if (latitude.HasValue != longitude.HasValue || latitude is < -90 or > 90 || longitude is < -180 or > 180 ||
            (latitude.HasValue && decimal.Round(latitude.Value, 9) != latitude.Value) ||
            (longitude.HasValue && decimal.Round(longitude.Value, 9) != longitude.Value))
            errors.Add(Error.Validation("PracticeLocation.Coordinates", "Provide both valid coordinates, with at most nine decimal places, or omit both.", "Coordinates"));
        if (errors.Count > 0) return errors;
        return new PracticeLocationDetails(trimmedName, trimmedAddress, latitude, longitude);
    }
}
