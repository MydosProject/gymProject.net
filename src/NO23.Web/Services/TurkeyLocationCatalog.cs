using System.Text.Json;
using System.Text.Json.Serialization;

namespace NO23.Web.Services;

public sealed class TurkeyLocationCatalog
{
    private const string DataFileName =
        "turkey-provinces-districts.json";

    private static readonly StringComparer LocationComparer =
        StringComparer.Create(
            System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),
            ignoreCase: true);

    private readonly IReadOnlyDictionary<string, HashSet<string>> _districtsByCity;

    public TurkeyLocationCatalog(IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var dataFilePath = Path.Combine(
            environment.WebRootPath,
            "data",
            DataFileName);

        var json = File.ReadAllText(dataFilePath);
        var provinces = JsonSerializer.Deserialize<List<ProvinceRecord>>(json)
            ?? throw new InvalidOperationException(
                "Türkiye il-ilçe veri dosyası okunamadı.");

        _districtsByCity = provinces.ToDictionary(
            province => province.City,
            province => province.Districts.ToHashSet(LocationComparer),
            LocationComparer);
    }

    public int ProvinceCount => _districtsByCity.Count;

    public int DistrictCount => _districtsByCity.Values.Sum(set => set.Count);

    public bool IsValid(string? city, string? district)
    {
        if (string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(district))
        {
            return false;
        }

        return _districtsByCity.TryGetValue(city.Trim(), out var districts) &&
               districts.Contains(district.Trim());
    }

    private sealed class ProvinceRecord
    {
        [JsonPropertyName("city")]
        public required string City { get; init; }

        [JsonPropertyName("districts")]
        public required List<string> Districts { get; init; }
    }
}
