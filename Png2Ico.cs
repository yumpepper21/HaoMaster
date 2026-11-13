using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class Png2Ico
{
    static void Main(string[] args)
    {
        string pngPath = args[0];
        string icoPath = args[1];
        int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };

        using (Bitmap src = new Bitmap(pngPath))
        using (MemoryStream outMs = new MemoryStream())
        {
            byte[][] entries = new byte[sizes.Length][];
            byte[][] dirEntries = new byte[sizes.Length][];
            int offset = 6 + 16 * sizes.Length;

            for (int i = 0; i < sizes.Length; i++)
            {
                int s = sizes[i];
                byte[] data;
                using (Bitmap bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.Clear(Color.Transparent);
                        g.DrawImage(src, 0, 0, s, s);
                    }

                    if (s >= 256)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, ImageFormat.Png);
                            data = ms.ToArray();
                        }
                    }
                    else
                    {
                        byte[] xor = new byte[s * s * 4];
                        for (int y = 0; y < s; y++)
                        {
                            int dst = (s - 1 - y) * s * 4;
                            for (int x = 0; x < s; x++)
                            {
                                Color c = bmp.GetPixel(x, y);
                                xor[dst + x * 4 + 0] = c.B;
                                xor[dst + x * 4 + 1] = c.G;
                                xor[dst + x * 4 + 2] = c.R;
                                xor[dst + x * 4 + 3] = c.A;
                            }
                        }
                        byte[] and = new byte[s * s / 8]; // 全 0，透明由 alpha 表示
                        data = new byte[40 + xor.Length + and.Length];
                        WriteU32(data, 0, 40);            // biSize
                        WriteU32(data, 4, (uint)s);       // biWidth
                        WriteU32(data, 8, (uint)(2 * s)); // biHeight (XOR+AND)
                        WriteU16(data, 12, 1);            // biPlanes
                        WriteU16(data, 14, 32);           // biBitCount
                        WriteU32(data, 20, (uint)(xor.Length + and.Length));
                        Buffer.BlockCopy(xor, 0, data, 40, xor.Length);
                        Buffer.BlockCopy(and, 0, data, 40 + xor.Length, and.Length);
                    }
                }

                byte[] e = new byte[16];
                e[0] = (byte)(s >= 256 ? 0 : s);
                e[1] = (byte)(s >= 256 ? 0 : s);
                WriteU16(e, 4, 1);
                WriteU16(e, 6, 32);
                WriteU32(e, 8, (uint)data.Length);
                WriteU32(e, 12, (uint)offset);
                dirEntries[i] = e;
                entries[i] = data;
                offset += data.Length;
            }

            WriteU16(outMs, 0, 0);
            WriteU16(outMs, 2, 1);
            WriteU16(outMs, 4, (ushort)sizes.Length);
            foreach (byte[] e in dirEntries) outMs.Write(e, 0, e.Length);
            foreach (byte[] d in entries) outMs.Write(d, 0, d.Length);
            File.WriteAllBytes(icoPath, outMs.ToArray());
        }
        Console.WriteLine("OK: " + icoPath);
    }

    static void WriteU16(Stream s, int pos, ushort v)
    {
        s.Position = pos;
        s.WriteByte((byte)(v & 0xFF));
        s.WriteByte((byte)(v >> 8));
    }

    static void WriteU16(byte[] b, int pos, ushort v)
    {
        b[pos] = (byte)(v & 0xFF);
        b[pos + 1] = (byte)(v >> 8);
    }

    static void WriteU32(byte[] b, int pos, uint v)
    {
        b[pos] = (byte)(v & 0xFF);
        b[pos + 1] = (byte)((v >> 8) & 0xFF);
        b[pos + 2] = (byte)((v >> 16) & 0xFF);
        b[pos + 3] = (byte)((v >> 24) & 0xFF);
    }
}
