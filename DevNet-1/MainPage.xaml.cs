using Newtonsoft.Json;
using System.Diagnostics;
namespace Devnet_1
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void RunScraper(String addr)
        {
            var scrap = new Scraper.Scraper();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ProgressLabel.Progress = 0.33;
            });
            Debug.WriteLine("Starting scraping...");

            var pid = scrap.GetPiD(addr);

            if (pid != "notfound")
            {
                Debug.WriteLine("Got ParcelID, getting data...");
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ErrorLabel.Text = "Address not found.";
                });
                Debug.WriteLine("Failed to locate property with address: " + addr);
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ProgressLabel.Progress = 0.66;
            });

            var keyData = scrap.GetKeyData(pid);
            Debug.WriteLine(JsonConvert.SerializeObject(keyData));

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ProgressLabel.Progress = 1;

                OwnerLabel.Text = $"Owner: {keyData["owner"]}";
                AddressLabel.Text = $"Address: {keyData["address"]}, {keyData["city"]}";
                STRLabel.Text = $"Section: {keyData["section"]}, Township: {keyData["township"]}, Range: {keyData["range"]}";
                LegalLabel.Text = $"Legal Description: {keyData["legal"]}";
            });
        }

        private void OnRunClicked(object sender, EventArgs e)
        {
            if (AddressInput.Text != "")
            {
                ProgressLabel.IsVisible = true;
                ProgressLabel.Progress = 0;
                Task.Run(() =>
                {
                    RunScraper(AddressInput.Text);
                });
            } else
            {
                ErrorLabel.Text = "No address given.";
            }
        }
    }

}
