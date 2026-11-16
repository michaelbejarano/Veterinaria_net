namespace vet.Domain.DTOs.Auth;

public class CurrentUserDto
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public long RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}