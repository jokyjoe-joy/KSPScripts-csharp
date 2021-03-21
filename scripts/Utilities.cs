using System;
using KRPC.Client;

class Utilities
{
    public static void Countdown(int x, IProgress<string> progress = null)
    {
        for (int i = x; i > 0; i--)
        {
            if (progress != null) progress.Report(i.ToString());
            System.Threading.Thread.Sleep(1000);
        }
    }

    public static Connection Connect(string name = "")
    {
        Connection conn;
        try 
        {
            conn = new Connection(name: name);
            return conn;
        }
        catch
        {
            return null;
        }
    }
}