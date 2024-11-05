using ConfigurationLibrary;
using CSharpFunctionalExtensions;
using MappingLibrary;

namespace ExcelReaderLibrary
{
    public interface IMappingHelper
    {
        Result<GeneralUserModel> MapGeneralUser(ExcelUserModel user);
    }

    public class MappingHelper : IMappingHelper
    {
        private const int DaysToAdd = 2;
        private readonly ExcelMappingHelperConfig config;

        public MappingHelper(ExcelMappingHelperConfig config)
        {
            this.config = config;
        }

        public Result<GeneralUserModel> MapGeneralUser(ExcelUserModel user)
        {
            try
            {
                if (!MapDepartment(user.Department, out string department))
                {
                    return Result.Failure<GeneralUserModel>($"Błąd przy mapowaniu miejsca zatrudnienia użytkownika");
                }

                if (!MapPosition(user.Position, out string position))
                {
                    return Result.Failure<GeneralUserModel>($"Błąd przy mapowaniu stanowiska użytkownika");
                }

                (bool isMappingAreasSuccessful, string problematicArea) = MapAreas(user.Areas, out List<string> areas);
                if (!isMappingAreasSuccessful)
                {
                    return Result.Failure<GeneralUserModel>($"Błąd przy mapowaniu obszaru: {problematicArea}");
                }

                GeneralUserModel output = new()
                {
                    Iod = user.Iod,
                    Login = user.Login,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Pesel = user.Pesel,
                    Email = user.Email,
                    Npwz = PrepareNpwz(user.Npwz),
                    DateFrom = user.DateFrom,
                    DateTo = PrepareDateTo(user.DateTo),
                    Department = department,
                    Position = position,
                    Areas = areas
                };

                return Result.Success(output);
            }
            catch (Exception ex)
            {
                return Result.Failure<GeneralUserModel>($"Błąd przy mapowaniu użytkownika: {ex.Message}");
            }
        }

        private string PrepareNpwz(string npwz) => npwz == "nie dotyczy" ? null : npwz;

        private DateTime? PrepareDateTo(DateTime? dateTo)
        {
            if (dateTo is not DateTime dateToWithValue)
            {
                return null;
            }

            return dateToWithValue.AddDays(DaysToAdd);
        }

        private bool MapDepartment(string department, out string output)
        {
            output = default;

            if(config.DepartmentLookUpTable.TryGetValue(department, out string result))
            {
                output = result;
                return true;
            }

            return false;
        }

        private bool MapPosition(string position, out string output)
        {
            output = default;

            if (config.PositionLookUpTable.TryGetValue(position, out string result))
            {
                output = result;
                return true;
            }

            return false;
        }

        private (bool, string) MapAreas(List<string> areas, out List<string> output)
        {
            output= [];

            foreach (string area in areas)
            {
                if (!config.AreaLookUpTable.TryGetValue(area, out string result))
                {
                    return (false, area);
                }

                output.Add(result);
            }

            output = output.GroupBy(x => x).Select(x => x.Key).ToList();

            return (true, string.Empty);
        }
    }
}
