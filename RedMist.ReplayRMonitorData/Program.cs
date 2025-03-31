using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RedMist.ReplayRMonitorData;

internal class Program
{
    //const string TEST_FILE = "ec-test-data.txt";
    const string TEST_FILE = "barber2025.txt";
    const double REPLAY_SPEED = 3.0;

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
            Socket clientSocket = socket.Accept();

            try
            {
                var data = eventData.GetNextData();
                while (!string.IsNullOrEmpty(data))
                {
                    Console.WriteLine(data);
                    var bytes = Encoding.UTF8.GetBytes(data);
                    clientSocket.Send(bytes);
                    var duration = (int)(eventData.GetNextTime().TotalMilliseconds / REPLAY_SPEED);
                    if (duration < 0)
                        duration = 0;
                    else if (duration > 10000)
                        duration = 10000;

                    Thread.Sleep(duration);
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
