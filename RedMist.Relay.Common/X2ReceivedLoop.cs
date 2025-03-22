namespace RedMist.Relay.Common;

public class X2ReceivedLoop(List<TimingCommon.Models.X2.Loop> loops)
{
    public List<TimingCommon.Models.X2.Loop> Loops { get; } = loops;
}
