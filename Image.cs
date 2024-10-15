using System.Text;

namespace Cryptography
{
    public class Image
    {
        public int Width;
        public int Height;
        public int Depth;        
        public int ColourType;            
        public int CompressionMethod;
        public int FilterMethod;
        public int InterlaceMethod;

        public List<Chunk> Chunks = new List<Chunk>();

        public Image()
        {
            Clear();
        }

        public Image(ref byte[] data)
        {
            Clear();
            Get(ref data);
        }

        public void Clear()
        {
            Width = 0; 
            Height = 0; 
            Depth = 0;
            ColourType = 0;
            CompressionMethod = 0;
            FilterMethod = 0;
            InterlaceMethod = 0;
            Chunks.Clear();
        }

        public Chunk? Last(string type)
        {
            for(int i = Chunks.Count - 1; i >= 0; i--)
            {
                Chunk chunk = Chunks[i];
                if(chunk.Type == type) return chunk;
            }

            return null;
        }      

        public void Get(ref byte[] data)
        {
            int offset = 8;

            do
            {
                Chunk chunk = new Chunk();

                chunk.Offset = offset;
                chunk.Length = (((int)data[offset]) << 24) | (((int)data[offset + 1]) << 16) |(((int)data[offset + 2]) << 8) | (int)data[offset + 3];
                offset += 4;

                byte[] type = new byte[4];
                type[0] = data[offset];
                type[1] = data[offset + 1];
                type[2] = data[offset + 2];
                type[3] = data[offset + 3];

                chunk.Type = Encoding.ASCII.GetString(type);
                
                offset += 4;

                if(chunk.Type == "IHDR")
                {
                    Width = (((int)data[offset]) << 24) | (((int)data[offset + 1]) << 16) |(((int)data[offset + 2]) << 8) | (int)data[offset + 3];
                    Height = (((int)data[offset + 4]) << 24) | (((int)data[offset + 5]) << 16) |(((int)data[offset + 6]) << 8) | (int)data[offset + 7];
                    Depth = (int)data[offset + 8];
                    ColourType = (int)data[offset + 9];
                    CompressionMethod = (int)data[offset + 10];
                    FilterMethod = (int)data[offset + 11];
                    InterlaceMethod = (int)data[offset + 12];
                }

                offset += chunk.Length;

                chunk.Crc32 = (((uint)data[offset]) << 24) | (((uint)data[offset + 1]) << 16) |(((uint)data[offset + 2]) << 8) | (uint)data[offset + 3];
                offset += 4;

                Chunks.Add(chunk);

            } while(offset < data.Length);
        }  
    };
};