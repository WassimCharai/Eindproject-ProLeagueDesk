using System;
using System.Collections.Generic;

namespace ProLeagueDesk.Models;

public partial class AppSetting
{
    public string Key { get; set; } = null!;

    public string? Value { get; set; }
}
