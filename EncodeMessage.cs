using System.Text;
using System.IO.Compression;

namespace Cryptography
{
    public class EncodeMessage
    {
        static byte[] pdfId = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
    
        public static void PutMessage(string source, string destination, string msg)
        {
            byte[]? source_data = null;

            using(FileStream _in = File.OpenRead(source))
            {
                _in.Seek(0, SeekOrigin.End);
                int length = (int)_in.Position;

                source_data = new byte[length];

                _in.Seek(0,SeekOrigin.Begin);
                _in.Read(source_data, 0, length);

                _in.Close();
            }

            Image image = Image.GetImageSpecs(ref source_data);
            
            byte[]? raw_source = null;
            using(MemoryStream output = new MemoryStream())
            {             
                using(MemoryStream input = new MemoryStream())
                {
                    foreach(Chunk chunk in image.chunks)
                    {
                        if(chunk.type == "IDAT")
                        {
                            input.Write(source_data, chunk.offset + 8, chunk.length);
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
                
                raw_source = output.ToArray();
            };

            byte[] message = Encoding.ASCII.GetBytes(msg);//"Hello World");
            byte[] raw_source_and_message = new byte[raw_source.Length + message.Length];
            
            for(int i = 0; i < raw_source.Length; ++i)
            {
                raw_source_and_message[i] = raw_source[i];
            }
            
            for(int i = 0; i < message.Length; ++i)
            {
                raw_source_and_message[i + raw_source.Length] = message[i];
                //Console.WriteLine(message[i]);
            }

            byte[]? compressed = null;
            using(MemoryStream output = new MemoryStream())
            {
                using (var compressor = new ZLibStream(output, CompressionMode.Compress))
                {                    
                    //compressor.Write(raw_source, 0, raw_source.Length);         
                    compressor.Write(raw_source_and_message, 0, raw_source_and_message.Length);         
                    compressor.Close();                      
                    compressed = output.ToArray();
                };
            };

            Chunk last = image.Last("IDAT");

            using(FileStream _out = File.OpenWrite(destination))
            {
                _out.Write(pdfId, 0, pdfId.Length);

                foreach(Chunk chunk in image.chunks)
                {
                    if(chunk == last)
                    {   
                        byte[] type = Encoding.ASCII.GetBytes("IDAT");

                        byte[] raw_output = new byte[compressed.Length + 4];
                        raw_output[0] = type[0]; raw_output[1] = type[1]; raw_output[2] = type[2]; raw_output[3] = type[3];
                        for(int i = 0; i < compressed.Length; ++i)
                        {
                            raw_output[i + 4] = compressed[i];
                        }
                        
                        byte[] length = new byte[4];
                        length[0] = (byte)((compressed.Length >> 24) & 0xFF);
                        length[1] = (byte)((compressed.Length >> 16) & 0xFF);
                        length[2] = (byte)((compressed.Length >> 8) & 0xFF);
                        length[3] = (byte)((compressed.Length) & 0xFF);

                        Crc32 crc = new Crc32();
                        ulong c = crc.Calculate(ref raw_output, 0, raw_output.Length);

                        byte[] raw_crc = new byte[4];
                        raw_crc[0] = (byte)((c >> 24) & 0xFF);
                        raw_crc[1] = (byte)((c >> 16) & 0xFF);
                        raw_crc[2] = (byte)((c >> 8) & 0xFF);
                        raw_crc[3] = (byte)((c) & 0xFF);

                        _out.Write(length, 0, 4);
                        _out.Write(raw_output);
                        _out.Write(raw_crc, 0, 4);
         
                    }
                    else if(chunk.type != "IDAT")
                    {
                        //Console.WriteLine(chunk.type + " " + chunk.offset + " " + chunk.length);
                        _out.Write(source_data, chunk.offset, chunk.length + 4 + 4 + 4);
                    }
                }    

                _out.Close();        
            }
        }

        public static string GetMessage(string source)
        {
            string result = "";
            byte[]? source_data = null;

            using(FileStream _in = File.OpenRead(source))
            {
                _in.Seek(0, SeekOrigin.End);
                int length = (int)_in.Position;

                source_data = new byte[length];

                _in.Seek(0,SeekOrigin.Begin);
                _in.Read(source_data, 0, length);

                _in.Close();
            }

            Image image = Image.GetImageSpecs(ref source_data);
            
            byte[]? raw_source = null;
            using(MemoryStream output = new MemoryStream())
            {             
                using(MemoryStream input = new MemoryStream())
                {
                    foreach(Chunk chunk in image.chunks)
                    {
                        if(chunk.type == "IDAT")
                        {
                            input.Write(source_data, chunk.offset + 8, chunk.length);
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
                
                raw_source = output.ToArray();

                if(image.depth == 8)
                {
                    int number_of_bytes = 1;
                    
                    if(image.colour_type == 2)
                        number_of_bytes = 3;
                    else if(image.colour_type == 6)
                        number_of_bytes = 4;

                    int offset = (image.width * image.height * number_of_bytes) + image.height;
                    int diff = raw_source.Length - offset;

                    byte[] raw_message = new byte[diff];

                    for(int i = 0; i < diff; ++i)
                    {
                        raw_message[i] = raw_source[i + offset];
                        //result += Convert.ToString(raw_source[i + offset]) + " ";
                    }
                    result = Encoding.ASCII.GetString(raw_message);
                    //Console.WriteLine(result);
                }
            };

            return result;
        }
  };
};