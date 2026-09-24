using Google.Apis.Auth.OAuth2;
using Google.Apis.SearchConsole.v1;
using Google.Apis.SearchConsole.v1.Data;
using Google.Apis.Services;

namespace MovasseghiShop.Web.Services;

public class GscQueryRow
{
    public string Query { get; set; } = "";
    public double Clicks { get; set; }
    public double Impressions { get; set; }
    public double Ctr { get; set; }
    public double Position { get; set; }
}

public class GscSyncResult
{
    public bool IsConfigured { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int QueriesImported { get; set; }
    public List<GscQueryRow> TopQueries { get; set; } = [];
}

public interface IGoogleSearchConsoleService
{
    bool IsConfigured { get; }
    string? PropertyUrl { get; }
    string? SetupHint { get; }
    Task<GscSyncResult> SyncTopQueriesAsync(CancellationToken ct = default);
    Task<GscSyncResult> GetTopQueriesAsync(int days = 28, CancellationToken ct = default);
}

public class GoogleSearchConsoleService(
    IConfiguration config,
    IWebHostEnvironment env,
    IRankTrackerService rankTracker,
    ILogger<GoogleSearchConsoleService> log) : IGoogleSearchConsoleService
{
    public bool IsConfigured => TryGetCredential(out _, out _);
    public string? PropertyUrl
    {
        get
        {
            var url = config["GoogleSearchConsole:PropertyUrl"]?.Trim();
            if (!string.IsNullOrWhiteSpace(url)) return url;
            var baseUrl = config["SiteSettings:PublicBaseUrl"]?.Trim();
            if (string.IsNullOrWhiteSpace(baseUrl)) return null;
            return baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        }
    }

    public string? SetupHint
    {
        get
        {
            if (IsConfigured) return null;
            return "Google Search Console: PropertyUrl و ServiceAccountKeyPath را در appsettings تنظیم کنید، "
                   + "سپس ایمیل Service Account را در GSC به‌عنوان Owner اضافه کنید.";
        }
    }

    public async Task<GscSyncResult> SyncTopQueriesAsync(CancellationToken ct = default)
    {
        var result = await GetTopQueriesAsync(28, ct);
        if (!result.Success || result.TopQueries.Count == 0)
            return result;

        foreach (var row in result.TopQueries)
        {
            var pos = (int)Math.Round(row.Position);
            if (pos < 1) pos = 0;
            await rankTracker.RecordSnapshotAsync(row.Query, pos, "gsc", null, null);
        }

        result.QueriesImported = result.TopQueries.Count;
        result.Message = $"{result.QueriesImported} کلمه از Google Search Console وارد Rank Radar شد.";
        return result;
    }

    public async Task<GscSyncResult> GetTopQueriesAsync(int days = 28, CancellationToken ct = default)
    {
        var result = new GscSyncResult { IsConfigured = IsConfigured };

        if (!IsConfigured)
        {
            result.Message = SetupHint;
            return result;
        }

        var property = PropertyUrl!;
        if (string.IsNullOrWhiteSpace(property))
        {
            result.Message = "GoogleSearchConsole:PropertyUrl خالی است (مثال: https://example.com/ یا sc-domain:example.com).";
            return result;
        }

        try
        {
            var service = CreateService();
            var end = DateTime.UtcNow.Date;
            var start = end.AddDays(-Math.Clamp(days, 7, 90));

            var query = new SearchAnalyticsQueryRequest
            {
                StartDate = start.ToString("yyyy-MM-dd"),
                EndDate = end.ToString("yyyy-MM-dd"),
                Dimensions = ["query"],
                RowLimit = 50,
                DataState = "all"
            };

            var response = await service.Searchanalytics.Query(query, property).ExecuteAsync(ct);
            result.TopQueries = (response.Rows ?? [])
                .Select(r => new GscQueryRow
                {
                    Query = r.Keys?.FirstOrDefault() ?? "",
                    Clicks = r.Clicks ?? 0,
                    Impressions = r.Impressions ?? 0,
                    Ctr = r.Ctr ?? 0,
                    Position = r.Position ?? 0
                })
                .Where(r => !string.IsNullOrWhiteSpace(r.Query))
                .OrderByDescending(r => r.Impressions)
                .ToList();

            result.Success = true;
            result.Message = result.TopQueries.Count > 0
                ? $"{result.TopQueries.Count} کلمه از GSC (۲۸ روز اخیر)."
                : "GSC متصل است ولی داده‌ای برنگشت — PropertyUrl را بررسی کنید.";
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "GSC query failed");
            result.Message = $"خطا در GSC: {ex.Message}";
        }

        return result;
    }

    SearchConsoleService CreateService()
    {
        if (!TryGetCredential(out var credential, out var error))
            throw new InvalidOperationException(error);

        return new SearchConsoleService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Movasseghi Shop Admin"
        });
    }

    bool TryGetCredential(out GoogleCredential? credential, out string? error)
    {
        credential = null;
        error = null;

        var keyPath = config["GoogleSearchConsole:ServiceAccountKeyPath"]?.Trim();
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            error = "ServiceAccountKeyPath تنظیم نشده.";
            return false;
        }

        var fullPath = Path.IsPathRooted(keyPath) ? keyPath : Path.Combine(env.ContentRootPath, keyPath);
        if (!File.Exists(fullPath))
        {
            error = $"فایل کلید یافت نشد: {fullPath}";
            return false;
        }

        credential = GoogleCredential.FromFile(fullPath)
            .CreateScoped(SearchConsoleService.Scope.WebmastersReadonly);
        return true;
    }
}
