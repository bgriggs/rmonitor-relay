using RedMist.TimingCommon.Models;
using System.Collections.Generic;

namespace RedMist.Relay.Models;

public class CompetitorMetadataUpdate(List<CompetitorMetadata> cm)
{
    public List<CompetitorMetadata> CompetitorMetadata { get; } = cm;
}