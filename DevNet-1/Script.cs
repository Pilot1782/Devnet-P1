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

        if (pid != "notfound")
        {
            Console.WriteLine("Got ParcelID, getting data...");
        } else
        {
            Console.Error.WriteLine("Failed to locate property with address: " + addr);
            return;
        }

        var keyData = scrap.GetKeyData(pid);
        Console.WriteLine(keyData.Serialize());
    }
}
