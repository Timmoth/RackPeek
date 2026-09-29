using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Tests.Mcp;

/// <summary>
///     A real Kestrel server on a random loopback port, serving captured API fixtures.
///     The discovery tools construct their own HttpClients internally, so unlike the
///     unit tests in Tests.Discovery a message-handler stub cannot reach them — the
///     fake engine has to answer on an actual socket.
/// </summary>
internal sealed class FakeHttpServer : IAsyncDisposable {
    private readonly WebApplication _app;

    private FakeHttpServer(WebApplication app) => _app = app;

    /// <summary>e.g. http://127.0.0.1:49213 — no trailing slash.</summary>
    public string BaseUrl => _app.Urls.First();

    public string Host => new Uri(BaseUrl).Authority;

    public static async Task<FakeHttpServer> StartAsync(Action<WebApplication> map, bool ipv6 = false) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();

        // An IPv6-only endpoint is the one case where an engine answers but has no IPv4
        // address to record its services at.
        builder.WebHost.UseUrls(ipv6 ? "http://[::1]:0" : "http://127.0.0.1:0");

        WebApplication app = builder.Build();
        map(app);
        await app.StartAsync();

        return new FakeHttpServer(app);
    }

    /// <summary>A fake Docker Engine API with the shared captured fixtures.</summary>
    public static Task<FakeHttpServer> StartDockerEngineAsync(bool ipv6 = false) =>
        StartAsync(app => {
            app.MapGet("/containers/json", () => Results.Content(
                TestData.Fixture("docker-containers.json"), "application/json"));
            app.MapGet("/info", () => Results.Content(
                TestData.Fixture("docker-info.json"), "application/json"));
        }, ipv6);

    /// <summary>
    ///     A fake Proxmox VE API. Both fixture nodes answer with the same guest lists,
    ///     which doubles as the migration case: the mapper must dedupe guests by vmid.
    /// </summary>
    public static Task<FakeHttpServer> StartProxmoxAsync() =>
        StartAsync(app => {
            string Json(string name) => TestData.Fixture(name);

            app.MapGet("/api2/json/cluster/status", () => Results.Content(Json("pve-cluster-status.json"), "application/json"));
            app.MapGet("/api2/json/nodes", () => Results.Content(Json("pve-nodes-full.json"), "application/json"));
            app.MapGet("/api2/json/nodes/{node}/status", () => Results.Content(Json("pve-node-status.json"), "application/json"));
            app.MapGet("/api2/json/nodes/{node}/disks/list", () => Results.Content(Json("pve-disks.json"), "application/json"));
            app.MapGet("/api2/json/nodes/{node}/hardware/pci", () => Results.Content(Json("pve-hardware-pci.json"), "application/json"));
            app.MapGet("/api2/json/nodes/{node}/qemu", () => Results.Content(Json("pve-qemu.json"), "application/json"));
            app.MapGet("/api2/json/nodes/{node}/lxc", () => Results.Content(Json("pve-lxc.json"), "application/json"));
            app.MapGet("/api2/json/nodes/{node}/qemu/{vmid}/config", () => Results.Content(Json("pve-qemu-config.json"), "application/json"));
            app.MapGet("/api2/json/nodes/{node}/lxc/{vmid}/config", () => Results.Content(Json("pve-lxc-config.json"), "application/json"));
        });

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
