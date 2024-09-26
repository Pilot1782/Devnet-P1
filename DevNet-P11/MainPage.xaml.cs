using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using System.Text;
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
    public static readonly Scraper Scraper = new();
    private readonly List<Dictionary<string, IView>> _uiObjects = [];
    private List<string>? _pidList;
    private string _outputFolder;
    private Label? _buffer;

    public MainPage()
    {
        _outputFolder = "";
        _isDebug = false;
        InitializeComponent();

#if DEBUG
        //DebugLabel.IsVisible = true;
        AddressInput.Text = "18500 Murdock Circle\r18401 Murdock Circle\r" +
                            "1120 EL JOBEAN RD\r17701 MURDOCK CIR\r" +
                            "2989 ROCK CREEK DR\r29890 BERMONT RD\r" +
                            "299 ANTIS DR";
        _isDebug = true;
#endif
        _ = Scraper.GetPidLocal("Test");
    }

    private async Task<List<string>?> GetPidList(string[] addrList)
    {
        var outData = new List<string>();

        for (var i = 0; i < addrList.Length; i++)
        {
            var addr = addrList[i];

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ((ProgressBar)_uiObjects[i]["progress"]).Progress = 0.1;
                ((Label)_uiObjects[i]["debug"]).Text = "Starting scraping of " + addr + " (LOCAL)";
            });
            Debug.WriteLine("Starting scraping of " + addr);

            var pid = Scraper.GetPidLocal(addr);
            if (pid == "")
            {
                Debug.WriteLine("Trying online for " + addr);
                await MainThread.InvokeOnMainThreadAsync(() => {
                    ((ProgressBar)_uiObjects[i]["progress"]).Progress = 0.2;
                    ((Label)_uiObjects[i]["debug"]).Text = "Starting scraping of " + addr + " (ONLINE)";
                });
                pid = Scraper.GetPid(addr, (Label)_uiObjects[i]["debug"], (ProgressBar)_uiObjects[i]["progress"]);
            }

            if (pid != "notfound")
            {
                Debug.WriteLine("Got ParcelID for " + addr + ": " + pid);
                await MainThread.InvokeOnMainThreadAsync(() => {
                    ((ProgressBar)_uiObjects[i]["progress"]).Progress = 0.33;
                    ((Label)_uiObjects[i]["debug"]).Text = "Got ParcelID for " + addr + ": " + pid;
                });

                outData.Add(pid);
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ((Label)_uiObjects[i]["addr"]).Text = $"<strong>{addr} (FAILED)</strong>";
                    ((Label)_uiObjects[i]["debug"]).Text = "Failed to locate property with address: " + addr;
                });
                Debug.WriteLine("Failed to locate property with address: " + addr);

                outData.Add("0");
            }
        }

        return outData;
    }

    private async Task PlotParsing(string pid)
    {
        if (pid == "0")
        {
            return;
        }

        if (_pidList != null)
        {
            var i = _pidList.IndexOf(pid);
            var keyData = Scraper.GetKeyDataAsync(pid);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ((Label)_uiObjects[i]["addr"]).Text = $"<strong>{keyData["address"]}</strong>";
            });

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ((ProgressBar)_uiObjects[i]["progress"]).Progress = 0.5;
                ((Label)_uiObjects[i]["debug"]).Text = "Data retrieved.";
            });

            try
            {
                string outputPdf = _outputFolder + @"\" + keyData["address"].Replace(' ', '_') + ".pdf";

                var legalOutput1Line = SplitStringByLengthSingle(keyData["legal"], 110);
                var legalOutput = (legalOutput1Line.Length >= keyData["legal"].Length) ? Array.Empty<string>() : SplitStringByLength(keyData["legal"].Substring(legalOutput1Line.Length), 140);
                var lastCitySpace = keyData["city"].LastIndexOf(' ');
                var city = keyData["city"][..lastCitySpace];
                var zip = keyData["city"][(lastCitySpace + 1)..];

                Dictionary<string, System.Drawing.Point> textToAdd = new()
                {
                    { keyData["owner"], new System.Drawing.Point(250, 707) },
                    { keyData["address"] + ", " + city + ", FL " + zip, new System.Drawing.Point(130, 660) },
                    { keyData["section"], new System.Drawing.Point(97, 591) },
                    { keyData["township"], new System.Drawing.Point(142, 591) },
                    { keyData["range"], new System.Drawing.Point(187, 591) },
                    { "CHARLOTTE", new System.Drawing.Point(232, 591) },
                    { keyData["parcelId"], new System.Drawing.Point(480, 591) },
                    { legalOutput1Line, new System.Drawing.Point(158, 648) }
                };

                // add the legal output with line breaking
                for (var j = 0; j < legalOutput.Length; j++)
                {
                    textToAdd.Add(
                        legalOutput[j],
                        new System.Drawing.Point(70, 648 - (j * 11) - 11)
                    );
                }

                Debug.WriteLine("Initialized");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ((ProgressBar)_uiObjects[i]["progress"]).Progress = 0.66;
                    ((Label)_uiObjects[i]["debug"]).Text = "Starting PDF: " + keyData["address"];
                });

                await AddTextToPdf(
                    "input.pdf",
                    outputPdf,
                    textToAdd,
                    i
                );


                await MainThread.InvokeOnMainThreadAsync(() => {
                    ((ProgressBar)_uiObjects[i]["progress"]).Progress = 1;
                    ((Label)_uiObjects[i]["debug"]).Text = "Done!"; 
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex + "\nFailed to add Address to PDF: " + keyData["address"]);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ((Label)_uiObjects[i]["debug"]).Text = "Failed to add address to PDF: " + keyData["address"];
                });
            }
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

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ((Label)_uiObjects[i]["debug"]).Text = "Adding text to PDF...";
        });

        try
        {
            var stream = await OpenAppPackageFileAsync("input.pdf");
            _ = new StreamReader(stream);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            Debug.WriteLine("Input PDF not found: " + inputPdfPath);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ((Label)_uiObjects[i]["debug"]).Text = "Input PDF not found.";
            });
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

            await MainThread.InvokeOnMainThreadAsync(() => {
                ((ProgressBar)_uiObjects[i]["progress"]).Progress = 0.80;
                ((Label)_uiObjects[i]["debug"]).Text = "Opened PDF."; 
            });
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            Debug.WriteLine("Output PDF not found: " + outputPdfPath + "\n" + e.Message);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ((Label)_uiObjects[i]["debug"]).Text = "\nOutput PDF not found. " + e.Message;
            });
            return;
        }

        var doc = new Document(pdfDoc);

        // get the text from the doc
        var canvas = new PdfCanvas(pdfDoc.GetFirstPage());
        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        canvas.SetFontAndSize(font, 7);

        foreach (var textItem in textToAdd)
        {
            canvas.BeginText()
                .MoveText(textItem.Value.X, textItem.Value.Y)
                .ShowText(textItem.Key)
                .EndText();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ((ProgressBar)_uiObjects[i]["progress"]).Progress += 0.015;
                ((Label)_uiObjects[i]["debug"]).Text = "Added: " + textItem.Key;
            });
        }

        doc.Close();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ((Button)_uiObjects[i]["open"]).Clicked += (_, _) => { Launcher.Default.OpenAsync(outputPdfPath); };

            ((Button)_uiObjects[i]["open"]).IsVisible = true;
        });
    }

    // Multi-line splitting
    private static string SplitStringByLengthSingle(string input, int maxLength)
    {
        var words = input.Split(' ');
        var currentLine = new StringBuilder();
        foreach (var word in words)
        {
            if (currentLine.Length + word.Length >= maxLength)
            {
                return currentLine.ToString();
            }

            currentLine.Append(word + " ");
        }

        return currentLine.ToString();
    }
    private static string[] SplitStringByLength(string input, int maxLength)
    {
        var words = input.Split(' ');
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        foreach (var word in words)
        {
            if (currentLine.Length + word.Length >= maxLength)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }

            currentLine.Append(word + " ");
        }

        lines.Add(currentLine.ToString());
        return lines.ToArray();
    }

    private void OnRunClicked(object sender, EventArgs e)
    {
        var sw = Stopwatch.StartNew();
        sw.Start();

        // ensure that text is present
        if (AddressInput.Text == "") return;

        // show the progress bar and reset it
        //ProgressLabel.IsVisible = true;
        //ProgressLabel.Progress = 0;

        
        AddressInput.IsReadOnly = true;
        RunButton.IsEnabled = false;
        RunButton.Text = "Please Wait...";

        var addrList = AddressInput.Text.Split("\r").ToList();

        for (int i = 0; i < addrList.Count; i++)
        {
            if (addrList[i] == "")
            {
                addrList.Remove(addrList[i]);
                i--;
            }
        }

        foreach (var item in _uiObjects.SelectMany(items => items.Values))
        {
            ThisStackLayout.Children.Remove(item);
        }

        _uiObjects.Clear();

        if (_buffer != null)
        {
            ThisStackLayout.Children.Remove(_buffer);
        }


        foreach (var addr in addrList)
        {
            Dictionary<string, IView> dic = new Dictionary<string, IView>
            {
                ["addr"] = new Label
                {
                    Text = $"<strong>{addr} (Not Verified) </strong>",
                    TextType = TextType.Html
                }
            };
            ThisStackLayout.Children.Add(dic["addr"]);

            dic["progress"] = new ProgressBar
            {
                Progress = 0
            };
            ThisStackLayout.Children.Add(dic["progress"]);

            dic["debug"] = new Label
            {
                Text = "Starting..."
            };
            ThisStackLayout.Children.Add(dic["debug"]);

            dic["open"] = new Button
            {
                Text = "Open PDF",
                IsVisible = false,
                WidthRequest = 300
            };
            ThisStackLayout.Children.Add(dic["open"]);

            _uiObjects.Add(dic);
        }

        _buffer = new Label
        {
            Text = "\r\r"
        };
        ThisStackLayout.Children.Add(_buffer);

        Task.Run(async () =>
        {

            // run the scraper
            _pidList = await GetPidList(addrList.ToArray());

            if (_pidList != null)
            {
                bool success = false;
                foreach (var pid in _pidList)
                {
                    if (pid != "notfound")
                    {
                        success = true;
                        break;
                    }
                }

                if (!success) { return; }
                // bypass the folder picker if compiled in debug mode
                if (!_isDebug)
                {
                    var folderPickerResult = await FolderPicker.Default.PickAsync();
                    if (folderPickerResult.IsSuccessful)
                    {
                        _outputFolder = folderPickerResult.Folder.Path;
                        Debug.WriteLine("Success");
                    }
                    else
                    {
                        Debug.WriteLine("Output Folder selection failed: " + folderPickerResult.Exception.Message);
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            //((Microsoft.Maui.Controls.Label)_uiObjects[i]["debug"]).Text = "\nOutput PDF selection failed: " + fileSaverResult.Exception;
                            for (int i = 0; i < _pidList.Count; i++)
                            {
                                if (_pidList[i] == "0") { continue; }
                                ((Label)_uiObjects[i]["addr"]).Text = $"<strong>{addrList[i]} (Cancelled)</strong>";
                                ((Label)_uiObjects[i]["debug"]).Text = "Output Folder not selected.";
                            }
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

                    _outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                + @"\Downloads\PlotOuts";
                }

                var tasks = _pidList.Select(PlotParsing).ToList();
                await Task.WhenAll(tasks);
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                //DebugLabel.Text += "\nDone!";
                AddressInput.IsReadOnly = false;
                RunButton.IsEnabled = true;
                RunButton.Text = "Click to Run";

                sw.Stop();
                Debug.WriteLine("Time elapsed: " + sw.Elapsed);
            });
        });
    }
}