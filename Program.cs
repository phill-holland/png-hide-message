using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;
using SixLabors.ImageSharp.Processing.Processors.Binarization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using SixLabors.ImageSharp.Processing.Processors.Dithering;

namespace Steganography
{
    class Program
    {
        static void Encode(string filename, string message)
        {
            Image<Rgba32> source = Image.Load<Rgba32>(filename);

            if(source.Width * source.Height < message.Length) return;

            byte[] data = Encoding.ASCII.GetBytes(message);
            int index = 0;

            using(Image<Rgba32> dest = new Image<Rgba32>(source.Width, source.Height))
            {
                for(int y = 0; y < source.Height; ++y)
                {
                    for(int x = 0; x < source.Width; ++x)
                    {
                        Rgba32 pixel = source[x,y];
                        if(index == 0) 
                        {
                            // encode size of message in first pixel                            
                            UInt32 temp = (UInt32)data.Length;
                            
                            pixel.R = (byte)(temp & 0xFF);
                            pixel.G = (byte)((temp >> 8) & 0xFF);
                            pixel.B = (byte)((temp >> 16) & 0xFF);
                            pixel.A = (byte)((temp >> 24) & 0xFF);
                        }
                        else if(index <= message.Length)
                        {
                            // encode single ASCII character in LSB colours of pixel
                            byte value = data[index - 1];

                            // clear lowest two bits per pixel
                            pixel.R = (byte)(pixel.R & 0xFC);
                            pixel.G = (byte)(pixel.G & 0xFC);
                            pixel.B = (byte)(pixel.B & 0xFC);
                            pixel.A = (byte)(pixel.A & 0xFC);

                            // encode character into lowest two bit of each pixel
                            pixel.R = (byte)(pixel.R | (value & (0x3)));
                            pixel.G = (byte)(pixel.G | (value >> 2 & (0x3)));
                            pixel.B = (byte)(pixel.B | (value >> 4 & (0x3)));
                            pixel.A = (byte)(pixel.A | (value >> 6 & (0x3)));
                        }

                        dest[x,y] = pixel;

                        ++index;
                    }
                }

                dest.SaveAsPng("output.png");
            }
        }

        static string Decode(string filename)
        {
            Image<Rgba32> source = Image.Load<Rgba32>(filename);

            Rgba32 first = source[0,0];

            int size = 0;

            // decode size from first pixel
            size |= (first.R & 0xFF);
            size |= (first.G << 8);
            size |= (first.B << 16);
            size |= (first.A << 24);

            if(size >= source.Width * source.Height) return "";

            byte []data = new byte[size];
            int index = 0;

            int y = 0;
            int x = 1;
            for(int i = 1; i <= size; i++)
            {
                Rgba32 pixel = source[x,y];

                byte value = 0;

                // decode ASCII characters from pixels and recombine
                value |= (byte)(pixel.R & 0x3);
                value |= (byte)(((pixel.G & 0x3) << 2) & 0xC);
                value |= (byte)(((pixel.B & 0x3) << 4) & 0x30);
                value |= (byte)(((pixel.A & 0x3) << 6) & 0xC0);

                data[index++] = value;

                ++x;
                if(x >= source.Width)
                {
                    x = 0;
                    ++y;
                }
            }

            return Encoding.ASCII.GetString(data);
        }


        static string out_bin(byte source)
        {
            string result = "";
            
            result += (source & 0x10) > 0 ? "1" : "0";
            result += (source & 0x08) > 0 ? "1" : "0";
            result += (source & 0x04) > 0 ? "1" : "0";
            result += (source & 0x02) > 0 ? "1" : "0";
            result += (source & 0x01) > 0 ? "1" : "0";
        
            return result;
        }

        static string Decode2(string filename)
        {
            Image<Rgba32> source = Image.Load<Rgba32>(filename);

            Rgba32 first = source[0,0];

            int size = source.Width * source.Height;

            // decode size from first pixel
            //size |= (first.R & 0xFF);
            //size |= (first.G << 8);
            //size |= (first.B << 16);
            //size |= (first.A << 24);

            //if(size >= source.Width * source.Height) return "";

            byte []data = new byte[size];
            int index = 0;

            int y = 0;
            int x = 1;
            for(int i = 1; i <= size; i++)
            {
                Rgba32 pixel = source[x,y];

                byte value = 0;


                //Console.WriteLine("(" + Convert.ToString(pixel.R) + "," + Convert.ToString(pixel.G) + "," + Convert.ToString(pixel.B) + ")");
                // decode ASCII characters from pixels and recombine
                byte r = (byte)(pixel.R & 0x1F);
                byte g = (byte)(pixel.G & 0x1F);
                byte b = (byte)(pixel.B & 0x1F);

                Console.WriteLine(out_bin(r) + "," + out_bin(g) + "," + out_bin(b));

                value |= (byte)(pixel.R & 0x3);
                value |= (byte)(((pixel.G & 0x3) << 2) & 0xC);
                value |= (byte)(((pixel.B & 0x3) << 4) & 0x30);
                value |= (byte)(((pixel.A & 0x3) << 6) & 0xC0);

                data[index++] = value;

                ++x;
                if(x >= source.Width)
                {
                    x = 0;
                    ++y;
                    if(y >= source.Height) y = 0;                    
                }
            }

            return Encoding.ASCII.GetString(data);
        }

