using System.Net.Http.Json;
using BlazorApp.Models;

namespace BlazorApp.BlazorClient.Services
{
    /// <summary>
    /// Backs the Projects page's GitHub Activity card (#68). Reads the configured owner's
    /// display settings from #69's public endpoint, then fetches that owner's repos directly
    /// from GitHub's public, unauthenticated REST API - no ResumeFunctions involvement in the
    /// GitHub fetch itself. Takes two HttpClients deliberately: the API one is the DI-registered
    /// client that carries the session cookie handler (harmless here since this is an anonymous
    /// GET, but it's the only client with the API's base address); the GitHub one must NOT carry
    /// credentials, since GitHub's API responds with a wildcard CORS origin that the browser
    /// rejects outright on a credentialed request.
    /// </summary>
    public class GitHubActivityService
    {
        private readonly HttpClient _apiHttp;
        private readonly HttpClient _gitHubHttp;

        public GitHubActivityService(HttpClient apiHttp, HttpClient gitHubHttp)
        {
            _apiHttp = apiHttp;
            _gitHubHttp = gitHubHttp;
        }

        /// <summary>Returns null when the feature is off, unconfigured, or either fetch fails -
        /// callers should render nothing (not a broken/empty card) in every null case.</summary>
        public async Task<IReadOnlyList<GitHubRepoModel>?> GetActivityAsync()
        {
            var settings = await TryGetSettingsAsync();
            if (settings is null || !settings.Enabled || string.IsNullOrWhiteSpace(settings.GitHubUsername))
            {
                return null;
            }

            var repos = await GetReposForUsernameAsync(settings.GitHubUsername);
            if (repos is null)
            {
                return null;
            }

            return SelectRepos(repos, settings.PinnedRepoNames, settings.RepoCount);
        }

        private async Task<GitHubActivitySettingsDto?> TryGetSettingsAsync()
        {
            try
            {
                var response = await _apiHttp.GetAsync("api/github-activity-settings/public");
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<GitHubActivitySettingsDto>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Also used directly by the admin settings page (#68 follow-up) to populate its
        /// "pin from your repos" picker - returns null on any failure, same as the display path.</summary>
        public async Task<List<GitHubRepoModel>?> GetReposForUsernameAsync(string gitHubUsername)
        {
            try
            {
                var url = $"users/{Uri.EscapeDataString(gitHubUsername)}/repos?sort=pushed&per_page=100&type=owner";
                return await _gitHubHttp.GetFromJsonAsync<List<GitHubRepoModel>>(url);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Pinned repos first (in pinned order, skipping names that don't match any
        /// fetched repo - forks included, since pinning is a deliberate admin choice), then the
        /// remaining non-fork repos by most-recently-pushed, up to RepoCount total - pinned
        /// repos count toward that total, they don't add to it.</summary>
        internal static List<GitHubRepoModel> SelectRepos(
            IEnumerable<GitHubRepoModel> repos, IReadOnlyList<string> pinnedRepoNames, int repoCount)
        {
            var allRepos = repos.ToList();
            var byName = allRepos
                .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var nonForks = allRepos.Where(r => !r.Fork).ToList();

            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<GitHubRepoModel>();

            foreach (var pinnedName in pinnedRepoNames)
            {
                if (result.Count >= repoCount)
                {
                    break;
                }

                if (byName.TryGetValue(pinnedName, out var repo) && used.Add(repo.Name))
                {
                    result.Add(repo);
                }
            }

            var remaining = nonForks
                .Where(r => !used.Contains(r.Name))
                .OrderByDescending(r => r.PushedAt);

            foreach (var repo in remaining)
            {
                if (result.Count >= repoCount)
                {
                    break;
                }

                result.Add(repo);
                used.Add(repo.Name);
            }

            return result;
        }
    }
}
