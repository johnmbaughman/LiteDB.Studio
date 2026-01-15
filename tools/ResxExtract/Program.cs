using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

if (args.Length < 2)
{
    Console.WriteLine("Usage: ResxExtract <resx-path> <out-dir>");
    return 1;
}

var resxPath = args[0];
var outDir = args[1];
Directory.CreateDirectory(outDir);

var text = File.ReadAllText(resxPath);
var m = Regex.Match(text, @"<data[^>]*name=""\$this.Icon""[^>]*>[\s\S]*?<value>([\s\S]*?)</value>", RegexOptions.IgnoreCase);
if (!m.Success)
{
    Console.WriteLine("$this.Icon block not found");
    return 2;
}

var b64 = Regex.Replace(m.Groups[1].Value, "\\s+", "");
byte[] data;
try
{
    data = Convert.FromBase64String(b64);
}
catch (Exception ex)
{
    Console.WriteLine("base64 decode failed: " + ex.Message);
    return 3;
}

var rawPath = Path.Combine(outDir, "litedb_icon.raw");
File.WriteAllBytes(rawPath, data);
Console.WriteLine($"Wrote raw bytes to {rawPath}");

byte[] pngSig = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A };
byte[] icoSig = new byte[] { 0x00, 0x00, 0x01, 0x00 };

if (StartsWith(data, icoSig))
{
    var icoPath = Path.Combine(outDir, "litedb_icon.ico");
    File.WriteAllBytes(icoPath, data);
    Console.WriteLine($"Detected ICO header, wrote {icoPath}");
    try
    {
        // try convert to PNG (Windows only)
        using (var ms = new MemoryStream(data))
        using (var icon = new System.Drawing.Icon(ms))
        using (var bmp = icon.ToBitmap())
        {
            var pngPath = Path.Combine(outDir, "litedb_icon.png");
            bmp.Save(pngPath, System.Drawing.Imaging.ImageFormat.Png);
            Console.WriteLine($"Also wrote PNG conversion {pngPath}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("PNG conversion failed: " + ex.Message);
    }
}
else if (StartsWith(data, pngSig))
{
    var pngPath = Path.Combine(outDir, "litedb_icon.png");
    File.WriteAllBytes(pngPath, data);
    Console.WriteLine($"Detected PNG header, wrote {pngPath}");
}
else
{
    int idx = IndexOf(data, pngSig);
    if (idx >= 0)
    {
        var iend = System.Text.Encoding.ASCII.GetBytes("IEND");
        int j = IndexOf(data, iend, idx);
        int end = (j >= 0) ? j + 8 : data.Length;
        var pngPath = Path.Combine(outDir, "litedb_icon_from_inner.png");
        File.WriteAllBytes(pngPath, data.Skip(idx).Take(end - idx).ToArray());
        Console.WriteLine($"Found embedded PNG at {idx}, wrote {pngPath}");
    }
    else
    {
        int idx2 = IndexOf(data, icoSig, 0);
        if (idx2 >= 0)
        {
            var icoPath = Path.Combine(outDir, "litedb_icon_from_inner.ico");
            File.WriteAllBytes(icoPath, data.Skip(idx2).ToArray());
            Console.WriteLine($"Found embedded ICO at {idx2}, wrote {icoPath}");
        }
        else
        {
            Console.WriteLine("No known image signature found; wrote raw file for manual inspection");
        }
    }
}

Console.WriteLine("Done");
return 0;

static bool StartsWith(byte[] data, byte[] prefix)
{
    if (data.Length < prefix.Length) return false;
    for (int i = 0; i < prefix.Length; i++) if (data[i] != prefix[i]) return false;
    return true;
}

static int IndexOf(byte[] haystack, byte[] needle, int start = 0)
{
    if (needle.Length == 0) return 0;
    for (int i = start; i <= haystack.Length - needle.Length; i++)
    {
        bool ok = true;
        for (int j = 0; j < needle.Length; j++) if (haystack[i + j] != needle[j]) { ok = false; break; }
        if (ok) return i;
    }
    return -1;
}
