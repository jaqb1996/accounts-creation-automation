namespace MappingLibrary
{
    public class GeneralUserModel
    {
        public string Login { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Pesel { get; set; }
        public string Email { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string Npwz { get; set; }
        public int Iod { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public List<string> Areas { get; set; }
    }
}
