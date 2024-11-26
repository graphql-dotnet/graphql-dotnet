// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
foreach (string dir in Directory.GetDirectories(Path.Join(@"..",@"..",@"..",@"..",@"..", "src")))
{
    var p = Process.Start("dotnet.exe", "sln ..\\..\\..\\..\\Samples.sln add "+dir+'\\');
    p.WaitForExit();
}

