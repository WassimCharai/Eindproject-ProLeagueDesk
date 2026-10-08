using System;
using System.Collections.Generic;

namespace ProLeagueDesk.Models;

public partial class Match
{
    public int MatchId { get; set; }

    public int HomeScore { get; set; }

    public int AwayScore { get; set; }

    public bool IsPlayed { get; set; }

    public string Datum { get; set; } = null!;

    public int? HomeTeamId { get; set; }

    public int? AwayTeamId { get; set; }

    public int? RefereeId { get; set; }

    public int? MatchdayId { get; set; }

    public virtual Team? AwayTeam { get; set; }

    public virtual Team? HomeTeam { get; set; }

    public virtual MatchDay? Matchday { get; set; }

    public virtual Referee? Referee { get; set; }
}
