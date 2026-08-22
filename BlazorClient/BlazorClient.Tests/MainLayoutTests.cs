using BlazorApp.BlazorClient.Layout;
using BlazorApp.BlazorClient.Services;
using BlazorClient.Tests.Helpers;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Xunit;

namespace BlazorClient.Tests;

public class MainLayoutTests : MudBunitTestContext
{
    private void RegisterHttpClient()
    {
        var handler = new FakeHttpHandler("{}");
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<JwtAuthenticationStateProvider>();
        Services.AddScoped(sp => new AuthenticationService(client, sp.GetRequiredService<JwtAuthenticationStateProvider>()));
    }

    // The desktop Admin menu is a MudMenu (popover-based), which needs MudPopoverProvider
    // rendered - App.razor provides it at the app root, but MainLayout is rendered
    // standalone here. Must be called last, after all service registration: rendering
    // anything locks bUnit's service collection against further AddScoped/AddSingleton
    // calls (e.g. AddTestAuthorization's).
    private void RenderPopoverProvider() => RenderComponent<MudPopoverProvider>();

    [Fact]
    public void Visitor_DoesNotSeeAdminNavGroup()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("jane");
        authContext.SetRoles("visitor");
        RenderPopoverProvider();

        var cut = RenderComponent<MainLayout>();

        Assert.DoesNotContain("Manage Resumes", cut.Markup);
        Assert.DoesNotContain("GitHub Activity Settings", cut.Markup);
        Assert.DoesNotContain("Manage Project Listings", cut.Markup);
    }

    [Fact]
    public void ResumeAdmin_SeesManageResumesButNotProjectListings()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");
        RenderPopoverProvider();

        var cut = RenderComponent<MainLayout>();

        Assert.Contains("Manage Resumes", cut.Markup);
        Assert.Contains("GitHub Activity Settings", cut.Markup);
        Assert.DoesNotContain("Manage Project Listings", cut.Markup);
    }

    [Fact]
    public void SuperAdmin_SeesManageResumesAndProjectListings()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("root");
        authContext.SetRoles("superadmin");
        RenderPopoverProvider();

        var cut = RenderComponent<MainLayout>();

        Assert.Contains("Manage Resumes", cut.Markup);
        Assert.Contains("GitHub Activity Settings", cut.Markup);
        Assert.Contains("Manage Project Listings", cut.Markup);
    }
}
