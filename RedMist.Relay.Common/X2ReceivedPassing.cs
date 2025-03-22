using RedMist.TimingCommon.Models.X2;

namespace RedMist.Relay.Common;

public class X2ReceivedPassing(List<Passing> passings)
{
    public List<Passing> Passings { get; } = passings;
}
