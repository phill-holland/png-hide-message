namespace Cryptography
{
    public class Crc32
    {
        private ulong[] crc_table = new ulong[256];

        private void Initalise()
        {
            ulong c;
            int n, k;
        
            for (n = 0; n < 256; n++) 
            {
                c = (ulong)n;
                for (k = 0; k < 8; k++) 
                {
                    if ((c & 1) > 0)
                        c = 0xEDB88320L ^ (c >> 1);
                    else
                        c = c >> 1;
                }

                crc_table[n] = c;
            }
        }

        public ulong Update(ulong crc, ref byte[] buf, int start, int len)
        {
            ulong c = crc;
            int n;
        
            for (n = 0; n < len; n++) 
            {
                c = crc_table[(c ^ buf[n + start]) & 0xff] ^ (c >> 8);
            }

            return c;
        }

        public ulong Calculate(ref byte []buf, int start, int len)
        {
            Initalise();
            return Update(0xFFFFFFFFL, ref buf, start, len) ^ 0xFFFFFFFFL;
        }
    };
};