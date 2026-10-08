using System;
using System.Collections.Generic;

namespace ProLeagueDesk.Models;

public partial class Admin
{
    public int AdminId { get; set; }

    public int? AdminNumber { get; set; }

    public int? UserId { get; set; }

    public virtual User? User { get; set; }
}
