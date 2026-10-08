using System;
using System.Collections.Generic;

namespace ProLeagueDesk.Models;

public partial class Coach
{
    public int CoachId { get; set; }

    public string Name { get; set; } = null!;

    public int PhoneNumber { get; set; }

    public int? UserId { get; set; }

    public virtual User? User { get; set; }
}
