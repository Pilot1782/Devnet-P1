using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;

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
        private static void AddTextToPdf(string inputPdfPath, string outputPdfPath, string textToAdd, System.Drawing.Point point)
        {
            //variables
            string pathin = inputPdfPath;
            string pathout = outputPdfPath;

            //create PdfReader object to read from the existing document
            Debug.WriteLine(string.Join(", ", Assembly.GetExecutingAssembly().GetManifestResourceNames()));
            Debug.WriteLine(Assembly.GetExecutingAssembly().GetManifestResourceStream(pathin));
            using (PdfReader reader = new PdfReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(pathin)))
            //create PdfStamper object to write to get the pages from reader 
            using (PdfStamper stamper = new PdfStamper(reader, new FileStream(pathout, FileMode.Create)))
            {
                //select two pages from the original document
                reader.SelectPages("1-2");

                //gettins the page size in order to substract from the iTextSharp coordinates
                var pageSize = reader.GetPageSize(1);

                // PdfContentByte from stamper to add content to the pages over the original content
                PdfContentByte pbover = stamper.GetOverContent(1);

                //add content to the page using ColumnText
                iTextSharp.text.Font font = new (iTextSharp.text.Font.FontFamily.COURIER, 45);

                //setting up the X and Y coordinates of the document
                int x = point.X;
                int y = point.Y;

                y = (int)(pageSize.Height - y);

                ColumnText.ShowTextAligned(pbover, iTextSharp.text.Element.ALIGN_CENTER, new Phrase(textToAdd, font), x, y, 0);
            }
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
                Task.Run(() =>
                {
                    AddTextToPdf("Devnet_1.Properties.Resources.inputpdf", "output.pdf", "AAAAAAAAAAAAAAAAAAAAAAA", new System.Drawing.Point(200,200));
                });
            } else
            {
                ErrorLabel.Text = "No address given.";
            }
        }
    }

}
