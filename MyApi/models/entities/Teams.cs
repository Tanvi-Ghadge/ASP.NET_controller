using System;

namespace MyApi.models.entities;

public class Teams
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public int LeagueId { get; set; }
    public virtual League? League { get; set; }
}
