namespace RedMist.Relay.Common;

public interface IX2Client
{
    ConnectionState ConnectionState { get; }
    bool Connect(string hostname, string username, string password);
    List<TimingCommon.Models.X2.Loop>? GetLoops();
    void ResendPassings(DateTime start, DateTime? end);
}