        static void Something(String filename)
        {
            using(FileStream _in = File.OpenRead(filename))
            {
                _in.Seek(813, SeekOrigin.Begin);

                byte[] length = new byte[4];
                _in.Read(length, 0, 4);

                int l = (((int)length[0]) << 24) | (((int)length[1]) << 16) |(((int)length[2]) << 8) | (int)length[3];

                byte[] chunk_type = new byte[4];
                _in.Read(chunk_type, 0, 4);

                Console.WriteLine(System.Text.Encoding.ASCII.GetString(chunk_type));                
                Console.WriteLine(l);

                byte[] data = new byte[l];
                _in.Read(data, 0, l);

                using(MemoryStream source = new MemoryStream(data))
                {
                    using(FileStream output = File.Create("data.dmp"))
                    {
                        using (var deflate = new ZLibStream(source, CompressionMode.Decompress))
                        {
                            deflate.CopyTo(output);
                        };
                    };
                };
                //int b = _in.ReadByte();
            }
        }

        static void ToImage(string filename)
        {
            int width = 984;
            int height = 974;

            using(FileStream _in = File.OpenRead(filename))
            {
                byte[] line = new byte[width + 1];
                int y = 0;

                using(Image<Rgba32> dest = new Image<Rgba32>(width, height))
                {
                    while(y < height)
                    {
                        _in.Read(line, 0, width + 1);
                        for(int x = 0; x < width; ++x)
                        {
                            Rgba32 pixel = new Rgba32(line[x],line[x],line[x]);
                            //pixel.R = line[x];
                            //pixel.G = line[x];
                            //pixel.B = line[x];

                            dest[x,y] = pixel;
                        }

                        ++y;
                    }

                    dest.SaveAsPng("output_dmp.png");
                }


                byte[] dumb = new byte[1];
                int t = 0, counter = 0;
                do
                {
                    t = _in.ReadByte();
                    if(t != -1)
                    {
                        dumb[0] = (byte)t;
                        Console.WriteLine(System.BitConverter.ToString(dumb) + " ");
                        ++counter;
                    }
                }while(t != -1);

                Console.WriteLine("COUnter: " + counter);
            }
        }

/*
        static void Overlap(string a, string b, string output)
        {
            Image<Rgba32> source_a = Image.Load<Rgba32>(a);
            Image<Rgba32> source_b = Image.Load<Rgba32>(b);

            if(source_a.Width != source_b.Width) return;
            if(source_a.Height != source_b.Height) return;

            using(Image<Rgba32> dest = new Image<Rgba32>(source_a.Width, source_b.Height))
            {
                for(int y = 0; y < source_a.Height; ++y)
                {
                    for(int x = 0; x < source_a.Width; ++x)
                    {
                        Rgba32 pixel_a = source_a[x,y];
                        Rgba32 pixel_b = source_b[x,y];

                        Rgba32
                        dest[x,y].R = (byte)(pixel_a.R ^ pixel_b.R);
                    }
                }

                dest.SaveAsPng("xor.png");
        }
*/

