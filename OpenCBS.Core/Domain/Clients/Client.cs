using System.ComponentModel.DataAnnotations;

namespace OpenCBS.Core.Domain.Clients;

public enum ClientType
{
    Individual = 1,
    Corporate = 2
}

public enum ClientStatus
{
    PendingKyc = 0,
    Active = 1,
    Suspended = 2,
    Closed = 3
}

public class Client
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string IdentificationNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    public ClientType ClientType { get; set; } = ClientType.Individual;

    public ClientStatus Status { get; set; } = ClientStatus.PendingKyc;

    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [MaxLength(50)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string DisplayName => ClientType == ClientType.Corporate 
        ? CompanyName 
        : $"{FirstName} {LastName}".Trim();
}
