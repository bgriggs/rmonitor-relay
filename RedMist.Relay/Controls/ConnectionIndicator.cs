using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using RedMist.Relay.Models;

namespace RedMist.Relay.Controls;

public class ConnectionIndicator : TemplatedControl
{
    public static readonly DirectProperty<ConnectionIndicator, ConnectionState> ConnectionProperty =
    AvaloniaProperty.RegisterDirect<ConnectionIndicator, ConnectionState>(
        nameof(Connection),
        o => o.Connection,
        (o, v) => o.Connection = v);

    private ConnectionState _connection = ConnectionState.Unknown;

    public ConnectionState Connection
    {
        get { return _connection; }
        set 
        { 
            SetAndRaise(ConnectionProperty, ref _connection, value);
            IsConnectionConnected = value == ConnectionState.Connected;
            IsConnectionDisconnected = value == ConnectionState.Disconnected;
            IsConnectionConnecting = value == ConnectionState.Connecting;
        }
    }

    public static readonly DirectProperty<ConnectionIndicator, bool> IsConnectionConnectedProperty =
    AvaloniaProperty.RegisterDirect<ConnectionIndicator, bool>(
        nameof(IsConnectionConnected), o => o.IsConnectionConnected);

    private bool _isConnectionConnected;

    public bool IsConnectionConnected
    {
        get { return _isConnectionConnected; }
        private set { SetAndRaise(IsConnectionConnectedProperty, ref _isConnectionConnected, value); }
    }

    public static readonly DirectProperty<ConnectionIndicator, bool> IsConnectionDisconnectedProperty =
    AvaloniaProperty.RegisterDirect<ConnectionIndicator, bool>(
        nameof(IsConnectionDisconnected), o => o.IsConnectionDisconnected);

    private bool _isConnectionDisconnected;

    public bool IsConnectionDisconnected
    {
        get { return _isConnectionDisconnected; }
        private set { SetAndRaise(IsConnectionDisconnectedProperty, ref _isConnectionDisconnected, value); }
    }

    public static readonly DirectProperty<ConnectionIndicator, bool> IsConnectionConnectingProperty =
    AvaloniaProperty.RegisterDirect<ConnectionIndicator, bool>(
        nameof(IsConnectionConnecting), o => o.IsConnectionConnecting);

    private bool _isConnectingConnected;

    public bool IsConnectionConnecting
    {
        get { return _isConnectingConnected; }
        private set { SetAndRaise(IsConnectionConnectingProperty, ref _isConnectingConnected, value); }
    }
}
