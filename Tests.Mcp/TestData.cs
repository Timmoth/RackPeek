namespace Tests.Mcp;

internal static class TestData {
    /// <summary>
    ///     A small, fully-understood inventory: two pieces of connected hardware, a
    ///     system on the server, two services on the system, one cabled connection.
    ///     Small enough that every test can state its expectations exactly.
    /// </summary>
    public const string Seed =
        """
        version: 4
        resources:
          - kind: Server
            name: rack-server
            discoveryId: rpk1:sys:aaaaaaaaaaaaaaaa
            tags:
              - prod
            labels:
              ansible_host: 10.0.0.2
            ports:
              - type: rj45
                speed: 1
                count: 4
          - kind: Switch
            name: rack-switch
            ports:
              - type: rj45
                speed: 1
                count: 8
          - kind: System
            name: host-os
            type: baremetal
            os: debian
            cores: 8
            ram: 32
            ip: 10.0.0.5
            runsOn:
              - rack-server
            labels:
              env: prod
          - kind: Service
            name: grafana
            runsOn:
              - host-os
            network:
              ip: 10.0.0.5
              port: 3000
              protocol: TCP
          - kind: Service
            name: prometheus
            runsOn:
              - host-os
            network:
              ip: 10.0.1.9
              port: 9090
              protocol: TCP
        connections:
          - a:
              resource: rack-server
              portGroup: 0
              portIndex: 0
            b:
              resource: rack-switch
              portGroup: 0
              portIndex: 0
            label: uplink
        """;

    /// <summary>The 46-resource demo inventory shipped with the repo, for breadth tests.</summary>
    public static string DemoConfig() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestConfigs", "demo-config.yaml"));

    /// <summary>Captured API output shared with Tests.Discovery, for the discovery tools.</summary>
    public static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}
