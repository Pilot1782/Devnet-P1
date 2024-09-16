using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Devnet_P11.Scraper
{
    public class Scraper
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public Scraper()
        {
            var chromeDriverService = ChromeDriverService.CreateDefaultService();
            chromeDriverService.HideCommandPromptWindow = true; // Hiding CMD window

            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless"); // Hiding chrome instance

            _driver = new ChromeDriver(chromeDriverService, chromeOptions);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        }

        public String GetPiD(string addr)
        {
            _driver.Navigate().GoToUrl("https://agis.charlottecountyfl.gov/ccgis/");

            // Wait for the search bar to load
            _wait.Until(
                SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.Id("esri_dijit_Geocoder_0_input")
                )
            );

            // Enter the address into the search bar
            var search = _driver.FindElement(By.Id("esri_dijit_Geocoder_0_input"));
            search.Clear();
            search.SendKeys(addr);
            search.SendKeys(Keys.Enter);

            // Wait for the search results to load
            try
            {
                _wait.Until(
                    SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                        By.Id("dijit_layout_TabContainer_0")
                    )
                );
            }
            catch (WebDriverTimeoutException)
            {
                // try and toggle the table on

                var toggle = _driver.FindElement(By.CssSelector("div[title='Show Table']"));
                toggle.Click();

                try {
                    _wait.Until(
                        SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                            By.Id("dijit_layout_TabContainer_0")
                        )
                    );
                } catch (WebDriverTimeoutException) {
                    return "notfound";
                }
            }

            // Get the parcel ID
            _wait.Until(
                SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.ClassName("field-ACCOUNT")
                )
            );

            var plist = _driver.FindElements(By.ClassName("field-ACCOUNT"));

            return plist.Count != 0 ? plist[1].Text : "notfound";
        }

        public Dictionary<string, string> GetKeyData(string parcelId)
        {
            this._driver.Navigate().GoToUrl("https://www.ccappraiser.com/Show_parcel.asp?acct=" + parcelId +
                                            "&gen=T&tax=F&bld=F&oth=F&sal=F&lnd=F&leg=T");

            // Wait for the first container to load
            _wait.Until(
                SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.ClassName("w3-container")
                )
            );

            // All cells have this tag, so get all of them
            var cells = _driver.FindElements(By.ClassName("w3-cell"));

            Dictionary<string, string> results = new Dictionary<string, string>();

            // Loop through cells
            for (int i = 0; i < cells.Count; i++)
            {
                switch (cells[i].Text)
                {
                    case var s when s.Contains("Owner:"):
                        results.Add("owner", cells[i].Text.Split("\n")[1].Replace("\r", ""));
                        break;
                    case "Section/Township/Range:":
                        var temp = cells[i + 1].Text.Split("-");
                        results.Add("section", temp[0]);
                        results.Add("township", temp[1]);
                        results.Add("range", temp[2]);
                        break;
                    case var s when s.Contains("Long Legal:"):
                        results.Add("legal", cells[i].Text.Split("\n")[1]);
                        break;
                    case "Property Address: ":
                        results.Add("address", cells[i + 1].Text.Split("\n")[0].Replace("\r", ""));
                        break;
                    case "Property City & Zip: ":
                        results.Add("city", cells[i + 1].Text);
                        break;
                }
            }

            return results;
        }
    }
}