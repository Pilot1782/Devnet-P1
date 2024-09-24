using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;

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
            chromeOptions.AddArgument("headless"); // Hiding chrome instance

            _driver = new ChromeDriver(chromeDriverService, chromeOptions);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            _driver.Navigate().GoToUrl("https://agis.charlottecountyfl.gov/ccgis/");

            // Wait for the search bar to load
            _wait.Until(
                SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.Id("esri_dijit_Geocoder_0_input")
                )
            );
        }

        public void Shutdown()
        {
            _driver.Close();
            _driver.Quit();
        }

        public string GetPiD(string addr, Label debugLabel)
        {
            if (_driver.Url.Contains("https://www.ccappraiser.com/"))
            {
                _driver.Navigate().GoToUrl("https://agis.charlottecountyfl.gov/ccgis/");
            } else
            {
                _driver.Navigate().Refresh();
            }

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

                try
                {
                    var toggle = _driver.FindElement(By.CssSelector("div[title='Show Table']"));
                    toggle.Click();
                }
                catch { }

                MainThread.InvokeOnMainThreadAsync(() => {
                    debugLabel.Text = "Starting scraping of " + addr + " (Attempt #2)";
                });

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
                    return "notfound";
                }
            }

            Debug.WriteLine("Scrap Addr: " + addr);

            // Get the parcel ID
            _wait.Until(
                SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
                    By.ClassName("field-ACCOUNT")
                )
            );

            var plist = _driver.FindElements(By.ClassName("field-ACCOUNT"));

            return plist.Count != 0 ? plist[1].Text : "notfound";
        }

        public static Dictionary<string, string> GetKeyDataAsync(string parcelId)
        {
            var results = new Dictionary<string, string> { { "parcelId", parcelId } };

            var url = "https://www.ccappraiser.com/Show_parcel.asp?acct=" + parcelId +
                      "&gen=T&tax=T&bld=F&oth=F&sal=F&lnd=F&leg=T";
            var web = new HtmlWeb();
            var htmlDoc = web.Load(url);

            results["owner"] = FixText(htmlDoc.DocumentNode.SelectSingleNode(
                "//h2[text()='Owner:']/../div[@class='w3-border w3-border-blue']"
            ).InnerText, includeNewLine: true).Split("\n")[0];

            var sectionTownshipRange = FixText(htmlDoc.DocumentNode.SelectSingleNode(
                "(//div/strong[text()='Section/Township/Range:']/../../div)[2]"
            ).InnerText).Split("-");
            results["section"] = sectionTownshipRange[0];
            results["township"] = sectionTownshipRange[1];
            results["range"] = sectionTownshipRange[2];

            results["legal"] = FixText(htmlDoc.DocumentNode.SelectSingleNode(
                "//strong[text()='Long Legal:']/.."
            ).InnerText.Replace("Long Legal:", ""));

            results["address"] = FixText(htmlDoc.DocumentNode.SelectSingleNode(
                "//h2[text()='Property Location:']/../div[1]/div[2]"
            ).InnerText, includeNewLine: true).Split("\n")[0];

            results["city"] = FixText(htmlDoc.DocumentNode.SelectSingleNode(
                "//h2[text()='Property Location:']/../div[2]/div[2]"
            ).InnerText);

            return results;
        }

        private static string FixText(string text, bool includeNewLine = false)
        {
            if (!includeNewLine)
                text = text.Replace("\n", "");

            return text
                .Replace("\r", "")
                .Replace("\t", "")
                .Replace("  ", "")
                .Replace("&nbsp;", "")
                .Trim();
        }
    }
}