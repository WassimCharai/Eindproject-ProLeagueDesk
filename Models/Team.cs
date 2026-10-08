using System;
using System.Collections.Generic;

namespace ProLeagueDesk.Models;

public partial class Team
{
    public int TeamId { get; set; }

    public string Name { get; set; } = null!;

    public int Points { get; set; }

    public int GoalsFor { get; set; }

    public int GoalsAgainst { get; set; }

    public int? CoachId { get; set; }

    public virtual ICollection<Match> MatchAwayTeams { get; set; } = new List<Match>();

    public virtual ICollection<Match> MatchHomeTeams { get; set; } = new List<Match>();
}
