using ActiveDirectoryAccountsLibrary;
using AmmsAccountsLibrary;
using ConfigurationLibrary;
using CSharpFunctionalExtensions;
using EmailAccountsLibrary;
using ExcelReaderLibrary;
using MappingLibrary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium.Edge;
using PcControlAccountsLibrary;

namespace ExcelAccountsSyncConsoleUI
{
    internal class Program
    {
        static void Main()
        {
            GeneralConfig config = GetConfig();

            using ILoggerFactory factory = GetLoggerFactory();

            ILogger<Program> logger = factory.CreateLogger<Program>();

            IExcelReader excelReader = new ExcelReader(config.ExcelReaderConfig, new ParsingHelper(), new MappingHelper(config.ExcelMappingHelperConfig));

            int iod = GetIod();

            (_, bool excelReadingFailure, GeneralUserModel generalUserModel, string excelReadingError) = excelReader.Read(iod);

            if (excelReadingFailure)
            {
                LogErrorAndWait($"Błąd przy odczycie danych: {excelReadingError}", logger);
                return;
            }

            logger.LogInformation("Odczytano z pliku dane użytkownika: {generalUserModel.Login}", generalUserModel.Login);
            
            Thread.Sleep(500); // Console.Write jest za szybko
            
            Console.Write("Pager: ");
            string pager = Console.ReadLine().Trim();

            IMapper mapper = new Mapper(config.MapperConfig);

            bool runAd = GetRunningConfig(config.ProgramConfig.RunActiveDirectory, "Active Directory");
            bool runPControl = GetRunningConfig(config.ProgramConfig.RunPControl, "PControl");
            bool runAmms = GetRunningConfig(config.ProgramConfig.RunAmms, "AMMS");
            bool runZimbra = GetRunningConfig(config.ProgramConfig.RunZimbra, "Zimbra");

            if (runAd)
            {
                (_, bool isMappingAdUserFailure, ActiveDirectoryUserModel adUser, string adUserMappingError) = mapper.MapAdUser(generalUserModel, pager);

                if (isMappingAdUserFailure)
                {
                    LogErrorAndWait($"Błąd przy mapowaniu danych użytkownika w AD: {adUserMappingError}", logger);
                    return;
                }

                IActiveDirectoryAccountCreator activeDirectoryAccountCreator = new ActiveDirectoryAccountCreator(config.AdConfig);

                (_, bool isAdFailure, string adMessage, string adError) = activeDirectoryAccountCreator.CreateAccount(adUser);

                if (isAdFailure)
                {
                    LogErrorAndWait($"Błąd przy tworzeniu konta w AD: {adError}", logger);
                    return;
                }
                else
                {
                    logger.LogInformation("Raport z tworzenia konta w AD: {adMessage}", adMessage);
                }
            }

            if (runPControl)
            {
                (_, bool isMappingPcControlUserFailure, PcControlUserModel pcControlUser, string pcControlUserMappingError) = mapper.MapPcControlUser(generalUserModel, pager);

                if (isMappingPcControlUserFailure)
                {
                    LogErrorAndWait($"Błąd przy mapowaniu danych użytkownika w PControl: {pcControlUserMappingError}", logger);
                    return;
                }

                PcControlAccountCreator pcControlAccountCreator = new(factory.CreateLogger<PcControlAccountCreator>(), new EdgeDriver(), config.PcControlConfig);

                (_, bool isPControlAccountCreationFailure, string pControlAccountCreationError) = pcControlAccountCreator.CreateAccount(pcControlUser);

                if (isPControlAccountCreationFailure)
                {
                    LogErrorAndWait($"Błąd przy zakładaniu konta w PControl: {pControlAccountCreationError}", logger);
                    return;
                }
            }
            
            if (runAmms)
            {
                (_, bool isMappingAmmsUserFailure, AmmsUserModel ammsUser, string ammsUserMappingError) = mapper.MapAmmsUser(generalUserModel);

                if (isMappingAmmsUserFailure)
                {
                    LogErrorAndWait($"Błąd przy mapowaniu danych użytkownika w AMMS: {ammsUserMappingError}", logger);
                    return;
                }

                AmmsAccountCreator ammsAccountCreator = new(new EdgeDriver(), factory.CreateLogger<AmmsAccountCreator>(), config.AmmsConfig);

                (_, bool ammsFailure, string ammsError) = ammsAccountCreator.CreateAccount(ammsUser);

                if (ammsFailure)
                {
                    LogErrorAndWait($"Błąd przy tworzeniu konta w AMMS: {ammsError}", logger);
                    return;
                }
            }

            if (runZimbra && generalUserModel.Email is not null)
            {
                (_, bool isMappingEmailUserFailure, EmailUserModel emailUser, string emailUserMappingError) = mapper.MapEmailUser(generalUserModel);

                if (isMappingEmailUserFailure)
                {
                    LogErrorAndWait($"Błąd przy mapowaniu danych użytkownika w Zimbra: {emailUserMappingError}", logger);
                    return;
                }

                EmailAccountCreator emailAccountCreator = new(new EdgeDriver(), factory.CreateLogger<EmailAccountCreator>(), config.ZimbraConfig);

                (_, bool emailFailure, string emailError) = emailAccountCreator.CreateAccount(emailUser);

                if (emailFailure)
                {
                    LogErrorAndWait($"Błąd przy tworzeniu konta email: {emailError}", logger);
                    return;
                }
            }
        }

        private static bool GetRunningConfig(RunningSectionOption option, string module)
            => option switch
            {
                RunningSectionOption.Run => true,
                RunningSectionOption.DoNotRun => false,
                RunningSectionOption.Ask => AskIfRunModule(module),
                _ => throw new ArgumentOutOfRangeException(nameof(option))
            };

        private static bool AskIfRunModule(string module)
        {
            Console.Write($"Czy zakładać konto w {module}? [T/N]: ");

            while (true)
            {
                string userInput = Console.ReadLine().Trim().ToLower();
                if (userInput == "t")
                {
                    return true;
                } 
                if (userInput == "n")
                {
                    return false;
                }

                Console.Write($"Niedozwolona wartość. Czy zakładać konto w {module}? [T/N]: ");
            }
        }

        private static int GetIod()
        {
            Console.Write("Nr pracownika w pliku: ");
            int iod;
            while (!int.TryParse(Console.ReadLine(), out iod))
            {
                Console.WriteLine("Niepoprawna liczba. Spróbuj ponownie:");
            }

            return iod;
        }

        private static ILoggerFactory GetLoggerFactory()
            => LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter(logLevel => logLevel >= LogLevel.Information)
                    .AddConsole();
            });

        private static GeneralConfig GetConfig()
        {
            const string filename = "ustawienia.json";
            ConfigurationBuilder builder = new();
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(filename, optional: false, reloadOnChange: true);
            IConfiguration configuration = builder.Build();

            IConfigPreparer configPreparer = new ConfigPreparer();

            (_, bool isFailure, GeneralConfig config, string error) = configPreparer.GetConfig(configuration);

            if (isFailure)
            {
                Console.WriteLine($"Błąd przy wczytywaniu ustawień z pliku {filename}: {error}");
            }

            return config;
        }

        private static void LogErrorAndWait(string message, ILogger<Program> logger)
        {
            logger.LogError("{error}", message);
            Console.ReadKey();
        }
    }
}
