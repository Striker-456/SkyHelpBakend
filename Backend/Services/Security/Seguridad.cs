using System.Security.Cryptography;
using System.Text;
// Si esta mierda funciona no la vuelvo a tocar.
namespace SkyHelp.Services.Security
{
    // El nombre del namespace se conserva para no tocar todos los using existentes,
    // pero desde ahora las contraseñas nuevas se protegen con BCrypt (con sal
    // aleatoria por contraseña), no con SHA-256 plano.
    public static class Seguridad
    {
        // Hashes legados (previos a esta migración) son SHA-256 + Base64: 44 caracteres,
        // sin el prefijo "$2" que usa BCrypt. Se siguen aceptando solo para poder verificar
        // el login de cuentas viejas; ContrasenaEsBCrypt distingue ambos formatos.
        private static bool ContrasenaEsBCrypt(string hash) => hash.StartsWith("$2");

        public static string Hashear(string contrasenaPlano) => BCrypt.Net.BCrypt.HashPassword(contrasenaPlano);

        // Devuelve true si la contraseña en texto plano coincide con el hash almacenado,
        // sin importar si ese hash es BCrypt (formato actual) o SHA-256 (formato legado).
        public static bool Verificar(string contrasenaPlano, string hashAlmacenado)
        {
            if (string.IsNullOrEmpty(hashAlmacenado)) return false;

            if (ContrasenaEsBCrypt(hashAlmacenado))
                return BCrypt.Net.BCrypt.Verify(contrasenaPlano, hashAlmacenado);

            return EncriptarSHA256Legado(contrasenaPlano) == hashAlmacenado;
        }

        public static bool EsHashLegado(string hashAlmacenado) => !string.IsNullOrEmpty(hashAlmacenado) && !ContrasenaEsBCrypt(hashAlmacenado);

        private static string EncriptarSHA256Legado(string texto)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
            return Convert.ToBase64String(hash);
        }
    }
}
