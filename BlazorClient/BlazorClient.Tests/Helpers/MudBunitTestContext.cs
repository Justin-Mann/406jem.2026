using Bunit;
using MudBlazor.Services;

namespace BlazorClient.Tests.Helpers;

/// <summary>
/// bUnit TestContext base for any component under test that renders MudBlazor
/// components — MudBlazor's input/interop components resolve services (popover,
/// key interceptor, resize observer, etc.) from DI, which AddMudServices() registers.
/// Note: a popover-based component under test (MudMenu, MudSelect, MudDialog, ...)
/// additionally needs `RenderComponent&lt;MudPopoverProvider&gt;()` called in the test
/// itself, after any test-specific service registration - rendering it eagerly here
/// in the constructor locks bUnit's service collection before derived tests get a
/// chance to register their own scoped services (e.g. a fake HttpClient).
/// </summary>
public abstract class MudBunitTestContext : TestContext
{
    protected MudBunitTestContext()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
