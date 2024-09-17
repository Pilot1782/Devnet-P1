using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using Devnet_P11.Scraper;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using static Microsoft.Maui.Storage.FileSystem;
using System.Threading.Tasks;
using System.Threading;
using Windows.Storage.Pickers;
using System.Text;
using CommunityToolkit.Maui.Storage;
using Org.BouncyCastle.Utilities;

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

    private async Task<Dictionary<string, string>?> RunScraper(string addr)
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
            return null;
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

        return keyData;
    }

    private async Task AddTextToPdf(string inputPdfPath, string outputPdfPath, Dictionary<String, System.Drawing.Point> textToAdd)
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
        canvas.SetFontAndSize(font, 8);

        foreach (var textItem in textToAdd)
        {
            canvas.BeginText()
            .MoveText(textItem.Value.X, textItem.Value.Y)
            .ShowText(textItem.Key)
            .EndText();
        }

        doc.Close();
    }

    private int GetClosestInt(List<int> input, int target)
    {
        int res = input[0];
        int diff = Math.Abs(input[0] - target);
        foreach (int num in input)
        {
            int thisDiff = Math.Abs(num - target);
            if (thisDiff < diff)
            {
                res = num;
                diff = thisDiff;
            }
        }
        return res;
    }
    // Multi-line splitting
    private string[] SplitStringByLength(string input, int maxLength)
    {
        int numStrings = (int) Math.Ceiling((double) input.Length / maxLength);
        string[] result = new string[numStrings];

        var foundSpaces = new List<int>();
        for (int i = input.IndexOf(' '); i > -1; i = input.IndexOf(' ', i + 1))
        {
            foundSpaces.Add(i);
        }

        Debug.WriteLine(input.Length);
        try { 
        for (int i = 0; i < numStrings; i++)
        {
            int startingSpace = GetClosestInt(foundSpaces, i * maxLength);
            Debug.WriteLine(startingSpace);
            if (i == numStrings - 1)
            {
                Debug.WriteLine("B1");
                result[i] = input.Substring(startingSpace);
            } else
            {
                int endingSpace = GetClosestInt(foundSpaces, (i * maxLength) + 80);
                Debug.WriteLine(endingSpace);
                Debug.WriteLine(input.Length);
                Debug.WriteLine("B2");
                result[i] = input.Substring(startingSpace, endingSpace - startingSpace);
            }
        }
        }catch (Exception e) { Debug.WriteLine(e.ToString()); }

        return result;
    }

    private void OnRunClicked(object sender, EventArgs e)
    {
        if (AddressInput.Text != "")
        {
            ProgressLabel.IsVisible = true;
            ProgressLabel.Progress = 0;
            Task.Run(async () =>
            {
                Dictionary<string, string>? data = await RunScraper(AddressInput.Text);
                if (data == null) { return; }

                String outputPdf = "";

                var fileSaverResult = await FileSaver.Default.SaveAsync("OutputContract.pdf", await OpenAppPackageFileAsync("input.pdf"));
                if (fileSaverResult.IsSuccessful)
                {
                    outputPdf = fileSaverResult.FilePath;
                    Debug.WriteLine("Success");
                }
                else
                {
                    Debug.WriteLine("Output PDF selection failed: " + fileSaverResult.Exception);
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        DebugLabel.Text += "\nOutput PDF selection failed: " + fileSaverResult.Exception;
                    });
                    return;
                }

                string[] legalOutput = SplitStringByLength(data["legal"], 80);

                Dictionary<String, System.Drawing.Point> textToAdd = new();
                textToAdd.Add(
                    data["address"], 
                    new System.Drawing.Point(120, 660)
                );

                for (int i = 0; i < legalOutput.Length; i++)
                {
                    textToAdd.Add(
                        legalOutput[i],
                        new System.Drawing.Point(158, 648 - (i * 11))
                    );
                }

                await AddTextToPdf(
                    "input.pdf",
                    outputPdf,
                    textToAdd
                );
            });
        }
        else
        {
            ErrorLabel.Text = "No address given.";
        }
    }
}