using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Serilog;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

/// <summary>
/// Fallout build for Papst.EventStore. Replaces the hand-written GitHub Actions workflows:
/// the <see cref="Test"/> target mirrors <c>test.yml</c> (PR) and the <see cref="Release"/> chain
/// mirrors <c>publish.yml</c> (push to main), including the "Skip CI" gate and — new — a wait until
/// the freshly published Contracts version is available on NuGet before the Stores are built/published.
/// </summary>
[GitHubActions(
    "test",
    GitHubActionsImage.UbuntuLatest,
    OnPullRequestBranches = ["main"],
    InvokedTargets = [nameof(Test)],
    FetchDepth = 0)]
[GitHubActions(
    "publish",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = ["main"],
    InvokedTargets = [nameof(Release)],
    // id-token: write -> the GitHub OIDC JWT exchanged for a short-lived NuGet key (Trusted Publishing).
    // contents: write -> creating the GitHub release.
    WritePermissions = [GitHubActionsPermissions.IdToken, GitHubActionsPermissions.Contents],
    EnableGitHubToken = true,
    ImportSecrets = [nameof(NuGetUser)],
    FetchDepth = 0)]
class Build : FalloutBuild
{
    public static int Main() => Execute<Build>(x => x.Test);

    // --- Parameters ------------------------------------------------------------------------------

    // Optional explicit NuGet API key. Normally left unset: on CI a short-lived key is minted via
    // NuGet Trusted Publishing (GitHub OIDC JWT -> nuget.org token exchange). See AcquireNuGetApiKeyAsync.
    [Parameter("Explicit NuGet API key (optional; CI mints a short-lived key via OIDC)")]
    [Secret]
    readonly string NuGetApiKey;

    // nuget.org username (profile name, NOT email) used in the OIDC token exchange. Provided via the
    // NUGET_USER GitHub secret. Not a publishing credential — the key is minted per run.
    [Parameter("nuget.org username for Trusted Publishing (OIDC)")]
    readonly string NuGetUser;

    [Parameter("GitHub token used to read PR titles and create the release")]
    [Secret]
    readonly string GitHubToken;

    // --- Constants / paths -----------------------------------------------------------------------

    const string NuGetSource = "https://api.nuget.org/v3/index.json";
    const string NuGetFlatContainer = "https://api.nuget.org/v3-flatcontainer";

    AbsolutePath ContractsSolution => RootDirectory / "Papst.EventStore.Contracts.slnx";
    AbsolutePath StoresSolution => RootDirectory / "Papst.EventStore.Stores.slnx";
    AbsolutePath FullSolution => RootDirectory / "Papst.EventStore.slnx";

    AbsolutePath ArtifactsContractsDirectory => RootDirectory / "artifacts.contracts";
    AbsolutePath ArtifactsStoresDirectory => RootDirectory / "artifacts";

    AbsolutePath[] ContractProjects =>
    [
        RootDirectory / "src" / "Papst.EventStore" / "Papst.EventStore.csproj",
        RootDirectory / "src" / "Papst.EventStore.Aggregation.EventRegistration" / "Papst.EventStore.Aggregation.EventRegistration.csproj",
        RootDirectory / "src" / "Papst.EventStore.CodeGeneration" / "Papst.EventStore.CodeGeneration.csproj",
    ];

    AbsolutePath[] StoreProjects =>
    [
        RootDirectory / "src" / "Papst.EventStore.AzureCosmos" / "Papst.EventStore.AzureCosmos.csproj",
        RootDirectory / "src" / "Papst.EventStore.EntityFrameworkCore" / "Papst.EventStore.EntityFrameworkCore.csproj",
        RootDirectory / "src" / "Papst.EventStore.FileSystem" / "Papst.EventStore.FileSystem.csproj",
        RootDirectory / "src" / "Papst.EventStore.InMemory" / "Papst.EventStore.InMemory.csproj",
        RootDirectory / "src" / "Papst.EventStore.MongoDB" / "Papst.EventStore.MongoDB.csproj",
    ];

    static readonly string[] ContractPackageIds =
    [
        "Papst.EventStore",
        "Papst.EventStore.Aggregation.EventRegistration",
        "Papst.EventStore.CodeGeneration",
    ];