        static void DumperJustRaw(String filename)
        {
            const int width = 984, height = 974;
            byte[] raw = new byte[width * height];
            byte[] palette = new byte[768];

            using(FileStream _in = File.OpenRead(filename))
            {
                // ***
                // load palette
                // ***

                _in.Seek(33, SeekOrigin.Begin);
                byte[] pal_length = new byte[4];
                _in.Read(pal_length, 0, 4);

                int pl = (((int)pal_length[0]) << 24) | (((int)pal_length[1]) << 16) |(((int)pal_length[2]) << 8) | (int)pal_length[3];

                byte[] pal_chunk_type = new byte[4];
                _in.Read(pal_chunk_type, 0, 4);

                Console.WriteLine("pal length:" + Convert.ToString(pl) + " " + Encoding.ASCII.GetString(pal_chunk_type));
                _in.Read(palette, 0, pl);

                // ***
                // load data
                _in.Seek(813, SeekOrigin.Begin);
                // ***

                byte[] raw_length = new byte[4];
                _in.Read(raw_length, 0, 4);

                int l = (((int)raw_length[0]) << 24) | (((int)raw_length[1]) << 16) |(((int)raw_length[2]) << 8) | (int)raw_length[3];

                byte[] chunk_type = new byte[4];
                _in.Read(chunk_type, 0, 4);

                Console.WriteLine(System.Text.Encoding.ASCII.GetString(chunk_type));                
                Console.WriteLine(l);

                byte[] data = new byte[l];
                _in.Read(data, 0, l);

                using(MemoryStream source = new MemoryStream(data))
                {
                    using(MemoryStream output = new MemoryStream())
                    {
                        using (var deflate = new ZLibStream(source, CompressionMode.Decompress))
                        {
                            deflate.CopyTo(output);
                            raw = output.ToArray();
                        };
                    };
                };
            }

            // ****
    Dictionary<int,int> map = new Dictionary<int, int>();


            using(Image<Rgba32> dest = new Image<Rgba32>(width, height))
            {
                for(int y = 0; y < height; ++y)
                {
                    int a_offset = y * (width + 1);
                    int b_offset = (y) * (width + 1);
                
                    for(int x = 0; x < width; ++x)
                    {
                        byte a = raw[a_offset + x];
                        byte b = raw[b_offset + x];

                        if(!map.ContainsKey(a)) map.Add(a,a);

                        int a_idx = (((int)a) * 3);
                        byte a_red = palette[a_idx];
                        byte a_green = palette[a_idx + 1];
                        byte a_blue = palette[a_idx + 2];

                        int b_idx = (((int)b) * 3);
                        byte b_red = palette[b_idx];
                        byte b_green = palette[b_idx + 1];
                        byte b_blue = palette[b_idx + 2];

                        byte tred = (byte)((((int)a_red) + ((int)b_red)) / 2);
                        byte tgreen = (byte)((((int)a_green) + ((int)b_green)) / 2);
                        byte tblue = (byte)((((int)a_blue) + ((int)b_blue)) / 2);

                        byte red = (byte)(a_red ^ b_red);
                        byte green = (byte)(a_green ^ b_green);
                        byte blue = (byte)(a_blue ^ b_blue);

                        Rgba32 pixel = new Rgba32(a,a,a);//tred,tgreen,tblue);//src,src,src);
                        dest[x,y] = pixel;
                    }
                }

                dest.SaveAsPng("output_dmp_just_raw.png");
            }

        }

