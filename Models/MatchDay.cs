using System;
using System.Collections.Generic;

namespace ProLeagueDesk.Models;

public partial class MatchDay
{
    public int MatchdayId { get; set; }

    public int Number { get; set; }

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
