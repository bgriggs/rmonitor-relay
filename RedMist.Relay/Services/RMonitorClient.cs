using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

public class RMonitorClient
{
    private Socket? _client;
    private ILogger Logger { get; }
    public event Action<string>? ReceivedData;

    public RMonitorClient(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType().Name);
    }

    public Task<bool> ConnectAsync(string ip, int port, CancellationToken cancellationToken)
    {
        var ipAddr = IPAddress.Parse(ip);
        var localEndPoint = new IPEndPoint(ipAddr, port);
        _client = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        Logger.LogInformation("RMonitor client connecting to {0}", localEndPoint);
        _ = Task.Run(async () =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _client != null)
                {
                    try
                    {
                        if (!_client.Connected)
                        {
                            if (_client != null)
                            {
                                try
                                {
                                    _client.Dispose();
                                }
                                catch { }
                            }

                            _client = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                            await _client.ConnectAsync(localEndPoint, cancellationToken);
                            Logger.LogInformation("RMonitor client connected to {0}", localEndPoint);

                            StartReceive(cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "RMonitor client failed to connect to {0}", localEndPoint);
                    }

                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "RMonitor client failed to connect to {0}", localEndPoint);
            }
        }, cancellationToken);

        return Task.FromResult(true);
    }

    private void StartReceive(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            try
            {
                while (_client != null)
                {
                    var message = new List<byte>();
                    int length = 0;
                    do
                    {
                        var buffer = new byte[1024];
                        length = _client.Receive(buffer);
                        if (length > 0)
                        {
                            message.AddRange(buffer[..length]);
                            if (message.Last() == '\n')
                            {
                                break;
                            }
                        }
                    } while (length > 0);

                    if (message.Count > 0)
                    {
                        var data = Encoding.UTF8.GetString([.. message], 0, message.Count);
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