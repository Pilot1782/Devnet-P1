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
using CommunityToolkit.Maui.Storage;

namespace DevNet_P11;

public partial class MainPage
{
    private readonly bool _isDebug;

    public MainPage()
    {
        InitializeComponent();

#if DEBUG
        DebugLabel.IsVisible = true;
        AddressInput.Text = "18500 Murdock Circle\n18401 Murdock Circle";
        _isDebug = true;
#endif
    }

    private async Task<Dictionary<string, Dictionary<string, string>?>> RunScraper(string addr)
    {
        if (!addr.Contains('\n'))
            return await RunScraper([addr]);
        var addrs = addr.Split("\n");
        return await RunScraper(addrs);
    }

    private async Task<Dictionary<string, Dictionary<string, string>?>> RunScraper(string[] addrs)
    {
        var scrap = new Scraper();
        var outData = new Dictionary<string, Dictionary<string, string>?>();

        foreach (var addr in addrs)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ProgressLabel.Progress = 0.33;
                DebugLabel.Text += "\nStarting scraping of " + addr;
            });
            Debug.WriteLine("Starting scraping of " + addr);

            var pid = scrap.GetPiD(addr);

            if (pid != "notfound")
            {
                Debug.WriteLine("Got ParcelID, getting data...");
                await MainThread.InvokeOnMainThreadAsync(
                    () => { DebugLabel.Text += "\nGot ParcelID, getting data..."; });
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ErrorLabel.Text = "Address not found.";
                    DebugLabel.Text += "\nFailed to locate property with address: " + addr;
                });
                Debug.WriteLine("Failed to locate property with address: " + addr);
                outData.Add(addr, null);
                continue;
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

            outData.Add(addr, keyData);
        }

        return outData;
    }

    private async Task AddTextToPdf(
        string inputPdfPath,
        string outputPdfPath,
        Dictionary<String, System.Drawing.Point> textToAdd
    )
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
            _ = new StreamReader(stream);
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

            await MainThread.InvokeOnMainThreadAsync(() => { DebugLabel.Text += "\nOpened PDF."; });
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

            await MainThread.InvokeOnMainThreadAsync(() => { DebugLabel.Text += "\nAdded: " + textItem.Key; });
        }

        doc.Close();
    }

    private static int GetClosestInt(List<int> input, int target)
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
    private static string[] SplitStringByLength(string input, int maxLength)
    {
        int numStrings = (int)Math.Ceiling((double)input.Length / maxLength);
        string[] result = new string[numStrings];

        var foundSpaces = new List<int>();
        for (int i = input.IndexOf(' '); i > -1; i = input.IndexOf(' ', i + 1))
        {
            foundSpaces.Add(i);
        }

        try
        {
            for (int i = 0; i < numStrings; i++)
            {
                int startingSpace = GetClosestInt(foundSpaces, i * maxLength);
                if (i == numStrings - 1)
                {
                    result[i] = input.Substring(startingSpace);
                }
                else
                {
                    int endingSpace = GetClosestInt(foundSpaces, (i * maxLength) + 80);
                    result[i] = input.Substring(startingSpace, endingSpace - startingSpace);
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.ToString());
        }

        return result;
    }

    private void OnRunClicked(object sender, EventArgs e)
    {
        // ensure that text is present
        if (AddressInput.Text != "")
        {
            // show the progress bar and reset it
            ProgressLabel.IsVisible = true;
            ProgressLabel.Progress = 0;

            Task.Run(async () =>
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AddressInput.IsReadOnly = true;
                });

                // run the scraper
                var dataDictionary = await RunScraper(AddressInput.Text);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    DebugLabel.Text += "\nAdding data to PDF...";
                });

                // iterate over each address and add the data to the PDF
                foreach (var addr in dataDictionary.Keys)
                {
                    try
                    {
                        string outputPdf;
                        var data = dataDictionary[addr];
                        if (data == null)
                        {
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                DebugLabel.Text += "\nNo data found for address: " + addr;
                            });
                            continue;
                        }

                        // bypass the file picker if compiled in debug mode
                        if (!_isDebug)
                        {
                            var fileSaverResult = await FileSaver.Default.SaveAsync("OutputContract.pdf",
                                await OpenAppPackageFileAsync("input.pdf"));
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
                                    DebugLabel.Text += "\nOutput PDF not selected.";
                                });
                                return;
                            }
                        }
                        else
                        {
                            // check if the folder %userprofile%/PlotOuts exists
                            if (
                                !Directory.Exists(
                                    Environment.GetFolderPath(
                                        Environment.SpecialFolder.UserProfile)
                                    + @"\Downloads\PlotOuts"
                                )
                            )
                            {
                                Directory.CreateDirectory(
                                    Environment.GetFolderPath(
                                        Environment.SpecialFolder.UserProfile)
                                    + @"\Downloads\PlotOuts"
                                );
                            }

                            outputPdf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                        + @"\Downloads\PlotOuts\"
                                        + data["address"].Replace(' ', '_') + ".pdf";
                        }

                        var legalOutput = SplitStringByLength(data["legal"], 90);

                        Dictionary<string, System.Drawing.Point> textToAdd = new()
                        {
                            { data["owner"] + "," + data["city"], new System.Drawing.Point(250, 707) },
                            { data["address"], new System.Drawing.Point(130, 660) },
                            { data["section"], new System.Drawing.Point(97, 591) },
                            { data["township"], new System.Drawing.Point(142, 591) },
                            { data["range"], new System.Drawing.Point(187, 591) },
                            { "CHARLOTTE", new System.Drawing.Point(232, 591) },
                            { data["parcelId"], new System.Drawing.Point(480, 591) }
                        };

                        // add the legal output with line breaking
                        for (var i = 0; i < legalOutput.Length; i++)
                        {
                            textToAdd.Add(
                                legalOutput[i],
                                new System.Drawing.Point(158, 648 - (i * 11))
                            );
                        }

                        Debug.WriteLine("Initialized");
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            DebugLabel.Text += "\nAdding text to PDF: " + addr;
                        });

                        await AddTextToPdf(
                            "input.pdf",
                            outputPdf,
                            textToAdd
                        );
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex + "\nFailed to add Address to PDF: " + addr);
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            DebugLabel.Text += "\nFailed to add address to PDF: " + addr;
                        });
                    }
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    DebugLabel.Text += "\nDone!";
                    AddressInput.IsReadOnly = false;
                });
            });
        }
        else
        {
            ErrorLabel.Text = "No address given.";
        }
    }
}