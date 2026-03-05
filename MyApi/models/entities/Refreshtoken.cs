using System;

namespace MyApi.models.entities;

public class Refreshtoken
{
    public int Id { get; set; }

    public required string Token { get; set; }

    public DateTime Expires { get; set; }

    public bool IsRevoked { get; set; }

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }
}
