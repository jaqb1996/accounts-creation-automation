namespace AmmsAccountsLibrary
{
    public class AmmsUserModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Pesel { get; set; }
        public string Npwz { get; set; }
        public DateTime? BlockingAccountDate { get; set; }
        public string PersonnelKind { get; set; }
        public string PersonnelFunction { get; set; }
        public IEnumerable<string> PersonnelUnits { get; set; }
        public IEnumerable<string> Groups { get; set; }
    }
}
