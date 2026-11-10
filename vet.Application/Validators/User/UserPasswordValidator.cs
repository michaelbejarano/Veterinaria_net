namespace vet.Application.Validators.User;

public class UserPasswordValidator
{
    public void Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                "La contraseña es obligatoria.");
        }

        if (password.Length < 12)
        {
            throw new ArgumentException(
                "La contraseña debe tener al menos 12 caracteres.");
        }
    }
}