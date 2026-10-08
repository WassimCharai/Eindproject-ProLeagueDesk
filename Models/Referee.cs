using System;
using System.Collections.Generic;

namespace ProLeagueDesk.Models;

public partial class Referee
{
    public int RefereeId { get; set; }

    public string Name { get; set; } = null!;

    public int MatchesOfficiated { get; set; }

    public int? UserId { get; set; }

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();

    public virtual User? User { get; set; }
}
