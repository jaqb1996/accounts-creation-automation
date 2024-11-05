namespace ActiveDirectoryAccountsLibrary
{
    public class ActiveDirectoryUserModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SamAccountName { get; set; }
        public DateTime? DateTo { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public string EmailAddress { get; set; }
        public string Pager { get; set; }
        public int Iod { get; set; }
        public string Pesel { get; set; }
        public string Npwz { get; set; }
        public IEnumerable<string> Groups { get; set; }
    }
}
