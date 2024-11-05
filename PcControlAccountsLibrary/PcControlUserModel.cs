namespace PcControlAccountsLibrary
{
    public class PcControlUserModel
    {
        public int Iod { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string CardNumber { get; set; }
        public string Department { get; set; }
        public string Pesel { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public IEnumerable<string> Rights { get; set; }
    }
}
