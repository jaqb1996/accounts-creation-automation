using CSharpFunctionalExtensions;
using Microsoft.Extensions.Configuration;

namespace ConfigurationLibrary
{
    public interface IConfigPreparer
    {
        Result<GeneralConfig> GetConfig(IConfiguration configuration);
    }

    public class ConfigPreparer : IConfigPreparer
    {
        public Result<GeneralConfig> GetConfig(IConfiguration configuration)
        {
            try
            {
                GeneralConfig output = new()
                {
                    ProgramConfig = GetProgramConfig(configuration.GetRequiredSection("Ustawienia:Program")),
                    ExcelReaderConfig = GetExcelReaderConfig(configuration.GetRequiredSection("Ustawienia:OdczytExcel")),
                    ExcelMappingHelperConfig = GetExcelMappingHelperConfig(configuration.GetRequiredSection("Ustawienia:MapowanieExcel")),
                    AdConfig = GetAdConfig(configuration.GetRequiredSection("Ustawienia:AD")),
                    AmmsConfig = GetAmmsConfig(configuration.GetRequiredSection("Ustawienia:Amms")),
                    PcControlConfig = GetPcControlConfig(configuration.GetRequiredSection("Ustawienia:PcControl")),
                    ZimbraConfig = GetZimbraConfig(configuration.GetRequiredSection("Ustawienia:Zimbra")),
                    MapperConfig = GetMapperConfig(configuration.GetRequiredSection("Ustawienia:MapowanieSystemy"))
                };

                return output;
            }
            catch (Exception ex)
            {
                return Result.Failure<GeneralConfig>(ex.Message);
            }
        }

        private ProgramConfig GetProgramConfig(IConfigurationSection configurationSection)
        {
            ProgramConfig output = new()
            {
                RunActiveDirectory = PrepareRun(configurationSection["Active Directory"]),
                RunPControl = PrepareRun(configurationSection["PControl"]),
                RunAmms = PrepareRun(configurationSection["AMMS"]),
                RunZimbra = PrepareRun(configurationSection["Zimbra"])
            };

            return output;
        }

        private RunningSectionOption PrepareRun(string info)
            => info.ToLower() switch
            {
                "tak" or "t" => RunningSectionOption.Run,
                "nie" or "n" => RunningSectionOption.DoNotRun,
                _ => RunningSectionOption.Ask
            };

        private ExcelMappingHelperConfig GetExcelMappingHelperConfig(IConfigurationSection configurationSection)
        {
            ExcelMappingHelperConfig output = new()
            {
                DepartmentLookUpTable = GetDictionaryFromSection(configurationSection, "Miejsce zatrudnienia"),
                PositionLookUpTable = GetDictionaryFromSection(configurationSection, "Stanowisko"),
                AreaLookUpTable = GetDictionaryFromSection(configurationSection, "Obszar przetwarzania")
            };

            return output;
        }

        private ExcelReaderConfig GetExcelReaderConfig(IConfigurationSection configurationSection)
        {
            ExcelReaderConfig output = new()
            {
                ExcelFilePath = configurationSection["Ścieżka do pliku"],
                WorkersSheetName = configurationSection["Nazwa arkusza"]
            };

            return output;
        }

        private AdConfig GetAdConfig(IConfigurationSection configurationSection)
        {
            AdConfig output = new()
            {
                FirstPassword = configurationSection["Pierwsze hasło"],
                Ps1File = configurationSection["Ścieżka do skryptu"]
            };

            return output;
        }

        private AmmsConfig GetAmmsConfig(IConfigurationSection configurationSection)
        {
            AmmsConfig output = new()
            {
                NewPassword = configurationSection["Pierwsze hasło"]
            };

            return output;
        }

        private PcControlConfig GetPcControlConfig(IConfigurationSection configurationSection)
        {
            PcControlConfig output = new()
            {
                UserName = configurationSection["Login"],
                Password = configurationSection["Hasło"],
                IdPrefix = configurationSection["PrefixId"]
            };

            return output;
        }

        private ZimbraConfig GetZimbraConfig(IConfigurationSection configurationSection)
        {
            ZimbraConfig output = new()
            {
                Login = configurationSection["Login"],
                Password = configurationSection["Hasło"],
                Groups = GetArrayFromSection(configurationSection, "Grupy")
            };

            return output;
        }

        private MapperConfig GetMapperConfig(IConfigurationSection configurationSection)
        {
            MapperConfig output = new()
            {
                PagerEmptyValue = configurationSection["Oznaczenie braku karty"],
                AdGroupsForPositionAndArea = GetDictionaryFromSectionWithTupleAsKeyAndArrayAsValue(configurationSection, "Grupy Active Directory"),
                AdGroupsForPosition = GetDictionaryFromSectionWithArrayAsValue(configurationSection, "Grupy Active Directory dla stanowiska"),
                PathForDepartment = GetDictionaryFromSection(configurationSection, "Ścieżki Active Directory"),
                PcControlRightsForPositionAndArea = GetDictionaryFromSectionWithTupleAsKeyAndArrayAsValue(configurationSection, "Uprawnienia PControl"),
                PcControlRightsForAllUsers = GetArrayFromSection(configurationSection, "Uprawnienia PControl dla wszystkich"),
                AmmsPersonnelUnitForArea = GetDictionaryFromSection(configurationSection, "Jednostki personelu w AMMS"),
                AmmsGroupsForPositionAndArea = GetDictionaryFromSectionWithTupleAsKeyAndArrayAsValue(configurationSection, "Grupy w AMMS"),
                AmmsGroupsForPosition = GetDictionaryFromSectionWithArrayAsValue(configurationSection, "Grupy w AMMS dla stanowiska"),
                AmmsPersonnelKindForPosition = GetDictionaryFromSection(configurationSection, "Rodzaj personelu w AMMS"),
                AmmsPersonnelFunctionForPosition = GetDictionaryFromSection(configurationSection, "Funkcja personelu w AMMS")
            };

            return output;
        }

        private string[] GetArrayFromSection(IConfigurationSection configurationSection, string section)
            => configurationSection.GetRequiredSection(section).GetChildren().Select(x => x.Value).ToArray();

        private Dictionary<string, string[]> GetDictionaryFromSectionWithArrayAsValue(IConfigurationSection configurationSection, string section)
            => configurationSection.GetRequiredSection(section).GetChildren().ToDictionary(x => x.Key, x => x.GetChildren().Select(y => y.Value).ToArray());

        private Dictionary<string, string> GetDictionaryFromSection(IConfigurationSection configurationSection, string section)
            => configurationSection.GetRequiredSection(section).GetChildren().ToDictionary(x => x.Key, x => x.Value);

        private Dictionary<(string, string), string[]> GetDictionaryFromSectionWithTupleAsKeyAndArrayAsValue(IConfigurationSection configurationSection, string section)
            => configurationSection.GetRequiredSection(section).GetChildren().ToDictionary(x => GetTupleFromString(x.Key), x => x.GetChildren().Select(y => y.Value).ToArray());

        private (string, string) GetTupleFromString(string tuple)
        {
            string[] splitted = tuple.Split(',').Select(x => x.Trim()).ToArray();
            return (splitted[0], splitted[1]);
        }

    }
}
