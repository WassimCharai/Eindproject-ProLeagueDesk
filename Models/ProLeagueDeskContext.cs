using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ProLeagueDesk.Models;

public partial class ProLeagueDeskContext : DbContext
{
    public ProLeagueDeskContext()
    {
    }

    public ProLeagueDeskContext(DbContextOptions<ProLeagueDeskContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<AppSetting> AppSettings { get; set; }

    public virtual DbSet<Coach> Coaches { get; set; }

    public virtual DbSet<Match> Matches { get; set; }

    public virtual DbSet<MatchDay> MatchDays { get; set; }

    public virtual DbSet<Referee> Referees { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("DataSource=ProLeagueDesk.sqlite");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.ToTable("Admin");

            entity.Property(e => e.UserId).HasColumnName("User_Id");

            entity.HasOne(d => d.User).WithMany(p => p.Admins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Key);
        });

        modelBuilder.Entity<Coach>(entity =>
        {
            entity.ToTable("Coach");

            entity.Property(e => e.CoachId).HasColumnName("Coach_Id");
            entity.Property(e => e.UserId).HasColumnName("User_Id");

            entity.HasOne(d => d.User).WithMany(p => p.Coaches).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.ToTable("Match");

            entity.Property(e => e.MatchId).HasColumnName("Match_Id");
            entity.Property(e => e.AwayScore).HasColumnName("Away_Score");
            entity.Property(e => e.AwayTeamId).HasColumnName("Away_Team_Id");
            entity.Property(e => e.HomeScore).HasColumnName("Home_Score");
            entity.Property(e => e.HomeTeamId).HasColumnName("Home_Team_Id");
            entity.Property(e => e.MatchdayId).HasColumnName("Matchday_Id");
            entity.Property(e => e.RefereeId).HasColumnName("Referee_Id");
            entity.Property(e => e.IsPlayed).HasColumnName("Is_Played");

            entity.HasOne(d => d.AwayTeam).WithMany(p => p.MatchAwayTeams).HasForeignKey(d => d.AwayTeamId);

            entity.HasOne(d => d.HomeTeam).WithMany(p => p.MatchHomeTeams).HasForeignKey(d => d.HomeTeamId);

            entity.HasOne(d => d.Matchday).WithMany(p => p.Matches).HasForeignKey(d => d.MatchdayId);

            entity.HasOne(d => d.Referee).WithMany(p => p.Matches).HasForeignKey(d => d.RefereeId);
        });

        modelBuilder.Entity<MatchDay>(entity =>
        {
            entity.ToTable("MatchDay");

            entity.Property(e => e.MatchdayId).HasColumnName("Matchday_Id");
        });

        modelBuilder.Entity<Referee>(entity =>
        {
            entity.ToTable("Referee");

            entity.Property(e => e.RefereeId).HasColumnName("Referee_Id");
            entity.Property(e => e.MatchesOfficiated).HasColumnName("Matches_officiated");
            entity.Property(e => e.UserId).HasColumnName("User_Id");

            entity.HasOne(d => d.User).WithMany(p => p.Referees).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("Team");

            entity.Property(e => e.TeamId).HasColumnName("Team_Id");
            entity.Property(e => e.CoachId).HasColumnName("Coach_Id");
            entity.Property(e => e.GoalsAgainst).HasColumnName("Goals_against");
            entity.Property(e => e.GoalsFor).HasColumnName("Goals_for");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.UserId).HasColumnName("User_Id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
