using System.Text.Json;

namespace MovasseghiShop.Web.Services;

public record ReverseGeocodeResult(string Province, string City, string FullAddress, string DisplayName);

public interface IGeocodingService
{
    Task<ReverseGeocodeResult?> ReverseAsync(double lat, double lng, CancellationToken ct = default);
}

public class NominatimGeocodingService(IHttpClientFactory httpClientFactory, ILogger<NominatimGeocodingService> logger) : IGeocodingService
{
    const int TimeoutSeconds = 10;

    public async Task<ReverseGeocodeResult?> ReverseAsync(double lat, double lng, CancellationToken ct = default)
    {
        if (lat < -90 || lat > 90 || lng < -180 || lng > 180) return null;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

        try
        {
            var nominatim = await TryNominatimAsync(lat, lng, timeout.Token);
            if (nominatim != null) return nominatim;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Nominatim reverse geocode timed out for {Lat},{Lng}", lat, lng);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Nominatim reverse geocode failed for {Lat},{Lng}", lat, lng);
        }

        try
        {
            return await TryPhotonAsync(lat, lng, timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Photon reverse geocode failed for {Lat},{Lng}", lat, lng);
            return null;
        }
    }

    async Task<ReverseGeocodeResult?> TryNominatimAsync(double lat, double lng, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Nominatim");
        var url = $"reverse?lat={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&format=json&addressdetails=1&accept-language=fa";

        using var res = await client.GetAsync(url, ct);
        if (!res.IsSuccessStatusCode) return null;

        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        if (!root.TryGetProperty("address", out var addr)) return null;

        var province = FirstNonEmpty(addr, "state", "province", "region");
        var city = FirstNonEmpty(addr, "city", "town", "village", "municipality", "county", "state_district", "district");
        if (string.IsNullOrWhiteSpace(city))
            city = FirstNonEmpty(addr, "suburb", "neighbourhood");

        var streetParts = new[]
        {
            FirstNonEmpty(addr, "road", "pedestrian", "footway", "residential"),
            FirstNonEmpty(addr, "neighbourhood", "suburb", "quarter", "hamlet"),
            FirstNonEmpty(addr, "district", "city_district")
        }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

        var fullAddress = streetParts.Count > 0
            ? string.Join("، ", streetParts)
            : (root.TryGetProperty("display_name", out var dn) ? dn.GetString()?.Split(',').FirstOrDefault()?.Trim() : null) ?? "";

        if (string.IsNullOrWhiteSpace(province) && string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(fullAddress))
            return null;

        var display = root.TryGetProperty("display_name", out var displayName)
            ? displayName.GetString() ?? ""
            : "";

        return new ReverseGeocodeResult(province ?? "", city ?? province ?? "", fullAddress, display);
    }

    async Task<ReverseGeocodeResult?> TryPhotonAsync(double lat, double lng, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Photon");
        var url = $"reverse?lat={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        using var res = await client.GetAsync(url, ct);
        if (!res.IsSuccessStatusCode) return null;

        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("features", out var features) || features.GetArrayLength() == 0)
            return null;

        var props = features[0].GetProperty("properties");
        var province = GetProp(props, "state");
        var city = GetProp(props, "city") ?? GetProp(props, "district") ?? GetProp(props, "county");
        var street = GetProp(props, "street") ?? GetProp(props, "name");
        var suburb = GetProp(props, "locality") ?? GetProp(props, "district");

        var parts = new[] { street, suburb }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        var fullAddress = parts.Count > 0 ? string.Join("، ", parts) : street ?? suburb ?? "";

        if (string.IsNullOrWhiteSpace(province) && string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(fullAddress))
            return null;

        var display = string.Join("، ", new[] { province, city, fullAddress }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return new ReverseGeocodeResult(province ?? "", city ?? province ?? "", fullAddress, display);
    }

    static string? GetProp(JsonElement el, string key) =>
        el.TryGetProperty(key, out var v) ? v.GetString()?.Trim() : null;

    static string? FirstNonEmpty(JsonElement addr, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (addr.TryGetProperty(key, out var el))
            {
                var v = el.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
        }
        return null;
    }
}
