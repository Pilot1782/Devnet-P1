using System.Collections;
using System.Collections.Generic;

namespace DevNet_1;

public class Script
{
    public static void Main()
    {
        var scrap = new Scraper.Scraper();

        Console.WriteLine("Starting scraping...");

        var addr = "18500 Murdock Circle";
        var pid = scrap.GetPiD(addr);

        Console.WriteLine("Got ParcelID, getting data...");

        var keyData = scrap.GetKeyData(pid);
        Console.WriteLine(string.Join(Environment.NewLine, keyData.Select(a => $"{a.Key}: {a.Value}")));
    }
}
