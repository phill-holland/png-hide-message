using System.Text;
using System.IO.Compression;
using System.Runtime.Intrinsics.Arm;

namespace Steganography
{
    public class EncodeMessageZLib
    {
        class Chunk
        {
            public int length = 0;
            public int offset = 0;
            public string type = "";
            public uint crc32 = 0;     
        };

        class Image
        {
            public int width;
            public int height;
            public int depth;        
            public int colour_type;            
            public int compression_method;
            public int filter_method;
            public int interlace_method;

            public List<Chunk> chunks = new List<Chunk>();

            public Chunk? Last(string type)
            {
                for(int i = chunks.Count - 1; i >= 0; i--)
                {
                    Chunk c = chunks[i];
                    if(c.type == type) return c;
                }

                return null;
            }        
        };

        private static Image GetImageSpecs(ref byte[] data)
        {
            Image result = new Image();
            int offset = 8;

            do
            {
                Chunk chunk = new Chunk();

                chunk.offset = offset;
                chunk.length = (((int)data[offset]) << 24) | (((int)data[offset + 1]) << 16) |(((int)data[offset + 2]) << 8) | (int)data[offset + 3];
                offset += 4;

                byte[] type = new byte[4];
                type[0] = data[offset];
                type[1] = data[offset + 1];
                type[2] = data[offset + 2];
                type[3] = data[offset + 3];

                chunk.type = Encoding.ASCII.GetString(type);
                
                offset += 4;

                if(chunk.type == "IHDR")
                {
                    result.width = (((int)data[offset]) << 24) | (((int)data[offset + 1]) << 16) |(((int)data[offset + 2]) << 8) | (int)data[offset + 3];
                    result.height = (((int)data[offset + 4]) << 24) | (((int)data[offset + 5]) << 16) |(((int)data[offset + 6]) << 8) | (int)data[offset + 7];
                    result.depth = (int)data[offset + 8];
                    result.colour_type = (int)data[offset + 9];
                    result.compression_method = (int)data[offset + 10];
                    result.filter_method = (int)data[offset + 11];
                    result.interlace_method = (int)data[offset + 12];
                }

                offset += chunk.length;

                chunk.crc32 = (((uint)data[offset]) << 24) | (((uint)data[offset + 1]) << 16) |(((uint)data[offset + 2]) << 8) | (uint)data[offset + 3];
                offset += 4;

                result.chunks.Add(chunk);

            }while(offset < data.Length);
        
            return result;
        }

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

            Image image = GetImageSpecs(ref source_data);
            
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

                        Crc crc = new Crc();
                        ulong c = crc.crc(ref raw_output, 0, raw_output.Length);

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

            Image image = GetImageSpecs(ref source_data);
            
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

