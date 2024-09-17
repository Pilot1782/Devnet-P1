using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using Devnet_P11.Scraper;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using static Microsoft.Maui.Storage.FileSystem;

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
            await MainThread.InvokeOnMainThreadAsync(() => { DebugLabel.Text += "\nGot ParcelID, getting data..."; });
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

    private async Task AddTextToPdf(string inputPdfPath, string outputPdfPath, string textToAdd,
        System.Drawing.Point point)
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

        await MainThread.InvokeOnMainThreadAsync(() => { DebugLabel.Text += "\nAdding text to PDF..."; });

        try
        {
            var stream = await OpenAppPackageFileAsync("input.pdf");
            var reader2 = new StreamReader(stream);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            Debug.WriteLine("Input PDF not found: " + inputPdfPath);
            await MainThread.InvokeOnMainThreadAsync(() => { DebugLabel.Text += "\nInput PDF not found."; });
            return;
        }

        // copy the data from the input file to the output file
        var inputPdf = await OpenAppPackageFileAsync(inputPdfPath);
        var outputPdf = new FileStream(outputPdfPath, FileMode.Create);
        await inputPdf.CopyToAsync(outputPdf);
        inputPdf.Close();
        outputPdf.Close();

        PdfDocument pdfDoc;
        try
        {
            var reader = new PdfReader(
                await OpenAppPackageFileAsync(inputPdfPath)
            );
            var writer = new PdfWriter(
                outputPdfPath
            );

            pdfDoc = new PdfDocument(reader, writer);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            Debug.WriteLine("Output PDF not found: " + outputPdfPath + "\n" + e.Message);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                DebugLabel.Text += "\nOutput PDF not found. " + e.Message;
            });
            return;
        }

        var doc = new Document(pdfDoc);

        // get the text from the doc
        var canvas = new PdfCanvas(pdfDoc.GetFirstPage());
        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        canvas.SetFontAndSize(font, 12);
        canvas.BeginText()
            .MoveText(point.X, point.Y)
            .ShowText(textToAdd)
            .EndText();

        doc.Close();
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
                    "input.pdf",
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.UserProfile
                    ) + @"\Documents\output.pdf",
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