namespace ExcelReaderLibrary
{
    public interface IParsingHelper
    {
        public string ParseDomainLogin(string domainLogin);
        public string ParseAmmsLogin(string ammsLogin);
        public List<string> ParseAreas(string primaryArea, string additionalArea1, string additionalArea2,
            string additionalArea3, string additionalArea4, string sorArea, string otherAreas);
        public DateTime? ParseDateTo(string dateTo);
        public DateTime ParseDateFrom(string dateFromField);

        public string ParseEmail(string email);
    }

    public class ParsingHelper : IParsingHelper
    {
        private readonly List<string> valuesToReplace = [" ", "-", "login", "(administrator)", "(pełendostęp)"];

        public string ParseDomainLogin(string domainLogin)
        {
            string output = domainLogin.Trim().ToLower();

            foreach (string value in valuesToReplace)
            {
                output = output.Replace(value, "");
            }

            return output;
        }

        public string ParseAmmsLogin(string ammsLogin)
        {
            string output = ammsLogin.Trim().ToLower();

            foreach (string value in valuesToReplace)
            {
                output = output.Replace(value, "");
            }

            return output;
        }

        public List<string> ParseAreas(string primaryArea, string additionalArea1, string additionalArea2,
            string additionalArea3, string additionalArea4, string sorArea, string otherAreas)
        {
            List<string> output = [primaryArea, additionalArea1, additionalArea2, additionalArea3, additionalArea4, sorArea];
            string[] splitted = otherAreas.Split(',');
            output.AddRange(splitted.Select(x => x.Trim()));
            return output.Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        public DateTime? ParseDateTo(string dateTo)
        {
            if (!DateTime.TryParse(dateTo, out var date))
            {
                return null;
            }

            return date;
        }

        public DateTime ParseDateFrom(string dateFromField) => DateTime.Parse(dateFromField);

        public string ParseEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            return email;
        }
    }
}
