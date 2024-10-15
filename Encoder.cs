using System.Text;
using System.IO.Compression;

namespace Cryptography
{
    namespace PNG
    {
        public class Encoder
        {
            static byte[] pdfId = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        
            public static void Put(string source, string destination, string msg)
            {
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
                };

                // ***
                // Take the decompressed RAW PNG image data, and add the message in ASCII format to the end
                // ***
                byte[] message = Encoding.ASCII.GetBytes(msg);
                byte[] rawSourceAndMessage = new byte[rawSource.Length + message.Length];
                
                for(int i = 0; i < rawSource.Length; ++i)
                {
                    rawSourceAndMessage[i] = rawSource[i];
                }
                
                for(int i = 0; i < message.Length; ++i)
                {
                    rawSourceAndMessage[i + rawSource.Length] = message[i];
                }

                // ***
                // Recompress RAW PNG image data and message
                // ***
                byte[]? compressed = null;
                using(MemoryStream output = new MemoryStream())
                {
                    using (var compressor = new ZLibStream(output, CompressionMode.Compress))
                    {                           
                        compressor.Write(rawSourceAndMessage, 0, rawSourceAndMessage.Length);         
                        compressor.Close();                      
                        compressed = output.ToArray();
                    };
                };

                // ***
                // Rewrite new PNG file, take all chunks from original file, except "IDAT" chunks and rewrite, creating a single
                // new "IDAT" chunk
                // ***
                Chunk last = image.Last("IDAT");
                using(FileStream _out = File.OpenWrite(destination))
                {
                    _out.Write(pdfId, 0, pdfId.Length);

                    foreach(Chunk chunk in image.Chunks)
                    {
                        if(chunk == last)
                        {   
                            byte[] type = Encoding.ASCII.GetBytes("IDAT");

                            byte[] rawOutput = new byte[compressed.Length + 4];
                            rawOutput[0] = type[0]; rawOutput[1] = type[1]; rawOutput[2] = type[2]; rawOutput[3] = type[3];
                            for(int i = 0; i < compressed.Length; ++i)
                            {
                                rawOutput[i + 4] = compressed[i];
                            }
                            
                            byte[] length = new byte[4];
                            length[0] = (byte)((compressed.Length >> 24) & 0xFF);
                            length[1] = (byte)((compressed.Length >> 16) & 0xFF);
                            length[2] = (byte)((compressed.Length >> 8) & 0xFF);
                            length[3] = (byte)((compressed.Length) & 0xFF);

                            Crc32 crc = new Crc32();
                            ulong c = crc.Calculate(ref rawOutput, 0, rawOutput.Length);

                            byte[] rawCrc = new byte[4];
                            rawCrc[0] = (byte)((c >> 24) & 0xFF);
                            rawCrc[1] = (byte)((c >> 16) & 0xFF);
                            rawCrc[2] = (byte)((c >> 8) & 0xFF);
                            rawCrc[3] = (byte)((c) & 0xFF);

                            _out.Write(length, 0, 4);
                            _out.Write(rawOutput);
                            _out.Write(rawCrc, 0, 4);
            
                        }
                        else if(chunk.Type != "IDAT")
                        {
                            _out.Write(sourceData, chunk.Offset, chunk.Length + 4 + 4 + 4);
                        }
                    }    

                    _out.Close();        
                }
            }
        };
    };
};