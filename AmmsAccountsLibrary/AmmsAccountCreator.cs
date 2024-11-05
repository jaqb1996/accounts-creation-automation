using ConfigurationLibrary;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace AmmsAccountsLibrary;

public class AmmsAccountCreator
{
    private readonly IWebDriver driver;
    private readonly ILogger<AmmsAccountCreator> logger;
    private readonly AmmsConfig config;
    private readonly int delayAfterNavigationToUrlInSeconds = 10;

    public AmmsAccountCreator(IWebDriver driver, ILogger<AmmsAccountCreator> logger, AmmsConfig config)
    {
        this.driver = driver;
        this.logger = logger;
        this.config = config;
    }

    public Result CreateAccount(AmmsUserModel user)
    {
        try
        {
            NavigateToPage();

            driver.SwitchTo().DefaultContent();
            driver.SwitchTo().Frame("vsmapp_anacc3");

            user.Username = user.Username.ToUpper();

            IWebElement nameInput = driver.FindElement(By.Id("tiNazwa"));
            MediumPause();
            nameInput.SendKeys(user.Username);
            MediumPause();
            nameInput.SendKeys(Keys.Enter);

            MediumPause();

            bool usernameAlreadyExists = true;
            try
            {
                driver.FindElement(By.XPath($"//ax-sg-default-renderer[text()='{user.Username}']"));
            }
            catch (NoSuchElementException)
            {
                usernameAlreadyExists = false;
            }

            if (usernameAlreadyExists)
            {
                logger.LogWarning("Użytkownik o podanej nazwie ({user.Username}) już istnieje", user.Username);
                return Result.Success();
            }

            driver.FindElement(By.XPath("//button[text()='Dodaj']")).Click();

            MediumPause();

            driver.FindElement(By.Id("tiUserNazwa")).SendKeys(user.Username);

            ShortPause();

            driver.FindElement(By.Id("tiUserNazwisko")).SendKeys(user.LastName);

            ShortPause();

            driver.FindElement(By.Id("tiUserImiona")).SendKeys(user.FirstName);

            ShortPause();

            driver.FindElement(By.Id("tiPESEL")).SendKeys(user.Pesel);

            ShortPause();

            // TODO: poprawa daty blokowania - zawsze jest 27-05-2024

            //if (user.BlockingAccountDate is DateTime blockingAccountDate)
            //{
            //    IWebElement dateToElement = driver.FindElement(By.XPath("//ax-date-input[@id='diDataAutomatycznejBlokadyKonta']//input"));

            //    dateToElement.Click();

            //    ShortPause();

            //    string blockingAccountDateText = blockingAccountDate.ToString("dd-MM-yyyy");
            //    dateToElement.SendKeys(blockingAccountDateText);

            //    ShortPause();
            //}

            IWebElement newPasswordInput = driver.FindElement(By.Id("tiNoweHaslo"));
            ShortPause();
            newPasswordInput.Clear();
            ShortPause();
            newPasswordInput.SendKeys(config.NewPassword);

            driver.FindElement(By.XPath("//ax-dict-text-input[@id='dtiPersonel']//button[@type='button']")).Click();

            ShortPause();

            if (user.Npwz is null) // jeśli nie ma NPWZ, można nie szukać istniejącego personelu, tylko od razu dodawać
            {
                AddPersonnel(user);
            }
            else
            {
                driver.FindElement(By.Id("ax-dictionary-folders-button")).Click();

                ShortPause();

                IWebElement npwzInput = driver.FindElement(By.Id("iNrPrawa"));
                npwzInput.SendKeys(user.Npwz);

                ShortPause();

                npwzInput.SendKeys(Keys.Enter);

                MediumPause();

                bool personnelAlreadyInDatabase = true;
                try
                {
                    driver.FindElement(By.XPath($"//td[@id='row-shortName']/ax-sg-default-renderer[text()='{user.Npwz}']")).Click();

                    logger.LogWarning("Personel już istnieje. Należy zweryfikować ręcznie dane personelu.");

                    ShortPause();

                    driver.FindElement(By.Id("ax-dictionary-select-button")).Click();

                    MediumPause();
                }
                catch (NoSuchElementException)
                {
                    personnelAlreadyInDatabase = false;
                }

                if (!personnelAlreadyInDatabase)
                {
                    AddPersonnel(user);
                }
            }

            driver.FindElement(By.XPath("//p-tabpanel[@id='uzytkownikWorkspace']//button[text()='Zapisz']")).Click();

            ShortPause();

            driver.FindElement(By.Id("button1")).Click();

            LongPause();

            driver.FindElement(By.XPath("//span[text()='Grupy użytkowników']")).Click();

            MediumPause();

            foreach (string group in user.Groups)
            {
                IWebElement groupInput = driver.FindElement(By.Id("tiNazwa"));

                groupInput.Clear();

                ShortPause();

                groupInput.SendKeys(group);

                ShortPause();

                driver.FindElement(By.Id("btnSearch")).Click();

                ShortPause();

                driver.FindElement(By.XPath($"//p-tabpanel[@id='grupyWorkspace']//ax-sg-default-renderer[text()='{group}']")).Click();

                ShortPause();

                driver.FindElement(By.Id("btnDodajRole")).Click();

                logger.LogInformation("Dodano grupę {group}", group);

                ShortPause();
            }

            driver.FindElement(By.XPath("//p-tabpanel[@id='grupyWorkspace']//button[text()='Zapisz']")).Click();

            MediumPause();

            driver.FindElement(By.XPath("//button[@aria-label='Powrót do ekranu domowego']")).Click();

            MediumPause();

            driver.SwitchTo().DefaultContent();
            driver.SwitchTo().Frame("vsmapp_anshell2");

            driver.FindElement(By.XPath("//span[text()='Użytkownicy domenowi']")).Click();

            LongPause();

            FindDomainUsersFrame();

            driver.FindElement(By.Id("add")).Click();

            ShortPause();

            driver.FindElement(By.XPath("//ax-dict-text-input[@id='diDbapUzytkNieprzyp']//input[@type='text']")).SendKeys(user.Username);

            ShortPause();

            driver.FindElement(By.XPath("//ax-dict-text-input[@id='diAdUzytkNieprzyp']//input[@type='text']")).SendKeys($"{user.Username}@WSS5.NET");

            ShortPause();

            driver.FindElement(By.Id("accept")).Click();

            MediumPause();

            driver.Quit();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    private void NavigateToPage()
    {
        driver.Navigate().GoToUrl("http://amms.wss5.net/index.html");

        Thread.Sleep(GetDelayInMs(delayAfterNavigationToUrlInSeconds));

        driver.SwitchTo().Frame("vsmapp_anboot0");

        driver.FindElement(By.CssSelector("button[aria-label='Zmień pulpit']")).Click();

        MediumPause();

        driver.FindElement(By.CssSelector("button[aria-label='Panel konfiguracyjny']")).Click();

        MediumPause();

        driver.FindElement(By.XPath("//*[text()='Moduł panelu administracyjnego części białej']")).Click();

        LongPause();

        driver.SwitchTo().DefaultContent();
        driver.SwitchTo().Frame("vsmapp_anshell2");

        driver.FindElement(By.Id("element-2")).Click();

        ShortPause();

        driver.FindElement(By.XPath("//span[text()='Użytkownicy']")).Click();

        LongPause();
    }

    private void FindDomainUsersFrame()
    {
        int[] frameNumbersToCheck = [5, 4, 6, 3, 7, 2, 8, 1, 9];
        int i = 0;
        while (true)
        {
            try
            {
                driver.SwitchTo().DefaultContent();
                driver.SwitchTo().Frame($"vsmapp_ancnf{frameNumbersToCheck[i]}");
                break;
            }
            catch (NoSuchFrameException)
            {
                if (++i == frameNumbersToCheck.Length)
                {
                    logger.LogError("Frame not found");
                    throw;
                }
                ShortPause();
            }
        }
    }

    private void AddPersonnel(AmmsUserModel user)
    {
        ShortPause();

        driver.FindElement(By.Id("ax-dictionary-add-button")).Click();

        LongPause();

        driver.SwitchTo().DefaultContent();
        driver.SwitchTo().Frame("vsmapp_msdict4");

        driver.FindElement(By.Id("btnAddInstitution")).Click();

        MediumPause();

        driver.FindElement(By.Id("ax-dictionary-select-button")).Click();

        MediumPause();

        driver.FindElement(By.Id("tiNazwisko")).SendKeys(user.LastName);

        ShortPause();

        driver.FindElement(By.Id("tiImie")).SendKeys(user.FirstName);

        ShortPause();

        driver.FindElement(By.XPath("//ax-dict-combo-box[@id='dcRodzajPersonelu']//div[@role='button']")).Click();

        ShortPause();

        driver.FindElement(By.XPath($"//ul[@role='listbox']/p-dropdownitem[@aria-label='{user.PersonnelKind}']")).Click();

        ShortPause();

        driver.FindElement(By.Id("tiPesel")).SendKeys(user.Pesel);

        ShortPause();

        if (user.Npwz is not null)
        {
            driver.FindElement(By.Id("tiNrPrawaZawodu")).SendKeys(user.Npwz);

            ShortPause();

            driver.FindElement(By.XPath("//ax-dict-combo-box[@id='dcRodzajPersoneluItem']//div[@role='button']")).Click();

            ShortPause();

            driver.FindElement(By.XPath($"//ul[@role='listbox']/p-dropdownitem[@aria-label='{user.PersonnelKind}']")).Click();

            ShortPause();
        }
        else
        {
            driver.FindElement(By.Id("tiNrPrawaZawodu")).Click();

            ShortPause();

            driver.FindElement(By.XPath("//button[@id='btnDeleteNpwz']")).Click();

            ShortPause();

            driver.FindElement(By.Id("button1")).Click();

            ShortPause();
        }

        driver.FindElement(By.Id("units-label")).Click();

        MediumPause();

        foreach (string unitCode in user.PersonnelUnits)
        {
            driver.FindElement(By.Id("btnAddUnit")).Click();

            MediumPause();

            driver.FindElement(By.Id("ax-dictionary-folders-button")).Click();

            ShortPause();

            IWebElement unitSearchInput = driver.FindElement(By.Id("dictSearchFilter"));
            unitSearchInput.SendKeys(unitCode);

            ShortPause();

            unitSearchInput.SendKeys(Keys.Enter);

            MediumPause();

            driver.FindElement(By.XPath($"//td[@id='row-codeMnem']/ax-sg-default-renderer[text()='{unitCode}']")).Click();

            ShortPause();

            driver.FindElement(By.Id("ax-svg-icon-one-right")).Click();

            ShortPause();

            driver.FindElement(By.Id("ax-dictionary-select-button")).Click();

            MediumPause();
        }

        foreach (IWebElement functionButton in driver.FindElements(By.XPath("//td[@id='units-row-diFunkcja']//div[@role='button']")))
        {
            functionButton.Click();

            ShortPause();

            driver.FindElement(By.XPath($"//p-dropdownitem[@aria-label='{user.PersonnelFunction}']")).Click();

            ShortPause();
        }

        foreach (IWebElement kindButton in driver.FindElements(By.XPath("//td[@id='units-row-diRpersonelu']//div[@role='button']")))
        {
            kindButton.Click();

            ShortPause();

            driver.FindElement(By.XPath($"//p-dropdownitem[@aria-label='{user.PersonnelKind}']")).Click();

            ShortPause();
        }

        driver.FindElement(By.Id("btnZapisz")).Click();

        MediumPause();

        driver.FindElement(By.Id("button1")).Click();

        LongPause();

        driver.SwitchTo().DefaultContent();
        driver.SwitchTo().Frame("vsmapp_anacc3");

        driver.FindElement(By.XPath("//div[@class='modal-footer']//button[@id='ax-dictionary-cancel-button']")).Click();

        MediumPause();

        IWebElement peselInput = driver.FindElement(By.Id("tiPESEL"));
        peselInput.Clear();

        ShortPause();

        IWebElement peselLabel = driver.FindElement(By.Id("lbPesel"));
        peselLabel.Click();

        ShortPause();

        peselInput.SendKeys(user.Pesel);

        ShortPause();

        peselLabel.Click();
    }

    private static int GetDelayInMs(double delayInSeconds) => (int)(delayInSeconds * 1000);

    private static void LongPause() => Thread.Sleep(5000);

    private static void MediumPause() => Thread.Sleep(2000);

    private static void ShortPause() => Thread.Sleep(1000);
}
