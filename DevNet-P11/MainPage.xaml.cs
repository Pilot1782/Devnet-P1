using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using Devnet_P11.Scraper;

namespace DevNet_P11;

public partial class MainPage
{
    public MainPage()
    {
        InitializeComponent();

#if DEBUG
        DebugLabel.IsVisible = true;
        AddressInput.Text = "18500 Murdock Circle";
#endif
    }

    private async void RunScraper(string addr)
    {
        var scrap = new Scraper();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ProgressLabel.Progress = 0.33;
            DebugLabel.Text += "\nStarting scraping...";
        });
        Debug.WriteLine("Starting scraping...");

        var pid = scrap.GetPiD(addr);

        if (pid != "notfound")
        {
            Debug.WriteLine("Got ParcelID, getting data...");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                DebugLabel.Text += "\nGot ParcelID, getting data...";
            });
        }
        else
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ErrorLabel.Text = "Address not found.";
                DebugLabel.Text += "\nFailed to locate property with address: " + addr;
            });
            Debug.WriteLine("Failed to locate property with address: " + addr);
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() => { ProgressLabel.Progress = 0.66; });

        var keyData = scrap.GetKeyData(pid);
        Debug.WriteLine(JsonConvert.SerializeObject(keyData));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ProgressLabel.Progress = 1;
            DebugLabel.Text += "\nData retrieved.";

            OwnerLabel.Text = $"Owner: {keyData["owner"]}";
            AddressLabel.Text = $"Address: {keyData["address"]}, {keyData["city"]}";
            StrLabel.Text =
                $"Section: {keyData["section"]}, Township: {keyData["township"]}, Range: {keyData["range"]}";
            LegalLabel.Text = $"Legal Description: {keyData["legal"]}";
        });
    }

    private async Task AddTextToPdf(string inputPdfPath, string outputPdfPath, string textToAdd, System.Drawing.Point point)
    {
        //create PdfReader object to read from the existing document
        Debug.WriteLine(
            string.Join(
                ", ",
                Assembly.GetExecutingAssembly().GetManifestResourceNames()
            )
        );
        Debug.WriteLine(
            Assembly.GetExecutingAssembly().GetManifestResourceStream(inputPdfPath)
        );

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            DebugLabel.Text += "\nAdding text to PDF...";
        });

        // Check to make sure input pdf exists
        if (Assembly.GetExecutingAssembly().GetManifestResourceStream(inputPdfPath) == null)
        {
            Debug.WriteLine("Input PDF not found.");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                DebugLabel.Text += "\nInput PDF not found.";
            });
            return;
        }

        using var reader = new PdfReader(
            Assembly.GetExecutingAssembly().GetManifestResourceStream(inputPdfPath)
        );
        using var stamper = new PdfStamper(
            reader,
            new FileStream(outputPdfPath, FileMode.Create)
        );

        //select two pages from the original document
        reader.SelectPages("1-2");

        //getting the page size in order to subtract from the iTextSharp coordinates
        var pageSize = reader.GetPageSize(1);

        // PdfContentByte from stamper to add content to the pages over the original content
        var overContent = stamper.GetOverContent(1);

        //add content to the page using ColumnText
        iTextSharp.text.Font font = new(iTextSharp.text.Font.FontFamily.COURIER, 45);

        //setting up the X and Y coordinates of the document
        var x = point.X;
        var y = point.Y;

        y = (int)(pageSize.Height - y);

        ColumnText.ShowTextAligned(overContent, iTextSharp.text.Element.ALIGN_CENTER, new Phrase(textToAdd, font), x, y,
            0);
    }

    private void OnRunClicked(object sender, EventArgs e)
    {
        if (AddressInput.Text != "")
        {
            ProgressLabel.IsVisible = true;
            ProgressLabel.Progress = 0;
            Task.Run(() => { RunScraper(AddressInput.Text); });
            Task.Run(() =>
            {
                _ = AddTextToPdf(
                    "DevNet_P11.Properties.Resources.inputpdf",
                    "output.pdf",
                    "AAAAAAAAAAAAAAAAAAAAAAA",
                    new System.Drawing.Point(200, 200));
            });
        }
        else
        {
            ErrorLabel.Text = "No address given.";
        }
    }
}