using System;
using System.Collections.Generic;

namespace ProLeagueDesk.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? ProfileImagePath { get; set; }

    public virtual ICollection<Admin> Admins { get; set; } = new List<Admin>();

    public virtual ICollection<Coach> Coaches { get; set; } = new List<Coach>();

    public virtual ICollection<Referee> Referees { get; set; } = new List<Referee>();
}
