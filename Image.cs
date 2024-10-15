using System.Text;

namespace Cryptography
{
    public class Image
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

        public static Image GetImageSpecs(ref byte[] data)
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
    };
};