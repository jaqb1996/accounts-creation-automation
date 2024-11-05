using ExcelDataReader;
using System.Data;
using System.Text;
using CSharpFunctionalExtensions;
using MappingLibrary;
using ConfigurationLibrary;

namespace ExcelReaderLibrary
{
    public interface IExcelReader
    {
        public Result<GeneralUserModel> Read(int iod);
    }

    public class ExcelReader : IExcelReader
    {
        private readonly ExcelReaderConfig config;
        private readonly IParsingHelper parsingHelper;
        private readonly IMappingHelper mappingHelper;

        public ExcelReader(ExcelReaderConfig config, IParsingHelper parsingHelper, IMappingHelper mappingHelper)
        {
            this.config = config;
            this.parsingHelper = parsingHelper;
            this.mappingHelper = mappingHelper;
        }

        public Result<GeneralUserModel> Read(int iod)
        {
            try
            {
                DataRow row = ReadRow(iod.ToString());

                if (row is null)
                {
                    return Result.Failure<GeneralUserModel>($"Nie znaleziono pracownika o podanym IOD: {iod}");
                }

                ExcelUserModel userModel = PrepareExcelUserModel(row);

                (_, bool isFailure, GeneralUserModel output, string error) = mappingHelper.MapGeneralUser(userModel);

                if (isFailure)
                {
                    return Result.Failure<GeneralUserModel>($"Błąd przy mapowaniu danych z pliku: {error}");
                }

                return Result.Success(output);
            }
            catch (Exception ex)
            {
                return Result.Failure<GeneralUserModel>($"Błąd przy przetwarzaniu danych: {ex.Message}");
            }
            
        }

        private ExcelUserModel PrepareExcelUserModel(DataRow row)
        {
            string dateFromField = row[8].ToString().Trim();
            string dateToField = row[9].ToString().Trim();
            string primaryArea = row[15].ToString().Trim();
            string additionalArea1 = row[19].ToString().Trim();
            string additionalArea2 = row[20].ToString().Trim();
            string additionalArea3 = row[21].ToString().Trim();
            string additionalArea4 = row[22].ToString().Trim();
            string sorArea = row[23].ToString().Trim();
            string otherAreas = row[25].ToString().Trim();
            string domainLoginField = row[29].ToString().Trim();
            string ammsLoginField = row[30].ToString().Trim();
            string email = row[39].ToString().Trim();

            ExcelUserModel userModel = new()
            {
                Iod = int.Parse(row[0].ToString()),
                Login = row[1].ToString().Trim(),
                FirstName = row[4].ToString().Trim(),
                LastName = row[5].ToString().Trim(),
                Pesel = row[6].ToString().Trim(),
                DateFrom = parsingHelper.ParseDateFrom(dateFromField),
                DateTo = parsingHelper.ParseDateTo(dateToField),
                Npwz = row[11].ToString().Trim(),
                Position = row[12].ToString().Trim(),
                Department = row[13].ToString().Trim(),
                Areas = parsingHelper.ParseAreas(primaryArea, additionalArea1, additionalArea2, additionalArea3,
                    additionalArea4, sorArea, otherAreas),
                DomainLogin = parsingHelper.ParseDomainLogin(domainLoginField),
                AmmsLogin = parsingHelper.ParseAmmsLogin(ammsLoginField),
                Email = parsingHelper.ParseEmail(email)
            };

            return userModel;
        }

        private DataRow ReadRow(string iod)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using FileStream stream = File.Open(config.ExcelFilePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream);
            DataSet data = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                FilterSheet = (dataReader, _) => dataReader.Name == config.WorkersSheetName,
                ConfigureDataTable = reader => new ExcelDataTableConfiguration
                {
                    FilterRow = rowReader => true
                }
            });

            DataTable dataTable = data.Tables[config.WorkersSheetName];

            DataRow output = null;

            foreach (DataRow row in dataTable.Rows)
            {
                if (row[0].ToString() == iod)
                {
                    output = row;
                    break;
                }
            }

            return output;
        }
    }
}
