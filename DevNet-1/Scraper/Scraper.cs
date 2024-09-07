using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace DevNet_1.Scraper
{
    class Scraper
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public Scraper()
        {
            _driver = new ChromeDriver();
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        }

        public String GetPiD(string addr)
        {
            this._driver.Navigate().GoToUrl("https://agis.charlottecountyfl.gov/ccgis/");

            // Wait for the search bar to load
            _wait.Until(
                SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.Id("esri_dijit_Geocoder_0_input")
                )
            );

            // Enter the address into the search bar
            var search = _driver.FindElement(By.Id("esri_dijit_Geocoder_0_input"));
            search.SendKeys(addr);
            search.SendKeys(Keys.Enter);

            // Wait for the search results to load
            _wait.Until(
                SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.Id("dijit_layout_TabContainer_0")
                )
            );

            // Get the parcel ID
            var plist = _driver.FindElement(
                By.XPath("//div[@id='Property Ownership']/div/div[1]")
            );

            Console.WriteLine(plist.Text);
            return plist.Text;
        }
    }
}