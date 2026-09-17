namespace SkyHelp.Authorization
{
    /// <summary>
    /// Convierte el nombre de rol almacenado en BD a la forma canónica del token JWT.
    /// </summary>
    public static class RoleClaimMapper
    {
        public static string ToJwtRole(string? nombreRolEnBd)
        {
            if (string.IsNullOrWhiteSpace(nombreRolEnBd))
                return RoleNames.Usuario;

            var n = nombreRolEnBd.Trim().ToLowerInvariant();
            return n switch
            {
                "administrador" => RoleNames.Administrador,
                "tecnico" or "técnico" => RoleNames.Tecnico,
                "domiciliario" or "domi" => RoleNames.Domiciliario,
                "cliente" or "usuario" => RoleNames.Usuario,
                "backend" => RoleNames.Administrador,
                _ => nombreRolEnBd.Trim()
            };
        }
    }
}
