using ConfigurationLibrary;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace PcControlAccountsLibrary
{
    public class PcControlAccountCreator
    {
        private readonly ILogger<PcControlAccountCreator> logger;
        private readonly IWebDriver driver;
        private readonly PcControlConfig config;

        public PcControlAccountCreator(ILogger<PcControlAccountCreator> logger, IWebDriver driver, PcControlConfig config)
        {
            this.logger = logger;
            this.driver = driver;
            this.config = config;
        }

        public Result CreateAccount(PcControlUserModel user)
        {
            try
            {
                driver.Navigate().GoToUrl("http://10.0.0.73/accounts/login/?next=/data/personnel/Employee/");

                LongPause();

                driver.FindElement(By.Id("id_username")).SendKeys(config.UserName);

                ShortPause();

                driver.FindElement(By.Id("id_password")).SendKeys(config.Password);

                ShortPause();

                driver.FindElement(By.Id("id_login")).Click();

                LongPause();

                if (!string.IsNullOrEmpty(user.CardNumber))
                {
                    IWebElement cardSearchInput = driver.FindElement(By.Id("search_id_Card"));
                    cardSearchInput.SendKeys(user.CardNumber);

                    ShortPause();

                    cardSearchInput.SendKeys(Keys.Enter);

                    MediumPause();

                    IWebElement editLink = null;
                    bool cardAlreadyExists = true;
                    try
                    {
                        editLink = driver.FindElement(By.XPath($"//td[@title='{user.CardNumber}']/following-sibling::td[@id='id_td_row_menu']//li[@id='id_op_edit']//a"));
                    }
                    catch (NoSuchElementException)
                    {
                        cardAlreadyExists = false;
                    }

                    if (cardAlreadyExists)
                    {
                        editLink.Click();

                        LongPause();

                        IWebElement inputName = driver.FindElement(By.XPath("//input[@name='EName']"));
                        string name = inputName.GetAttribute("value");
                        logger.LogInformation("Usuwanie karty użytkownika: {name}", name);

                        driver.FindElement(By.XPath("//input[@id='id_Card']")).Clear();

                        ShortPause();

                        driver.FindElement(By.Id("OK")).Click();

                        LongPause();
                    }
                }

                driver.FindElement(By.Id("id__add")).Click();

                LongPause();

                driver.FindElement(By.Id("id_PIN")).SendKeys(config.IdPrefix + user.Iod);

                ShortPause();

                driver.FindElement(By.Id("id_EName")).SendKeys(user.Name);

                ShortPause();

                driver.FindElement(By.Id("id_lastname")).SendKeys(user.LastName);

                ShortPause();

                if (!string.IsNullOrEmpty(user.CardNumber))
                {
                    driver.FindElement(By.Id("id_Card")).SendKeys(user.CardNumber);
                }

                driver.FindElement(By.Id("id_drop_dept")).Click();

                ShortPause();

                driver.FindElement(By.XPath($"//div[@id='id_dept']//p[text()='{user.Department}']")).Click();

                ShortPause();

                driver.FindElement(By.Id("id_identitycard")).SendKeys(user.Pesel);

                ShortPause();

                driver.FindElement(By.Id("id_Political")).SendKeys(user.Pesel);

                ShortPause();

                foreach (string right in user.Rights)
                {
                    driver.FindElement(By.XPath($"//ul[@id='levelSingleBrowser']//p[text()='{right}']/preceding-sibling::input[1]")).Click();

                    ShortPause();

                    logger.LogInformation("Nadano uprawnienie {right}", right);
                }

                if (user.DateTo is DateTime dateTo)
                {
                    driver.FindElement(By.Id("id_set_valid_time")).Click();

                    ShortPause();

                    driver.FindElement(By.Id("id_acc_startdate")).SendKeys(user.DateFrom.ToString("yyyy-MM-dd"));
                    
                    ShortPause();

                    driver.FindElement(By.Id("id_acc_enddate")).SendKeys(dateTo.ToString("yyyy-MM-dd"));

                    ShortPause();
                }

                driver.FindElement(By.Id("OK")).Click();

                LongPause();

                driver.Quit();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        private static void LongPause() => Thread.Sleep(5000);

        private static void MediumPause() => Thread.Sleep(2000);

        private static void ShortPause() => Thread.Sleep(1000);
    }
}
