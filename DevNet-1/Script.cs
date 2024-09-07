namespace DevNet_1;

public class Script
{
    public static void Main()
    {
        var scrap = new Scraper.Scraper();

        var addr = "18500 Murdock Circle";
        var pid = scrap.GetPiD(addr);

        Console.WriteLine($"Parcel ID for {addr}: {pid}");
    }
}
