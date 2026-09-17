namespace SkyHelp.Authorization
{
    /// <summary>
    /// Nombres canónicos de roles en JWT y en <c>[Authorize(Roles = ...)]</c>.
    /// </summary>
    public static class RoleNames
    {
        public const string Administrador = "Administrador";
        public const string Tecnico = "Tecnico";
        public const string Domiciliario = "Domiciliario";
        public const string Usuario = "Usuario";
    }
}
