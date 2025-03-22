namespace RedMist.Relay.Common;

public class X2ConnectionState(ConnectionState state)
{
    public ConnectionState State { get; } = state;
}
