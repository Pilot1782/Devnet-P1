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
using Microsoft.UI.Xaml.Controls;

namespace DevNet_P11;

public partial class MainPage
{
    private readonly bool _isDebug;
    private readonly Scraper _scraper;
    private List<Dictionary<string, Microsoft.Maui.IView>> _uiObjects = new();
    private List<string> pidList;

    public MainPage()
    {
        InitializeComponent();

        _scraper = new Scraper();

#if DEBUG
        //DebugLabel.IsVisible = true;
        AddressInput.Text = "18500 Murdock Circle\n18401 Murdock Circle";
        _isDebug = true;
#endif
    }

    private async Task<List<string>> GetPidList(string[] addrList)
    {
        var outData = new List<string>();

        for (int i = 0; i < addrList.Length; i++)
        {
            var addr = addrList[i];

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ((Microsoft.Maui.Controls.ProgressBar) _uiObjects[i]["progress"]).Progress = 0.33;
                ((Microsoft.Maui.Controls.Label) _uiObjects[i]["debug"]).Text = "Starting scraping of " + addr;
            });
            Debug.WriteLine("Starting scraping of " + addr);

            var pid = _scraper.GetPiD(addr);

            if (pid != "notfound")
            {
                Debug.WriteLine("Got ParcelID for " + addr + ": " + pid);
                await MainThread.InvokeOnMainThreadAsync(
                    () => { ((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "Got ParcelID for " + addr + ": " + pid; }
                );

                outData.Add(pid);
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "Failed to locate property with address: " + addr;
                });
                Debug.WriteLine("Failed to locate property with address: " + addr);

                outData.Add("0");
            }
        }

        return outData;
    }

    private async Task PlotParsing(string pid)
    {
        if (pid == "0") { return; }
        int i = pidList.IndexOf(pid);
        var keyData = _scraper.GetKeyData(pid);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ((Microsoft.Maui.Controls.Label)_uiObjects[i]["addr"]).Text = keyData["address"];
        });

        Debug.WriteLine(JsonConvert.SerializeObject(keyData));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ((Microsoft.Maui.Controls.ProgressBar)_uiObjects[i]["progress"]).Progress = 1;
            ((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "Data retrieved.";
        });

        try
        {
            string outputPdf;

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
                        //((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "\nOutput PDF selection failed: " + fileSaverResult.Exception;
                        ((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "Output PDF not selected.";
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
                            + keyData["address"].Replace(' ', '_') + ".pdf";
            }

            var legalOutput = SplitStringByLength(keyData["legal"], 90);
            var city = keyData["city"].Split(" ");

            Dictionary<string, System.Drawing.Point> textToAdd = new()
            {
                { keyData["owner"], new System.Drawing.Point(250, 707) },
                { keyData["address"] + ", " + city[0] + ", FL " + city[1], new System.Drawing.Point(130, 660) },
                { keyData["section"], new System.Drawing.Point(97, 591) },
                { keyData["township"], new System.Drawing.Point(142, 591) },
                { keyData["range"], new System.Drawing.Point(187, 591) },
                { "CHARLOTTE", new System.Drawing.Point(232, 591) },
                { keyData["parcelId"], new System.Drawing.Point(480, 591) }
            };

            // add the legal output with line breaking
            for (var j = 0; j < legalOutput.Length; j++)
            {
                textToAdd.Add(
                    legalOutput[j],
                    new System.Drawing.Point(158, 648 - (j * 11))
                );
            }

            Debug.WriteLine("Initialized");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "Adding text to PDF: " + keyData["address"];
            });

            await AddTextToPdf(
                "input.pdf",
                outputPdf,
                textToAdd,
                i
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex + "\nFailed to add Address to PDF: " + keyData["address"]);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "Failed to add address to PDF: " + keyData["address"];
            });
        }
    }

    private async Task AddTextToPdf(
        string inputPdfPath,
        string outputPdfPath,
        Dictionary<string, System.Drawing.Point> textToAdd,
        int i
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

        await MainThread.InvokeOnMainThreadAsync(() => { ((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "Adding text to PDF..."; });

        try
        {
            var stream = await OpenAppPackageFileAsync("input.pdf");
            _ = new StreamReader(stream);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            Debug.WriteLine("Input PDF not found: " + inputPdfPath);
            await MainThread.InvokeOnMainThreadAsync(() => { ((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "Input PDF not found."; });
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

            await MainThread.InvokeOnMainThreadAsync(() => { ((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "Opened PDF."; });
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            Debug.WriteLine("Output PDF not found: " + outputPdfPath + "\n" + e.Message);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "\nOutput PDF not found. " + e.Message;
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

            await MainThread.InvokeOnMainThreadAsync(() => { ((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "Added: " + textItem.Key; });
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
        if (AddressInput.Text == "") return;

        // show the progress bar and reset it
        //ProgressLabel.IsVisible = true;
        //ProgressLabel.Progress = 0;

        var addrList = AddressInput.Text.Split("\n");

        foreach (var items in _uiObjects)
        {
            foreach (var item in items.Values)
            {
                ThisStackLayout.Children.Remove(item);
            }
        }
        _uiObjects.Clear();


        for (int i = 0; i < addrList.Length; i++)
        {
            Dictionary<string, Microsoft.Maui.IView> dic = new Dictionary<string, Microsoft.Maui.IView>();
            dic["addr"] = new Microsoft.Maui.Controls.Label
            {
                Text = "Fetching Actual Address..."
            };
            ThisStackLayout.Children.Add(dic["addr"]);

            dic["progress"] = new Microsoft.Maui.Controls.ProgressBar
            {
                Progress = 0
            };
            ThisStackLayout.Children.Add(dic["progress"]);

            dic["debug"] = new Microsoft.Maui.Controls.Label
            {
                Text = "Starting..."
            };
            ThisStackLayout.Children.Add(dic["debug"]);

            _uiObjects.Add(dic);
        }

        Task.Run(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(() => { AddressInput.IsReadOnly = true; RunButton.IsEnabled = false; });

            // run the scraper
            pidList = await GetPidList(addrList);

            //await MainThread.InvokeOnMainThreadAsync(() => { DebugLabel.Text += "\nAdding data to PDF..."; });

            var tasks = pidList.Select(PlotParsing).ToList();
            await Task.WhenAll(tasks);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                //DebugLabel.Text += "\nDone!";
                AddressInput.IsReadOnly = false;
                RunButton.IsEnabled = true;
            });
        });
    }
}