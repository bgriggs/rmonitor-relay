using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedMist.RMonitorRelay.Services;

public class RMonitorClient
{
    private Socket? _client;
    private ILogger Logger { get; }
    public event Action<string>? ReceivedData;

    public RMonitorClient(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType().Name);
    }

    public async Task<bool> ConnectAsync(string ip, int port, CancellationToken cancellationToken)
    {
        var ipAddr = IPAddress.Parse(ip);
        var localEndPoint = new IPEndPoint(ipAddr, port);
        _client = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        Logger.LogInformation("RMonitor client connecting to {0}", localEndPoint);
        try
        {
            await _client.ConnectAsync(localEndPoint, cancellationToken);
            Logger.LogInformation("RMonitor client connected to {0}", localEndPoint);

            StartReceive(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RMonitor client failed to connect to {0}", localEndPoint);
        }

        return false;
    }

    private void StartReceive(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            try
            {
                while (_client != null)
                {
                    var buffer = new byte[1024];
                    var length = await _client.ReceiveAsync(buffer, cancellationToken);

                    if (length > 0)
                    {
                        var data = Encoding.UTF8.GetString(buffer, 0, length);
                        Logger.LogTrace("RX: {0}", data);
                        ReceivedData?.Invoke(data);
                    }

                    await Task.Yield();
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Socket error");
            }
        }, cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_client == null)
        {
            return;
        }
        try
        {
            await _client.DisconnectAsync(false, cancellationToken);
            _client.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Socket dispose error");
        }
        finally
        {
            _client = null;
        }
    }
}