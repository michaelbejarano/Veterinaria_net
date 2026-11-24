using Microsoft.AspNetCore.Identity;

Console.Write("Nueva contraseña: ");
var password = Console.ReadLine();

if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
{
    Console.WriteLine("La contraseña debe tener al menos 12 caracteres.");
    return;
}

var hasher = new PasswordHasher<object>();

var hash = hasher.HashPassword(
    new object(),
    password);

Console.WriteLine("\nHASH GENERADO:");
Console.WriteLine(hash);