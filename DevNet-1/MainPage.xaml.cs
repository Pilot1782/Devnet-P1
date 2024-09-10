using Devnet_1.Scraper;
using Newtonsoft.Json;
using System.Diagnostics;
namespace Devnet_1
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();

            Task.Run(RunScraper);
        }

        private void RunScraper()
        {
            var scrap = new Scraper.Scraper();

            Debug.WriteLine("Starting scraping...");

            var addr = "18500 Murdock Circle";
            var pid = scrap.GetPiD(addr);

            if (pid != "notfound")
            {
                Debug.WriteLine("Got ParcelID, getting data...");
            }
            else
            {
                Debug.WriteLine("Failed to locate property with address: " + addr);
                return;
            }

            var keyData = scrap.GetKeyData(pid);
            Debug.WriteLine(JsonConvert.SerializeObject(keyData));
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

}
