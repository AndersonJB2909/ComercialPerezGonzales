using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ComercialPerezGonzales.Helpers;

public static class ImageHelper
{
    public static byte[] ComprimirSiHaceFalta(byte[] original, int maxBytes = 200_000)
    {
        if (original.Length <= maxBytes) return original;

        for (int calidad = 80; calidad >= 30; calidad -= 10)
        {
            var c = CodificarJpeg(original, calidad);
            if (c.Length <= maxBytes) return c;
        }

        try
        {
            using var ms = new MemoryStream(original);
            var src = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var scaled = new TransformedBitmap(src, new ScaleTransform(0.5, 0.5));
            var encoder = new JpegBitmapEncoder { QualityLevel = 60 };
            encoder.Frames.Add(BitmapFrame.Create(scaled));
            using var ms2 = new MemoryStream();
            encoder.Save(ms2);
            return ms2.ToArray();
        }
        catch
        {
            return CodificarJpeg(original, 30);
        }
    }

    private static byte[] CodificarJpeg(byte[] original, int calidad)
    {
        using var msIn = new MemoryStream(original);
        var bitmap = BitmapFrame.Create(msIn, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var encoder = new JpegBitmapEncoder { QualityLevel = calidad };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var msOut = new MemoryStream();
        encoder.Save(msOut);
        return msOut.ToArray();
    }
}
