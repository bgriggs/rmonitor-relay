using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RedMist.ReplayRMonitorData;

internal class Program
{
    const string TEST_FILE = "ec-test-data.txt";
    const double REPLAY_SPEED = 2.0;

    static void Main(string[] args)
    {
        Console.WriteLine($"Loading data from: {TEST_FILE}");
        var eventData = new EventData();
        eventData.Load(TEST_FILE);
        Console.WriteLine("Loaded {0} events", eventData.Count);

        // Start socket server
        Console.WriteLine("Starting socket server");
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Any, 50000));
        socket.Listen(10);
        while (true)
        {

            Console.WriteLine("Waiting connection ... ");

            // Suspend while waiting for
            // incoming connection Using 
            // Accept() method the server 
            // will accept connection of client
            Socket clientSocket = socket.Accept();

            try
            {
                var data = eventData.GetNextData();
                while (!string.IsNullOrEmpty(data))
                {
                    Console.WriteLine(data);
                    var bytes = Encoding.UTF8.GetBytes(data);
                    clientSocket.Send(bytes);
                    Thread.Sleep((int)(eventData.GetNextTime().TotalMilliseconds / REPLAY_SPEED));
                    data = eventData.GetNextData();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                eventData.Reset();
            }

            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }
}