        static void Dumper(String filename)
        {
            const int width = 984, height = 974;
            byte[] raw = new byte[width * height];
            byte[] palette = new byte[768];

            using(FileStream _in = File.OpenRead(filename))
            {
                // ***
                // load palette
                // ***

                _in.Seek(33, SeekOrigin.Begin);
                byte[] pal_length = new byte[4];
                _in.Read(pal_length, 0, 4);

                int pl = (((int)pal_length[0]) << 24) | (((int)pal_length[1]) << 16) |(((int)pal_length[2]) << 8) | (int)pal_length[3];

                byte[] pal_chunk_type = new byte[4];
                _in.Read(pal_chunk_type, 0, 4);

                Console.WriteLine("pal length:" + Convert.ToString(pl) + " " + Encoding.ASCII.GetString(pal_chunk_type));
                _in.Read(palette, 0, pl);

                // ***
                // load data
                _in.Seek(813, SeekOrigin.Begin);
                // ***

                byte[] raw_length = new byte[4];
                _in.Read(raw_length, 0, 4);

                int l = (((int)raw_length[0]) << 24) | (((int)raw_length[1]) << 16) |(((int)raw_length[2]) << 8) | (int)raw_length[3];

                byte[] chunk_type = new byte[4];
                _in.Read(chunk_type, 0, 4);

                Console.WriteLine(System.Text.Encoding.ASCII.GetString(chunk_type));                
                Console.WriteLine(l);

                byte[] data = new byte[l];
                _in.Read(data, 0, l);

                using(MemoryStream source = new MemoryStream(data))
                {
                    using(MemoryStream output = new MemoryStream())
                    {
                        using (var deflate = new ZLibStream(source, CompressionMode.Decompress))
                        {
                            deflate.CopyTo(output);
                            raw = output.ToArray();
                        };
                    };
                };
            }

            // ****

/*
            // move over width rather than height

            int height_half = height / 2;
            for(int y = 0; y < height_half; ++y)
            {
                int source_ofs = y * (width + 1);
                int dest_ofs = (y + height_half) * (width + 1);

                for(int x = 0; x < width; ++x)
                {
                    byte a = raw[source_ofs + x];
                    byte b = raw[dest_ofs + x];

                    raw[dest_ofs + x] = (byte)(a ^ b);
                }   
            }
*/
            // ****
/*
            using(Image<Rgba32> dest = new Image<Rgba32>(width, height))
            {
                for(int y = 0; y < height; ++y)
                {
                    int offset = y * (width + 1);
                    for(int x = 0; x < width; ++x)
                    {
                        byte src = raw[offset + x];
                        int idx = (((int)src) * 3);
                        byte red = palette[idx];
                        byte green = palette[idx + 1];
                        byte blue = palette[idx + 2];

                        Rgba32 pixel = new Rgba32(red,green,blue);//src,src,src);
                        dest[x,y] = pixel;
                    }
                }

                dest.SaveAsPng("output_dmp2.png");
            }*/

            int height_half = height / 2;
            int width_half = width / 2;

/*
            using(Image<Rgba32> dest = new Image<Rgba32>(width, height))
            {
                for(int y = 0; y < height_half; ++y)
                {
                    int a_offset = y * (width + 1);
                    int b_offset = (y + height_half) * (width + 1);
                
                    for(int x = 0; x < width; ++x)
                    {
                        byte a = raw[a_offset + x];
                        byte b = raw[b_offset + x];

                        int a_idx = (((int)a) * 3);
                        byte a_red = palette[a_idx];
                        byte a_green = palette[a_idx + 1];
                        byte a_blue = palette[a_idx + 2];

                        int b_idx = (((int)b) * 3);
                        byte b_red = palette[b_idx];
                        byte b_green = palette[b_idx + 1];
                        byte b_blue = palette[b_idx + 2];

                        byte red = (byte)(a_red ^ b_red);
                        byte green = (byte)(a_green ^ b_green);
                        byte blue = (byte)(a_blue ^ b_blue);

                        Rgba32 pixel = new Rgba32(red,green,blue);//src,src,src);
                        dest[x,y] = pixel;
                    }
                }

                dest.SaveAsPng("output_dmp3.png");
            }
            */


            using(Image<Rgba32> dest = new Image<Rgba32>(width, height))
            {
                for(int y = 0; y < height_half; ++y)
                {
                    int a_offset = y * (width + 1);
                    int b_offset = (y + height_half) * (width + 1);
                
                    for(int x = 0; x < width; ++x)
                    {
                        byte a = raw[a_offset + x];
                        byte b = raw[b_offset + x];

                        int a_idx = (((int)a) * 3);
                        byte a_red = palette[a_idx];
                        byte a_green = palette[a_idx + 1];
                        byte a_blue = palette[a_idx + 2];

                        int b_idx = (((int)b) * 3);
                        byte b_red = palette[b_idx];
                        byte b_green = palette[b_idx + 1];
                        byte b_blue = palette[b_idx + 2];

                        byte tred = (byte)((((int)a_red) + ((int)b_red)) / 2);
                        byte tgreen = (byte)((((int)a_green) + ((int)b_green)) / 2);
                        byte tblue = (byte)((((int)a_blue) + ((int)b_blue)) / 2);

                        byte red = (byte)(a_red ^ b_red);
                        byte green = (byte)(a_green ^ b_green);
                        byte blue = (byte)(a_blue ^ b_blue);

                        Rgba32 pixel = new Rgba32(tred,tgreen,tblue);//tgreen,tblue);//src,src,src);
                        dest[x,y] = pixel;
                    }
                }

                dest.SaveAsPng("output_dmp3.png");
            }

        }

        static void Main(string[] args)
        {
            EncodeMessageZLib.PutMessage("cake.png", "output_cake.png", "Hello World moo baa moo baa");
            Console.WriteLine(EncodeMessageZLib.GetMessage("output_cake.png"));
            //EncodeMessageZLib.AddMessage("S.O.S.png", "output_msg.png", "Hello World");
            //DumperJustRaw("S.O.S.png");
            //Dumper("S.O.S.png");
            //Something("S.O.S.png");
            //ToImage("data.dmp");
            //Console.WriteLine(Decode2("S.O.S.png"));
            //Encode("images/cake.png", "hello world!");
            //Console.WriteLine(Decode("output.png"));            
        }
    };
};