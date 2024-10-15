using System.Text;
using System.IO.Compression;

namespace Cryptography
{
    namespace PNG
    {
        public class Decoder
        {
            static byte[] pdfId = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        
            public static string Get(string source)
            {
                string result = "";
                byte[]? sourceData = null;

                // ***
                // Load source PNG file into memory (byte array)
                // ***
                using(FileStream _in = File.OpenRead(source))
                {
                    _in.Seek(0, SeekOrigin.End);
                    int length = (int)_in.Position;

                    sourceData = new byte[length];

                    _in.Seek(0,SeekOrigin.Begin);
                    _in.Read(sourceData, 0, length);

                    _in.Close();
                }

                // ***
                // Decode source PNG file, find all the chunks, types, offsets & lengths
                // ***
                Image image = new Image(ref sourceData);
                
                // ***
                // For all PNG "IDAT" chunks. combine into a single data and then decompress using Zlib.
                // ***
                byte[]? rawSource = null;
                using(MemoryStream output = new MemoryStream())
                {             
                    using(MemoryStream input = new MemoryStream())
                    {
                        foreach(Chunk chunk in image.Chunks)
                        {
                            if(chunk.Type == "IDAT")
                            {
                                input.Write(sourceData, chunk.Offset + 8, chunk.Length);
                            }
                        }

                        input.Seek(0, SeekOrigin.Begin);
                        using (var inflate = new ZLibStream(input, CompressionMode.Decompress))
                        {
                            byte[] buffer = new byte[1024];
                            int len;
                            while ((len = inflate.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                output.Write(buffer, 0, len);
                            };
                            output.Flush();                          
                        };
                    };
                    
                    rawSource = output.ToArray();


                    // ***
                    // Find the message at the end of the decompressed Image data, limiting the decode process to certain colour types 
                    // and bit depths is a lazy hack for the computation of the offset of the message in the data.
                    // Colours are locked to 8 bits minimum for each pixel, or in the format of RGB, or RGBA.
                    // ***
                    if(image.Depth == 8)
                    {
                        int numberOfBytes = 1;
                        
                        if(image.ColourType == 2)
                            numberOfBytes = 3;
                        else if(image.ColourType == 6)
                            numberOfBytes = 4;

                        int offset = (image.Width * image.Height * numberOfBytes) + image.Height;
                        int diff = rawSource.Length - offset;

                        byte[] raw_message = new byte[diff];

                        for(int i = 0; i < diff; ++i)
                        {
                            raw_message[i] = rawSource[i + offset];
                        }

                        result = Encoding.ASCII.GetString(raw_message);
                    }
                };

                return result;
            }
        };
    };

};