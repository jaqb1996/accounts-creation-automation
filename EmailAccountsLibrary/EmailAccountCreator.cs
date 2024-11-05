using OpenQA.Selenium;
using Microsoft.Extensions.Logging;
using CSharpFunctionalExtensions;
using ConfigurationLibrary;

namespace EmailAccountsLibrary
{
    public class EmailAccountCreator
    {
        private const string Domain = "@wss5.pl";
        private readonly IWebDriver driver;
        private readonly ILogger<EmailAccountCreator> logger;
        private readonly ZimbraConfig config;

        public EmailAccountCreator(IWebDriver driver, ILogger<EmailAccountCreator> logger, ZimbraConfig config)
        {
            this.driver = driver;
            this.logger = logger;
            this.config = config;
        }

        public Result CreateAccount(EmailUserModel user)
        {
            try
            {
                NavigateToPage();

                MediumPause();

                IWebElement searchInput = driver.FindElement(By.Id("_XForm_query_display"));

                searchInput.SendKeys($"{user.Username}{Domain}");

                ShortPause();

                driver.FindElement(By.Id("_XForm_dwt_button___container")).Click();

                ShortPause();

                searchInput.Clear();

                ShortPause();

                bool accountAlreadyPresent = true;

                try
                {
                    var _ = driver.FindElement(By.CssSelector("td[id^=SEARCH_MANAGE_data_emailaddress]"));
                }
                catch (NoSuchElementException)
                {
                    accountAlreadyPresent = false;
                }

                if (accountAlreadyPresent)
                {
                    logger.LogWarning($"Podane konto {user.Username}{Domain} już istnieje");

                    return Result.Success();
                }

                driver.FindElement(By.XPath("//*[text()='Strona główna']")).Click();

                ShortPause();

                driver.FindElement(By.Id("ztabv__HOMEV_output_6")).Click();

                ShortPause();

                driver.FindElement(By.Id("zdlgv__NEW_ACCT_name_2")).SendKeys(user.Username);

                ShortPause();

                driver.FindElement(By.Id("zdlgv__NEW_ACCT_givenName")).SendKeys(user.FirstName);

                ShortPause();

                driver.FindElement(By.Id("zdlgv__NEW_ACCT_sn")).SendKeys(user.LastName);

                ShortPause();

                for (int i = 0; i < 3; i++)
                {
                    driver.FindElement(By.Id("zdlg__NEW_ACCT_button12_title")).Click();
                    ShortPause();
                }

                IWebElement distributionListSearchInput = driver.FindElement(By.Id("zdlgv__NEW_ACCT_query"));

                foreach (string group in config.Groups)
                {
                    distributionListSearchInput.SendKeys(group);

                    ShortPause();

                    distributionListSearchInput.SendKeys(Keys.Enter);

                    ShortPause();

                    distributionListSearchInput.Clear();

                    ShortPause();

                    IWebElement groupRow = driver.FindElement(By.CssSelector("div[id^=zli__DWT]"));
                    groupRow.Click();

                    ShortPause();

                    groupRow.SendKeys(Keys.Enter);

                    ShortPause();
                }

                driver.FindElement(By.Id("zdlg__NEW_ACCT_button13_title")).Click();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        private void NavigateToPage()
        {
            driver.Navigate().GoToUrl("https://zimbra.wss5.net:7071/zimbraAdmin/");

            LongPause();

            driver.FindElement(By.Id("ZLoginUserName")).SendKeys(config.Login);

            ShortPause();

            driver.FindElement(By.Id("ZLoginPassword")).SendKeys(config.Password);

            ShortPause();

            driver.FindElement(By.Id("ZLoginButton")).Click();
        }

        private static void LongPause() => Thread.Sleep(7_000);

        private static void MediumPause() => Thread.Sleep(2000);

        private static void ShortPause() => Thread.Sleep(1000);
    }
}
