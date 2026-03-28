namespace PgManagement_WebApi.DTOs.AccessPoint
{
    public class AccessPointDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
    }

    public class AccessPointModuleDto
    {
        public string Module { get; set; } = string.Empty;
        public List<AccessPointDto> AccessPoints { get; set; } = new();
    }

    public class UpdateRoleAccessPointsDto
    {
        public int[] AccessPointIds { get; set; } = Array.Empty<int>();
    }
}
