using System.Net;
using BlazorApp.BlazorClient.Pages.Admin;
using BlazorApp.BlazorClient.Services;
using BlazorClient.Tests.Helpers;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Xunit;

namespace BlazorClient.Tests;

public class GitHubActivitySettingsPageTests : MudBunitTestContext
{
    private const string MineJson = """
        {"enabled":true,"gitHubUsername":"justin-mann","repoCount":5,"pinnedRepoNames":["406jem.2026"]}
        """;

    private const string SavedJson = """
        {"enabled":false,"gitHubUsername":"justin-mann","repoCount":8,"pinnedRepoNames":["406jem.2026","another-repo"]}
        """;

    private const string ReposJson = """
        [
          {"name":"406jem.2026","html_url":"https://github.com/justin-mann/406jem.2026","description":null,"language":"C#","stargazers_count":0,"fork":false,"pushed_at":"2026-01-01T00:00:00Z"},
          {"name":"another-repo","html_url":"https://github.com/justin-mann/another-repo","description":"Another one","language":"TypeScript","stargazers_count":1,"fork":false,"pushed_at":"2026-02-01T00:00:00Z"},
          {"name":"a-fork","html_url":"https://github.com/justin-mann/a-fork","description":"Excluded","language":"Go","stargazers_count":9,"fork":true,"pushed_at":"2026-03-01T00:00:00Z"}
        ]
        """;

    private RoutedFakeHttpHandler RegisterHttpClient(HttpStatusCode putStatusCode = HttpStatusCode.OK)
    {
        var handler = new RoutedFakeHttpHandler()
            .When(HttpMethod.Get, "github-activity-settings/mine", MineJson)
            .When(HttpMethod.Put, "github-activity-settings/mine", SavedJson, putStatusCode);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);

        var gitHubHttp = new HttpClient(new FakeHttpHandler(ReposJson)) { BaseAddress = new Uri("https://api.github.test/") };
        Services.AddScoped(_ => new GitHubActivityService(client, gitHubHttp));

        return handler;
    }

    // The available-repos MudSelect is popover-based, which needs MudPopoverProvider rendered -
    // App.razor provides it at the app root, but the page is rendered standalone here. Must be
    // called last, after all service registration (see MainLayoutTests for why). Its return
    // value is a separate render root from the page under test - popover content (e.g. a
    // MudSelect's open dropdown) shows up in *this* markup, not the page's.
    private IRenderedComponent<MudPopoverProvider> RenderPopoverProvider() => RenderComponent<MudPopoverProvider>();

    [Fact]
    public void NonAdmin_SeesAccessRequiredMessage()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("jane");
        authContext.SetRoles("visitor");
        RenderPopoverProvider();

        var cut = RenderComponent<GitHubActivitySettings>();

        Assert.Contains("Resume Admin access required", cut.Markup);
        Assert.DoesNotContain("github-activity-save-btn", cut.Markup);
    }

    [Fact]
    public void Admin_SeesLoadedSettings_WithPinnedRepo()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");
        RenderPopoverProvider();

        var cut = RenderComponent<GitHubActivitySettings>();

        cut.WaitForAssertion(() => Assert.Contains("406jem.2026", cut.Markup));
        Assert.Contains("github-activity-save-btn", cut.Markup);
    }

    [Fact]
    public void Admin_AddsAndRemovesPinnedRepo_ByName()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");
        RenderPopoverProvider();

        var cut = RenderComponent<GitHubActivitySettings>();
        cut.WaitForAssertion(() => Assert.Contains("github-activity-pinned-add-btn", cut.Markup));

        cut.Find("input[placeholder='Or add by name']").Input("new-repo");
        cut.Find("button.github-activity-pinned-add-btn").Click();

        Assert.Equal(2, cut.FindAll(".github-activity-pinned-item").Count);
        Assert.Contains("new-repo", cut.Markup);

        cut.FindAll("button.github-activity-pinned-remove-btn")[0].Click();

        Assert.Single(cut.FindAll(".github-activity-pinned-item"));
        Assert.DoesNotContain("406jem.2026", cut.FindAll(".github-activity-pinned-item")[0].TextContent);
    }

    [Fact]
    public void Admin_ReordersPinnedRepos()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");
        RenderPopoverProvider();

        var cut = RenderComponent<GitHubActivitySettings>();
        cut.WaitForAssertion(() => Assert.Contains("github-activity-pinned-add-btn", cut.Markup));

        cut.Find("input[placeholder='Or add by name']").Input("second-repo");
        cut.Find("button.github-activity-pinned-add-btn").Click();

        var items = cut.FindAll(".github-activity-pinned-item");
        Assert.Equal("406jem.2026", items[0].TextContent.Trim().Split('\n')[0].Trim());

        cut.FindAll("button.github-activity-pinned-down-btn")[0].Click();

        items = cut.FindAll(".github-activity-pinned-item");
        Assert.Contains("second-repo", items[0].TextContent);
    }

    [Fact]
    public void Admin_PinsRepoFromAvailableReposSelect()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");
        var popovers = RenderPopoverProvider();

        var cut = RenderComponent<GitHubActivitySettings>();
        cut.WaitForAssertion(() => Assert.Contains("github-activity-available-repos-select", cut.Markup));

        // MudSelect's items are popover content, rendered under MudPopoverProvider's own
        // render root rather than the page's - not in cut.Markup even once opened.
        cut.Find(".github-activity-available-repos-select input").Click();
        popovers.WaitForAssertion(() => Assert.Contains("another-repo", popovers.Markup));

        // Forks ARE offered here - unlike the display page's automatic fill, pinning is a
        // deliberate admin choice, so all public repos (including forks) should be pickable.
        Assert.Contains("a-fork", popovers.Markup);
    }

    [Fact]
    public void Admin_SavesSettings_PutsAndShowsSuccess()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");
        RenderPopoverProvider();

        var cut = RenderComponent<GitHubActivitySettings>();
        cut.WaitForAssertion(() => Assert.Contains("github-activity-save-btn", cut.Markup));

        cut.Find("button.github-activity-save-btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Put && r.RequestUri!.ToString().Contains("github-activity-settings/mine")));
        cut.WaitForAssertion(() => Assert.Contains("GitHub Activity settings saved.", cut.Markup));
    }

    [Fact]
    public void Admin_SaveFailure_ShowsErrorMessage()
    {
        RegisterHttpClient(HttpStatusCode.BadRequest);
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");
        RenderPopoverProvider();

        var cut = RenderComponent<GitHubActivitySettings>();
        cut.WaitForAssertion(() => Assert.Contains("github-activity-save-btn", cut.Markup));

        cut.Find("button.github-activity-save-btn").Click();

        cut.WaitForAssertion(() => Assert.Contains("Could not save your GitHub Activity settings", cut.Markup));
    }
}
