namespace RackPeek.Domain.Resources.Connections;

public static class ConnectionHelpers {
    public static bool Matches(PortReference a, PortReference b) {
        return a.Resource == b.Resource
               && a.PortGroup == b.PortGroup
               && a.PortIndex == b.PortIndex;
    }

    public static bool Contains(Connection c, PortReference port) => Matches(c.A, port) || Matches(c.B, port);
}
