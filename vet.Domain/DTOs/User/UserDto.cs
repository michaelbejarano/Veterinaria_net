namespace vet.Domain.DTOs;

public class UserDto
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public long RoleId { get; set; }
    public bool IsActive { get; set; }
}