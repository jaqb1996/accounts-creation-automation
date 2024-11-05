using ActiveDirectoryAccountsLibrary;
using AmmsAccountsLibrary;
using ConfigurationLibrary;
using CSharpFunctionalExtensions;
using EmailAccountsLibrary;
using PcControlAccountsLibrary;

namespace MappingLibrary
{
    public interface IMapper
    {
        Result<ActiveDirectoryUserModel> MapAdUser(GeneralUserModel user, string pager);
        Result<AmmsUserModel> MapAmmsUser(GeneralUserModel user);
        Result<PcControlUserModel> MapPcControlUser(GeneralUserModel user, string cardNumber);
        Result<EmailUserModel> MapEmailUser(GeneralUserModel user);
    }

    public class Mapper : IMapper
    {
        private readonly MapperConfig config;

        public Mapper(MapperConfig config)
        {
            this.config = config;
        }

        public Result<ActiveDirectoryUserModel> MapAdUser(GeneralUserModel user, string pager)
        {
            bool isMappingPathSuccessful = MapPath(user.Department, out string path);
            if (!isMappingPathSuccessful)
            {
                return Result.Failure<ActiveDirectoryUserModel>("Błąd przy mapowaniu ścieżki jednostki organizacyjnej");
            }

            (bool isMappingGroupsSuccessful, string problematicGroup) = MapAdGroups(user.Position, user.Areas, out List<string> groups);
            if (!isMappingGroupsSuccessful)
            {
                return Result.Failure<ActiveDirectoryUserModel>($"Błąd przy mapowaniu grupy: {problematicGroup}");
            }

            ActiveDirectoryUserModel output = new()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                SamAccountName = user.Login,
                DateTo = user.DateTo,
                Path = path,
                Description = PrepareDescription(user.Position),
                EmailAddress = user.Email,
                Pager = PreparePager(pager),
                Iod = user.Iod,
                Pesel = user.Pesel,
                Npwz = PrepareNpwz(user.Npwz),
                Groups = groups
            };

            return Result.Success(output);
        }

        private string PrepareNpwz(string npwz) => string.IsNullOrEmpty(npwz) ? "brak" : npwz;

        public Result<AmmsUserModel> MapAmmsUser(GeneralUserModel user)
        {
            (bool isMappingPersonnelUnitsSuccessful, string problematicUnit) = MapPersonnelUnits(user.Areas, out List<string> personnelUnits);

            if (!isMappingPersonnelUnitsSuccessful)
            {
                return Result.Failure<AmmsUserModel>($"Błąd przy mapowaniu jednostki personelu: {problematicUnit}");
            }

            (bool isMappingGroupsSuccessful, string problematicGroup) = MapAmmsGroups(user.Position, user.Areas, out List<string> groups);

            if (!isMappingGroupsSuccessful)
            {
                return Result.Failure<AmmsUserModel>($"Błąd przy mapowaniu grup: {problematicGroup}");
            }

            bool isMappingPersonnelKindSuccessful = MapPersonnelKind(user.Position, out string personnelKind);

            if (!isMappingPersonnelKindSuccessful)
            {
                return Result.Failure<AmmsUserModel>($"Błąd przy mapowaniu rodzaju personelu dla {user.Position}");
            }

            bool isMappingPersonnelFunctionSuccessful = MapPersonnelFunction(user.Position, out string personnelFunction);

            if (!isMappingPersonnelFunctionSuccessful)
            {
                return Result.Failure<AmmsUserModel>($"Błąd przy mapowaniu funkcji personelu dla {user.Position}");
            }

            AmmsUserModel output = new()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.Login,
                Pesel = user.Pesel,
                Npwz = user.Npwz,
                BlockingAccountDate = user.DateTo,
                PersonnelKind = personnelKind,
                PersonnelFunction = personnelFunction,
                PersonnelUnits = personnelUnits,
                Groups = groups
            };

