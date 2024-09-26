using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using static Microsoft.Maui.Storage.FileSystem;

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
            chromeOptions.AddArgument("--headless=old"); // Hiding chrome instance

            _driver = new ChromeDriver(chromeDriverService, chromeOptions);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            
            _driver.Navigate().GoToUrl("https://agis.charlottecountyfl.gov/ccgis/");
        }

        public void Shutdown()
        {
            _driver.Close();
            _driver.Quit();
        }

        public string GetPiD(string addr, Label debugLabel)
        {
            _driver.Navigate().Refresh();

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

        public async Task<string> GetPidLocal(string addr)
        {
            var csvReader = await OpenAppPackageFileAsync("gis.csv");

            var lines = new List<string>();
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            using var reader = new StreamReader(csvReader);
            while (!reader.EndOfStream)
            {
                lines.Add(await reader.ReadLineAsync() ?? string.Empty);
            }

            addr = StreetAbv(addr).ToLower().Trim();

            // Binary search for the address
            var min = 0;
            var max = lines.Count - 1;
            while (min <= max)
            {
                var mid = (min + max) / 2;
                var line = lines[mid].Split(",");
                var lineAddr = line[0].ToLower().Trim();
                var linePid = line[1];

                if (lineAddr == addr)
                {
                    return linePid;
                }

                if (string.Compare(lineAddr, addr, StringComparison.Ordinal) < 0)
                {
                    min = mid + 1;
                }
                else
                {
                    max = mid - 1;
                }
            }

            return "";
        }

        private static string StreetAbv(string street)
        {
            var abv = new Dictionary<string, string>()
            {
                { "Alley", "ALY" },
                { "Club", "CLB" },
                { "Flat", "FLT" },
                { "Inlet", "INLT" },
                { "Annex", "ANX" },
                { "Common", "CMN" },
                { "Flats", "FLTS" },
                { "Island", "IS" },
                { "Apartment", "APT" },
                { "Corner", "COR" },
                { "Floor", "FL" },
                { "Islands", "ISS" },
                { "Arcade", "ARC" },
                { "Corners", "CORS" },
                { "Ford", "FRD" },
                { "Isle", "ISLE" },
                { "Avenue", "AVE" },
                { "Course", "CRSE" },
                { "Fords", "FRDS" },
                { "Junction", "JCT" },
                { "Basement", "BSMT" },
                { "Court", "CT" },
                { "Forest", "FRST" },
                { "Junctions", "JCTS" },
                { "Bayou", "BYU" },
                { "Courts", "CTS" },
                { "Forge", "FRG" },
                { "Key", "KY" },
                { "Beach", "BCH" },
                { "Cove", "CV" },
                { "Forges", "FRGS" },
                { "Keys", "KYS" },
                { "Bend", "BND" },
                { "Coves", "CVS" },
                { "Fork", "FRK" },
                { "Knoll", "KNL" },
                { "Bluff", "BLF" },
                { "Creek", "CRK" },
                { "Forks", "FRKS" },
                { "Knolls", "KNLS" },
                { "Bluffs", "BLFS" },
                { "Crescent", "CRES" },
                { "Fort", "FT" },
                { "Lake", "LK" },
                { "Bottom", "BTM" },
                { "Crest", "CRST" },
                { "Freeway", "FWY" },
                { "Lakes", "LKS" },
                { "Boulevard", "BLVD" },
                { "Crossing", "XING" },
                { "Front", "FRNT" },
                { "Land", "LAND" },
                { "Branch", "BR" },
                { "Crossroad", "XRD" },
                { "Garden", "GDN" },
                { "Landing", "LNDG" },
                { "Bridge", "BRG" },
                { "Curve", "CURV" },
                { "Gardens", "GDNS" },
                { "Lane", "LN" },
                { "Brook", "BRK" },
                { "Dale", "DL" },
                { "Gateway", "GTWY" },
                { "Light", "LGT" },
                { "Brooks", "BRKS" },
                { "Dam", "DM" },
                { "Glen", "GLN" },
                { "Lights", "LGTS" },
                { "Building", "BLDG" },
                { "Department", "DEPT" },
                { "Glens", "GLNS" },
                { "Loaf", "LF" },
                { "Burg", "BG" },
                { "Divide", "DV" },
                { "Green", "GRN" },
                { "Lobby", "LBBY" },
                { "Burgs", "BGS" },
                { "Drive", "DR" },
                { "Greens", "GRNS" },
                { "Lock", "LCK" },
                { "Bypass", "BYP" },
                { "Drives", "DRS" },
                { "Grove", "GRV" },
                { "Locks", "LCKS" },
                { "Camp", "CP" },
                { "Estate", "EST" },
                { "Groves", "GRVS" },
                { "Lodge", "LDG" },
                { "Canyon", "CYN" },
                { "Estates", "ESTS" },
                { "Hangar", "HNGR" },
                { "Loop", "LOOP" },
                { "Cape", "CPE" },
                { "Expressway", "EXPY" },
                { "Harbor", "HBR" },
                { "Lot", "LOT" },
                { "Causeway", "CSWY" },
                { "Extension", "EXT" },
                { "Harbors", "HBRS" },
                { "Lower", "LOWR" },
                { "Center", "CTR" },
                { "Centers", "CTRS" },
                { "Fall", "FALL" },
                { "Heights", "HTS" },
                { "Manor", "MNR" },
                { "Circle", "CIR" },
                { "Circles", "CIRS" },
                { "Ferry", "FRY" },
                { "Hill", "HL" },
                { "Meadow", "MDW" },
                { "Mill", "ML" },
                { "Mills", "MLS" },
                { "Plains", "PLNS" },
                { "Shores", "SHRS" },
                { "Trailer", "TRLR" },
                { "Mission", "MSN" },
                { "Point", "PT" },
                { "Skyway", "SKWY" },
                { "Turnpike", "TPKE" },
                { "Motorway", "MTWY" },
                { "Points", "PTS" },
                { "Slip", "SLIP" },
                { "Underpass", "UPAS" },
                { "Mount", "MT" },
                { "Port", "PRT" },
                { "Space", "SPC" },
                { "Union", "UN" },
                { "Mountain", "MTN" },
                { "Prairie", "PR" },
                { "Spring", "SPG" },
                { "Unions", "UNS" },
                { "Mountains", "MTNS" },
                { "Neck", "NCK" },
                { "Ramp", "RAMP" },
                { "Spur", "SPUR" },
                { "Upper", "UPPR" },
                { "Office", "OFC" },
                { "Ranch", "RNCH" },
                { "Spurs", "SPUR" },
                { "Valley", "VLY" },
                { "Orchard", "ORCH" },
                { "Rapid", "RPD" },
                { "Square", "SQ" },
                { "Valleys", "VLYS" },
                { "Oval", "OVAL" },
                { "Rapids", "RPDS" },
                { "Squares", "SQS" },
                { "Viaduct", "VIA" },
                { "Overpass", "OPAS" },
                { "Rear", "REAR" },
                { "Station", "STA" },
                { "View", "VW" },
                { "Park", "PARK" },
                { "Rest", "RST" },
                { "Stop", "STOP" },
                { "Views", "VWS" },
                { "Ridge", "RDG" },
                { "Ridges", "RDGS" },
                { "Stream", "STRM" },
                { "Villages", "VLGS" },
                { "Parkway", "PKWY" },
                { "River", "RIV" },
                { "Street", "ST" },
                { "Ville", "VL" },
                { "Pass", "PASS" },
                { "Road", "RD" },
                { "Streets", "STS" },
                { "Vista", "VIS" },
                { "Passage", "PSGE" },
                { "Routes", "RTE" },
                { "Summit", "SMT" },
                { "Walk", "WALK" },
                { "Penthouse", "PH" },
                { "Row", "ROW" },
                { "Terrace", "TER" },
                { "Walks", "WALK" },
                { "Pier", "PIER" },
                { "Rue", "RUE" },
                { "Throughway", "TRWY" },
                { "Wall", "WALL" },
                { "Pike", "PIKE" },
                { "Run", "RUN" },
                { "Trace", "TRCE" },
                { "Way", "WAY" },
                { "Pine", "PNE" },
                { "Shoal", "SHL" },
                { "Track", "TRAK" },
                { "Ways", "WAYS" },
                { "Pines", "PNES" },
                { "Shoals", "SHLS" },
                { "Trafficway", "TRFY" },
                { "Well", "WL" },
                { "Place", "PL" },
                { "Shore", "SHR" },
                { "Trail", "TRL" },
                { "Wells", "WLS" },
                { "Plain", "PLN" }
            };

            var @out = "";
            foreach (var word in street.Split(" "))
            {
                if (abv.TryGetValue(word, out var value))
                {
                    @out += value + " ";
                }
                else
                {
                    @out += word + " ";
                }
            }

            return @out;
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