namespace PgManagement_WebApi.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AccessPointAttribute : Attribute
    {
        public string Module { get; }
        public string DisplayName { get; }

        public AccessPointAttribute(string module, string displayName)
        {
            Module = module;
            DisplayName = displayName;
        }
    }
}