            return Result.Success(output);
        }

        private (bool, string) MapAmmsGroups(string position, List<string> areas, out List<string> groups)
        {
            groups = [];

            if (!config.AmmsGroupsForPosition.TryGetValue(position, out string[] groupsForPosition))
            {
                return (false, position);
            }

            groups.AddRange(groupsForPosition);

            foreach (string area in areas)
            {
                if (!config.AmmsGroupsForPositionAndArea.TryGetValue((position, area), out string[] groupsForPositionAndArea))
                {
                    return (false, $"({position}, {area})");
                }

                groups.AddRange(groupsForPositionAndArea);
            }

            return (true, string.Empty);
        }

        private (bool, string) MapPersonnelUnits(List<string> areas, out List<string> personnelUnits)
        {
            personnelUnits = [];

            foreach (string area in areas)
            {
                if(!config.AmmsPersonnelUnitForArea.TryGetValue(area, out string unit))
                {
                    return (false, area);
                }

                if (unit is null)
                {
                    continue;
                }

                personnelUnits.Add(unit);
            }

            return (true, string.Empty);
        }

        public Result<PcControlUserModel> MapPcControlUser(GeneralUserModel user, string cardNumber)
        {
            if(!MapDepartment(user.Department, out string department))
            {
                return Result.Failure<PcControlUserModel>($"Błąd przy mapowaniu wydziału: {user.Department}");
            }

            (bool isMappingRightsSuccessful, string problematicRight) = MapRights(user.Position, user.Areas, out List<string> rights);

            if(!isMappingRightsSuccessful)
            {
                return Result.Failure<PcControlUserModel>($"Błąd przy mapowaniu uprawnienia: {problematicRight}");
            }

            PcControlUserModel output = new()
            {
                Iod = user.Iod,
                Name = user.Login,
                LastName = user.LastName,
                CardNumber = cardNumber,
                Department = department,
                Pesel= user.Pesel,
                DateFrom = user.DateFrom,
                DateTo = user.DateTo,
                Rights = rights
            };

            return Result.Success(output);
        }

        public Result<EmailUserModel> MapEmailUser(GeneralUserModel user)
        {
            EmailUserModel output = new()
            {
                Username = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            return Result.Success(output);
        }

        private (bool, string) MapRights(string position, List<string> areas, out List<string> rights)
        {
            rights = new(config.PcControlRightsForAllUsers);

            foreach (string area in areas)
            {
                if (!config.PcControlRightsForPositionAndArea.TryGetValue((position, area), out string[] rightsForPositionAndArea))
                {
                    return (false, $"({position}, {area})");
                }

                rights.AddRange(rightsForPositionAndArea);
            }

            return (true, string.Empty);
        }

        private bool MapDepartment(string department, out string output)
        {
            output = "1 Szpital urazowy";

            return true;
        }

        private bool MapPath(string department, out string path) => config.PathForDepartment.TryGetValue(department, out path);

        private string PrepareDescription(string position) => position.ToString();

        private string PreparePager(string pager) => string.IsNullOrEmpty(pager) ? config.PagerEmptyValue : pager;

        private (bool, string) MapAdGroups(string position, List<string> areas, out List<string> groups)
        {
            groups = [];

            foreach (string area in areas)
            {
                if(!config.AdGroupsForPositionAndArea.TryGetValue((position, area), out string[] groupsForPositionAndArea))
                {
                    return (false, $"({position}, {area})");
                }

                groups.AddRange(groupsForPositionAndArea);
            }

            if(!config.AdGroupsForPosition.TryGetValue(position, out string[] groupsForPosition))
            {
                return (false, position.ToString());
            }

            groups.AddRange(groupsForPosition);

            return (true, string.Empty);
        }

        private bool MapPersonnelKind(string position, out string output) => config.AmmsPersonnelKindForPosition.TryGetValue(position, out output);

        private bool MapPersonnelFunction(string positon, out string output) => config.AmmsPersonnelFunctionForPosition.TryGetValue(positon, out output);
    }
}