    static readonly string[] StorePackageIds =
    [
        "Papst.EventStore.AzureCosmos",
        "Papst.EventStore.EntityFrameworkCore",
        "Papst.EventStore.FileSystem",
        "Papst.EventStore.InMemory",
        "Papst.EventStore.MongoDB",
    ];

    string _packageVersion;

    /// <summary>
    /// The NuGet package version for this build, computed by Nerdbank.GitVersioning (the same value
    /// baked into the packages at pack time). Resolved lazily via the nbgv CLI (restored from the
    /// dotnet tool manifest) so it is available to the wait/release targets.
    /// </summary>
    string PackageVersion => _packageVersion ??= ComputePackageVersion();

    string ComputePackageVersion()
    {
        var output = DotNet(
            "nbgv get-version -v NuGetPackageVersion",
            workingDirectory: RootDirectory,
            logOutput: false);

        string version = output
            .Select(o => o.Text?.Trim())
            .LastOrDefault(t => !string.IsNullOrWhiteSpace(t));

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException("Failed to compute the package version via nbgv.");
        }

        return version;
    }

    // --- Skip-CI gate ----------------------------------------------------------------------------

    bool? _skipCiRequested;

    /// <summary>
    /// True when the merged PR title contains "skip ci" or "skip-ci" (case-insensitive). Mirrors
    /// the old <c>check-skip</c> job: publishing is suppressed, build and test still run.
    /// </summary>
    bool SkipCiRequested => _skipCiRequested ??= ComputeSkipCiRequested();

    bool ShouldPublish => IsServerBuild && !SkipCiRequested;

    bool ComputeSkipCiRequested()
    {
        GitHubActions gha = GitHubActions.Instance;
        if (gha is null || string.IsNullOrWhiteSpace(gha.Sha))
        {
            return false;
        }

        try
        {
            (string owner, string repo) = SplitRepository(gha.Repository);
            using var http = CreateGitHubClient();
            using HttpResponseMessage response = http
                .GetAsync($"https://api.github.com/repos/{owner}/{repo}/commits/{gha.Sha}/pulls")
                .GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Could not read associated PRs ({Status}); assuming no Skip CI", response.StatusCode);
                return false;
            }

            string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using JsonDocument document = JsonDocument.Parse(json);
            List<string> titles = document.RootElement.EnumerateArray()
                .Select(pr => pr.TryGetProperty("title", out JsonElement t) ? t.GetString() ?? string.Empty : string.Empty)
                .ToList();

            bool skip = titles.Any(t => t.Replace('-', ' ').Contains("skip ci", StringComparison.OrdinalIgnoreCase));
            Log.Information("Associated PR titles: [{Titles}] -> SkipCI={Skip}", string.Join(", ", titles), skip);
            return skip;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to determine Skip CI from PR title; assuming no Skip CI");
            return false;
        }
    }

    // --- PR pipeline (mirrors test.yml): Debug build + test of the whole solution ----------------

    Target Test => _ => _
        .Description("Builds and tests the full solution in Debug (ProjectReferences, no NuGet needed).")
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(FullSolution)
                .SetConfiguration("Debug"));

            DotNetTest(s => s
                .SetProjectFile(FullSolution)
                .SetConfiguration("Debug")
                .EnableNoBuild());
        });

    // --- Publish pipeline (mirrors publish.yml): Release, PackageReferences, needs NuGet ---------

    Target CompileContracts => _ => _
        .Description("Builds the Contracts solution in Release.")
        .Executes(() => DotNetBuild(s => s
            .SetProjectFile(ContractsSolution)
            .SetConfiguration("Release")));

    Target TestContracts => _ => _
        .DependsOn(CompileContracts)
        .Executes(() => DotNetTest(s => s
            .SetProjectFile(ContractsSolution)
            .SetConfiguration("Release")
            .EnableNoBuild()));

    Target PackContracts => _ => _
        .DependsOn(TestContracts)
        .Executes(() =>
        {
            ArtifactsContractsDirectory.CreateOrCleanDirectory();
            foreach (AbsolutePath project in ContractProjects)
            {
                DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration("Release")
                    .EnableNoBuild()
                    .SetOutputDirectory(ArtifactsContractsDirectory));
            }
        });

    Target PublishContracts => _ => _
        .DependsOn(PackContracts)
        .OnlyWhenDynamic(() => ShouldPublish)
        .Requires(() => NuGetUser)
        .Executes(() => PushPackages(ArtifactsContractsDirectory));

    Target WaitForContracts => _ => _
        .Description("Polls NuGet until the just-published Contracts version is restorable.")
        .DependsOn(PublishContracts)
        .OnlyWhenDynamic(() => ShouldPublish)
        .Executes(() => WaitForPackagesAsync(ContractPackageIds, PackageVersion).GetAwaiter().GetResult());

    Target CompileStores => _ => _
        .Description("Builds the Stores solution in Release (restores the freshly published Contracts).")
        .DependsOn(WaitForContracts)
        .Executes(() =>
        {
            // Drop the NuGet HTTP cache so the [6.4.0,7.0) range re-resolves to the new Contracts.
            DotNet("nuget locals http-cache --clear");
            DotNetBuild(s => s
                .SetProjectFile(StoresSolution)
                .SetConfiguration("Release"));
        });

    Target TestStores => _ => _
        .DependsOn(CompileStores)
        .Executes(() => DotNetTest(s => s
            .SetProjectFile(StoresSolution)
            .SetConfiguration("Release")
            .EnableNoBuild()));

    Target PackStores => _ => _
        .DependsOn(TestStores)
        .Executes(() =>
        {
            ArtifactsStoresDirectory.CreateOrCleanDirectory();
            foreach (AbsolutePath project in StoreProjects)
            {
                DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration("Release")
                    .EnableNoBuild()
                    .SetOutputDirectory(ArtifactsStoresDirectory));
            }
        });

    Target PublishStores => _ => _
        .DependsOn(PackStores)
        .OnlyWhenDynamic(() => ShouldPublish)
        .Requires(() => NuGetUser)
        .Executes(() => PushPackages(ArtifactsStoresDirectory));

    Target WaitForStores => _ => _
        .Description("Polls NuGet until the just-published Store versions are restorable.")
        .DependsOn(PublishStores)
        .OnlyWhenDynamic(() => ShouldPublish)
        .Executes(() => WaitForPackagesAsync(StorePackageIds, PackageVersion).GetAwaiter().GetResult());

    Target Release => _ => _
        .Description("Creates the GitHub release for the published version.")
        .DependsOn(WaitForStores)
        .OnlyWhenDynamic(() => ShouldPublish)
        .Requires(() => GitHubToken)
        .Executes(() => CreateGitHubReleaseAsync().GetAwaiter().GetResult());

    // --- Helpers ---------------------------------------------------------------------------------

    string _resolvedNuGetApiKey;

    /// <summary>Key used for pushing: an explicit one if given, otherwise minted once via OIDC.</summary>
    string ResolvedNuGetApiKey => _resolvedNuGetApiKey ??= AcquireNuGetApiKeyAsync().GetAwaiter().GetResult();

    void PushPackages(AbsolutePath directory)
    {
        string apiKey = ResolvedNuGetApiKey;
        foreach (AbsolutePath package in directory.GlobFiles("*.nupkg"))
        {
            DotNetNuGetPush(s => s
                .SetTargetPath(package)
                .SetSource(NuGetSource)
                .SetApiKey(apiKey)
                .EnableSkipDuplicate());
        }
    }

    /// <summary>
    /// Obtains a NuGet API key. If <see cref="NuGetApiKey"/> is set it is used as-is; otherwise a
    /// short-lived key is minted via NuGet Trusted Publishing: request the GitHub Actions OIDC JWT
    /// (audience nuget.org) and exchange it at nuget.org's token endpoint for a ~1h API key.
    /// </summary>
    async Task<string> AcquireNuGetApiKeyAsync()
    {
        if (!string.IsNullOrWhiteSpace(NuGetApiKey))
        {
            return NuGetApiKey;
        }

        string requestUrl = Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_URL");
        string requestToken = Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_TOKEN");
        if (string.IsNullOrWhiteSpace(requestUrl) || string.IsNullOrWhiteSpace(requestToken))
        {
            throw new InvalidOperationException(
                "No NuGet API key available and GitHub OIDC is not enabled. " +
                "Grant 'id-token: write' to the job (WritePermissions IdToken) or pass --nuget-api-key.");
        }

        if (string.IsNullOrWhiteSpace(NuGetUser))
        {
            throw new InvalidOperationException("NuGetUser is required for Trusted Publishing (OIDC).");
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Papst.EventStore-Build", "1.0"));

        // 1. Ask GitHub for an OIDC token scoped to nuget.org.
        using var oidcRequest = new HttpRequestMessage(
            HttpMethod.Get, $"{requestUrl}&audience=https://www.nuget.org");
        oidcRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", requestToken);
        using HttpResponseMessage oidcResponse = await http.SendAsync(oidcRequest);
        oidcResponse.EnsureSuccessStatusCode();
        using JsonDocument oidcDocument = JsonDocument.Parse(await oidcResponse.Content.ReadAsStringAsync());
        string idToken = oidcDocument.RootElement.GetProperty("value").GetString();

        // 2. Exchange the OIDC JWT at nuget.org for a short-lived API key.
        using var exchange = new HttpRequestMessage(HttpMethod.Post, "https://www.nuget.org/api/v2/token");
        exchange.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
        exchange.Content = new StringContent(
            JsonSerializer.Serialize(new { username = NuGetUser, tokenType = "ApiKey" }),
            System.Text.Encoding.UTF8,
            "application/json");
        using HttpResponseMessage tokenResponse = await http.SendAsync(exchange);
        string tokenBody = await tokenResponse.Content.ReadAsStringAsync();
        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new Exception($"NuGet token exchange failed ({(int)tokenResponse.StatusCode}): {tokenBody}");
        }

        Log.Information("Obtained a short-lived NuGet API key via Trusted Publishing (OIDC)");
        using JsonDocument tokenDocument = JsonDocument.Parse(tokenBody);
        return tokenDocument.RootElement.GetProperty("apiKey").GetString();
    }

    async Task WaitForPackagesAsync(IReadOnlyList<string> packageIds, string version)
    {
        TimeSpan timeout = TimeSpan.FromMinutes(15);
        TimeSpan interval = TimeSpan.FromSeconds(15);
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Papst.EventStore-Build", "1.0"));

        foreach (string packageId in packageIds)
        {
            await WaitForPackageAsync(http, packageId, version, timeout, interval);
        }
    }

    static async Task WaitForPackageAsync(HttpClient http, string packageId, string version, TimeSpan timeout, TimeSpan interval)
    {
        string id = packageId.ToLowerInvariant();
        string url = $"{NuGetFlatContainer}/{id}/index.json";
        DateTime deadline = DateTime.UtcNow + timeout;
        int attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                using HttpResponseMessage response = await http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using JsonDocument document = JsonDocument.Parse(json);
                    bool available = document.RootElement.TryGetProperty("versions", out JsonElement versions)
                        && versions.EnumerateArray().Any(v =>
                            string.Equals(v.GetString(), version, StringComparison.OrdinalIgnoreCase));

                    if (available)
                    {
                        Log.Information("{Package} {Version} is available on NuGet (attempt {Attempt})", packageId, version, attempt);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Debug(e, "Polling {Package} failed on attempt {Attempt}", packageId, attempt);
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Package {packageId} {version} did not become available on NuGet within {timeout.TotalMinutes:0} minutes.");
            }

            Log.Information("Waiting for {Package} {Version} on NuGet (attempt {Attempt})...", packageId, version, attempt);
            await Task.Delay(interval);
        }
    }

    async Task CreateGitHubReleaseAsync()
    {
        GitHubActions gha = GitHubActions.Instance;
        (string owner, string repo) = SplitRepository(gha.Repository);
        string tag = $"v{PackageVersion}";

        using HttpClient http = CreateGitHubClient();
        var payload = new
        {
            tag_name = tag,
            name = tag,
            target_commitish = gha.Sha,
            generate_release_notes = true,
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await http.PostAsync(
            $"https://api.github.com/repos/{owner}/{repo}/releases",
            content);

        string body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create GitHub release ({(int)response.StatusCode}): {body}");
        }

        Log.Information("Created GitHub release {Tag}", tag);
    }

    HttpClient CreateGitHubClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Papst.EventStore-Build", "1.0"));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        if (!string.IsNullOrWhiteSpace(GitHubToken))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GitHubToken);
        }

        return http;
    }

    static (string Owner, string Repo) SplitRepository(string repository)
    {
        string[] parts = (repository ?? string.Empty).Split('/', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (string.Empty, repository ?? string.Empty);
    }
}
