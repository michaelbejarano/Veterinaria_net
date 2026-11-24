namespace vet.Domain.Services;

public static class UserRoleRules
{
    public static void ValidateRoleAssignment(
        string currentRole,
        string targetRole)
    {
        if (currentRole == "SUPER_ADMIN")
            return;

        if (currentRole == "ADMIN" &&
            targetRole is (
                "VETERINARIAN" or
                "RECEPTIONIST" or
                "SPECIALIST"))
        {
            return;
        }

        throw new UnauthorizedAccessException(
            "No tiene permisos para asignar este rol.");
    }

    public static void ValidateManageRole(
        string currentRole,
        string targetRole)
    {
        if (currentRole == "SUPER_ADMIN")
            return;

        if (currentRole == "ADMIN" &&
            targetRole is not ("SUPER_ADMIN" or "ADMIN"))
        {
            return;
        }

        throw new UnauthorizedAccessException(
            "No tiene permisos para modificar esta cuenta.");
    }
}