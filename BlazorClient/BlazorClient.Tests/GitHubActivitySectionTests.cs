using System.Net;
using BlazorApp.BlazorClient.Pages;
using BlazorApp.BlazorClient.Services;
using BlazorClient.Tests.Helpers;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests;

public class GitHubActivitySectionTests : MudBunitTestContext
{
    private const string DisabledSettingsJson = """{"enabled":false,"gitHubUsername":null,"repoCount":5,"pinnedRepoNames":[]}""";

    private const string EnabledSettingsJson = """
        {"enabled":true,"gitHubUsername":"jem","repoCount":3,"pinnedRepoNames":["pinned-repo"]}
        """;

    private const string ReposJson = """
        [
          {"name":"pinned-repo","html_url":"https://github.com/jem/pinned-repo","description":"Pinned one","language":"C#","stargazers_count":2,"fork":false,"pushed_at":"2026-01-01T00:00:00Z"},
          {"name":"newest-repo","html_url":"https://github.com/jem/newest-repo","description":"Newest","language":"TypeScript","stargazers_count":5,"fork":false,"pushed_at":"2026-08-01T00:00:00Z"},
          {"name":"older-repo","html_url":"https://github.com/jem/older-repo","description":"Older","language":"Go","stargazers_count":1,"fork":false,"pushed_at":"2026-03-01T00:00:00Z"},
          {"name":"a-fork","html_url":"https://github.com/jem/a-fork","description":"Should be excluded","language":"C#","stargazers_count":9,"fork":true,"pushed_at":"2026-09-01T00:00:00Z"}
        ]
        """;

    private void RegisterService(string settingsJson, HttpStatusCode settingsStatus, string reposJson, HttpStatusCode reposStatus)
    {
        var apiHttp = new HttpClient(new FakeHttpHandler(settingsJson, settingsStatus)) { BaseAddress = new Uri("http://localhost") };
        var gitHubHttp = new HttpClient(new FakeHttpHandler(reposJson, reposStatus)) { BaseAddress = new Uri("https://api.github.test/") };
        Services.AddScoped(_ => new GitHubActivityService(apiHttp, gitHubHttp));
    }

    [Fact]
    public void RendersNothing_WhenSettingsDisabled()
    {
        RegisterService(DisabledSettingsJson, HttpStatusCode.OK, ReposJson, HttpStatusCode.OK);

        var cut = RenderComponent<GitHubActivitySection>();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Loading GitHub activity", cut.Markup));
        Assert.DoesNotContain("GitHub Activity", cut.Markup);
    }

    [Fact]
    public void RendersNothing_WhenSettingsFetchReturnsNotFound()
    {
        RegisterService("{}", HttpStatusCode.NotFound, ReposJson, HttpStatusCode.OK);

        var cut = RenderComponent<GitHubActivitySection>();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Loading GitHub activity", cut.Markup));
        Assert.DoesNotContain("GitHub Activity", cut.Markup);
    }

    [Fact]
    public void RendersNothing_WhenGitHubApiFails()
    {
        RegisterService(EnabledSettingsJson, HttpStatusCode.OK, "rate limited", HttpStatusCode.Forbidden);

        var cut = RenderComponent<GitHubActivitySection>();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Loading GitHub activity", cut.Markup));
        Assert.DoesNotContain("GitHub Activity", cut.Markup);
    }

    [Fact]
    public void ShowsLoadingState_BeforeSettingsRespond()
    {
        var blockingHandler = new BlockingFakeHttpHandler();
        var apiHttp = new HttpClient(blockingHandler) { BaseAddress = new Uri("http://localhost") };
        var gitHubHttp = new HttpClient(new FakeHttpHandler(ReposJson)) { BaseAddress = new Uri("https://api.github.test/") };
        Services.AddScoped(_ => new GitHubActivityService(apiHttp, gitHubHttp));

        var cut = RenderComponent<GitHubActivitySection>();

        Assert.Contains("Loading GitHub activity", cut.Markup);
        blockingHandler.Complete(DisabledSettingsJson);
    }

    [Fact]
    public void RendersPinnedRepoFirst_ThenFillsRemainingSlotsByMostRecentlyPushed_ExcludingForks()
    {
        RegisterService(EnabledSettingsJson, HttpStatusCode.OK, ReposJson, HttpStatusCode.OK);

        var cut = RenderComponent<GitHubActivitySection>();

        cut.WaitForAssertion(() => Assert.Contains("pinned-repo", cut.Markup));

        var names = cut.FindAll("li.github-activity-item a").Select(a => a.TextContent).ToList();

        // repoCount is 3: pinned-repo (pinned) then the two most-recently-pushed non-forks
        // (newest-repo, older-repo) - a-fork is excluded despite the most recent push date.
        Assert.Equal(new[] { "pinned-repo", "newest-repo", "older-repo" }, names);
        Assert.DoesNotContain("a-fork", cut.Markup);
    }

    [Fact]
    public void RendersDescriptionAndLanguage_ButNotStars_ForEachRepo()
    {
        RegisterService(EnabledSettingsJson, HttpStatusCode.OK, ReposJson, HttpStatusCode.OK);

        var cut = RenderComponent<GitHubActivitySection>();

        cut.WaitForAssertion(() => Assert.Contains("pinned-repo", cut.Markup));
        Assert.Contains("Pinned one", cut.Markup);
        Assert.Contains("C#", cut.Markup);
        Assert.DoesNotContain("github-activity-stars", cut.Markup);

        var link = cut.Find("li.github-activity-item a");
        Assert.Equal("https://github.com/jem/pinned-repo", link.GetAttribute("href"));
        Assert.Equal("_new", link.GetAttribute("target"));
    }

    [Fact]
    public void RendersLanguage_WhenRepoHasNoDescription()
    {
        const string reposWithNoDescription = """
            [
              {"name":"no-desc-repo","html_url":"https://github.com/jem/no-desc-repo","description":null,"language":"C#","stargazers_count":0,"fork":false,"pushed_at":"2026-01-01T00:00:00Z"}
            ]
            """;
        RegisterService(EnabledSettingsJson, HttpStatusCode.OK, reposWithNoDescription, HttpStatusCode.OK);

        var cut = RenderComponent<GitHubActivitySection>();

        cut.WaitForAssertion(() => Assert.Contains("no-desc-repo", cut.Markup));
        Assert.DoesNotContain("github-activity-description", cut.Markup);
        Assert.Contains("github-activity-meta", cut.Markup);
        Assert.Contains("C#", cut.Markup);
    }
}