        public static void AddMessage(string source, string destination, string message)
        {            
            const int idat_index = 813;
            const int width = 984, height = 974;

            byte[]? data = null;

            using(FileStream _in = File.OpenRead(source))
            {
                _in.Seek(0, SeekOrigin.End);
                int length = (int)_in.Position;

                data = new byte[length];

                _in.Seek(0,SeekOrigin.Begin);
                _in.Read(data, 0, length);
            }


            Image image = GetImageSpecs(ref data);

            int idat_length = (((int)data[idat_index]) << 24) | (((int)data[idat_index + 1]) << 16) |(((int)data[idat_index + 2]) << 8) | (int)data[idat_index + 3];
        
            int idat_chunk_index = idat_index + 4;                        
            byte[] chunk_type = new byte[4];
            chunk_type[0] = data[idat_chunk_index];
            chunk_type[1] = data[idat_chunk_index + 1];
            chunk_type[2] = data[idat_chunk_index + 2];
            chunk_type[3] = data[idat_chunk_index + 3];

            Console.WriteLine(System.Text.Encoding.ASCII.GetString(chunk_type));

            byte[] message_ascii = Encoding.ASCII.GetBytes(message);

            //int raw_new_length = (width * height) + message_ascii.Length;
            byte[]? raw_source = null;//new byte[raw_new_length];


    //Crc crc2 = new Crc();
    //ulong c2 = crc2.crc(ref data, 813 + 4, idat_length + 4);
    // 19C9659D

            using(MemoryStream stream = new MemoryStream(data, 813 + 8, idat_length))
            {
                using(MemoryStream output = new MemoryStream())
                {
                    using (var inflate = new ZLibStream(stream, CompressionMode.Decompress))
                    {
                        inflate.CopyTo(output);
                        raw_source = output.ToArray();
                    };
                };
            };

// ***
/*
int w_by_h = width * height;
int diff = raw_source.Length - w_by_h;
string moobaa = "";
for(int i = 0; i < diff; ++i)
{
    
    moobaa += Convert.ToString(raw_source[w_by_h + i]) + " ";
}
Console.WriteLine(moobaa);

using(FileStream _out = File.OpenWrite("diff.dmp"))
{
    _out.Write(raw_source,w_by_h,diff);
}
*/
//***

/*
            for(int i = 0; i < message_ascii.Length; ++i)
            {
                raw_source[(width * height) + i] = message_ascii[i];
            }
*/
            using(FileStream _out = File.OpenWrite(destination))
            {
                byte[]? compressed = null;

                //using(MemoryStream stream = new MemoryStream(raw_source, 0, raw_new_length))
                //{
                    using(MemoryStream output = new MemoryStream())
                    {
                        using (var compressor = new ZLibStream(output, CompressionMode.Compress))
                        {
                            compressor.Write(raw_source, 0, raw_source.Length);//raw_new_length);
                            //compressor.CopyTo(output);
                            compressed = output.ToArray();
                        };
                    };
                //};

                // ***
                _out.Write(data, 0, idat_index);

                byte[] out_length = new byte[4];
                out_length[0] = (byte)((compressed.Length >> 24) & 0xFF);
                out_length[1] = (byte)((compressed.Length >> 16) & 0xFF);
                out_length[2] = (byte)((compressed.Length >> 8) & 0xFF);
                out_length[3] = (byte)((compressed.Length) & 0xFF);

                byte[] idat_id = Encoding.ASCII.GetBytes("IDAT");

// *** CRC calc, including chunk_type code and data
                byte[] _d = new byte[compressed.Length + 4];
                _d[0] = idat_id[0];
                _d[1] = idat_id[1];
                _d[2] = idat_id[2];                
                _d[3] = idat_id[3];
                for(int j = 0; j < compressed.Length; ++j) _d[j + 4] = compressed[j];

                _out.Write(out_length, 0, 4);
                _out.Write(idat_id, 0, 4);
                _out.Write(compressed);
                // ***

                Crc crc = new Crc();
                ulong c = crc.crc(ref _d, 0, compressed.Length + 4);
// ***
                //ulong c = crc.crc(ref idat_id, 0, 4);
                //c = crc.update_crc(c, ref compressed, 0, compressed.Length);

                byte[] out_crc = new byte[4];
                out_crc[0] = (byte)((c >> 24) & 0xFF);
                out_crc[1] = (byte)((c >> 16) & 0xFF);
                out_crc[2] = (byte)((c >> 8) & 0xFF);
                out_crc[3] = (byte)((c) & 0xFF);

                _out.Write(out_crc, 0, 4);

                // ***

                int source_offset = idat_index + 4 + 4 + idat_length + 4;            

                _out.Write(data, source_offset, data.Length - source_offset);
            }
        }

/*
        public static void DumperJustRaw(String filename)
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

// PLTE
                Console.WriteLine("pal length:" + Convert.ToString(pl) + " " + Encoding.ASCII.GetString(pal_chunk_type));
                _in.Read(palette, 0, pl);

                // ***
                // load data
                // ***
                _in.Seek(813, SeekOrigin.Begin);

                byte[] raw_length = new byte[4];
                _in.Read(raw_length, 0, 4);

                int l = (((int)raw_length[0]) << 24) | (((int)raw_length[1]) << 16) |(((int)raw_length[2]) << 8) | (int)raw_length[3];

                byte[] chunk_type = new byte[4];
                _in.Read(chunk_type, 0, 4);

// IDAT
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
        }*/
    };
};