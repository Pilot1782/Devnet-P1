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
            // Make chrome headless
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");

            _driver = new ChromeDriver(chromeOptions);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(45));
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
            search.Clear();
            search.SendKeys(addr);
            search.SendKeys(Keys.Enter);

            // Wait for the search results to load
            _wait.Until(
                SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.Id("dijit_layout_TabContainer_0")
                )
            );

            // Get the parcel ID
            _wait.Until(
                SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.ClassName("field-ACCOUNT")
                )
            );

            var plist = _driver.FindElements(By.ClassName("field-ACCOUNT"));

            if (plist.Count() > 0)
            {
                return plist[1].Text;
            } else
            {
                return "notfound";
            }
        }

        public Dictionary<string, string> GetKeyData(string parcelId)
        {
            this._driver.Navigate().GoToUrl("https://www.ccappraiser.com/Show_parcel.asp?acct="+ parcelId +"&gen=T&tax=F&bld=F&oth=F&sal=F&lnd=F&leg=T");

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
                if (cells[i].Text.IndexOf("Owner:") > -1) // Owner starts with "Owner:" word, seperated by \n
                {
                    results.Add("owner", cells[i].Text.Split("\n")[1]);
                } else if (cells[i].Text == "Section/Township/Range:") // S/T/R is one string seperated by dashes, so split them
                {
                    var temp = cells[i + 1].Text.Split("-");
                    results.Add("section", temp[0]);
                    results.Add("township", temp[1]);
                    results.Add("range", temp[2]);
                } else if (cells[i].Text.IndexOf("Long Legal:") > -1) // Long Legal is split by \n
                {
                    results.Add("legal", cells[i].Text.Split("\n")[1]);
                } else if (cells[i].Text == "Property Address: ") // Address is the cell after "Property Address: ", split by "\n" if there's multiple
                {
                    results.Add("address", cells[i + 1].Text.Split("\n")[0]);
                } else if (cells[i].Text == "Property City & Zip: ") // City and zip is the cell after "Property City & Zip: "
                {
                    results.Add("city", cells[i + 1].Text);
                }
            }
            return results;
        }
    }
}