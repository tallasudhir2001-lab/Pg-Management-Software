namespace PgManagement_WebApi.Options
{
    public class PgProfileOptions
    {
        public string Name { get; set; } = "My PG";
        public string Address { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    public class EmailOptions
    {
        public string Provider { get; set; } = "Smtp";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "PG Management";
        public SmtpOptions? Smtp { get; set; }
        public AwsSesOptions? AwsSes { get; set; }
    }

    public class SmtpOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 1025;
        public bool EnableSsl { get; set; } = false;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class AwsSesOptions
    {
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string Region { get; set; } = "ap-south-1";
    }
}
