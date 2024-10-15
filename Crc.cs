namespace Steganography
{
    public class Crc
    {
        private ulong[] crc_table = new ulong[256];
        private bool crc_table_computed = false;

        private void make_crc_table()
        {
            ulong c;
            int n, k;
        
            for (n = 0; n < 256; n++) 
            {
                c = (ulong) n;
                for (k = 0; k < 8; k++) 
                {
                    if ((c & 1) > 0)
                        c = 0xedb88320L ^ (c >> 1);
                    else
                        c = c >> 1;
                }

                crc_table[n] = c;
            }
            crc_table_computed = true;
        }

        public ulong update_crc(ulong crc, ref byte[] buf, int start, int len)
        {
            ulong c = crc;
            int n;
        
            if (!crc_table_computed)
                make_crc_table();

            for (n = 0; n < len; n++) 
            {
                c = crc_table[(c ^ buf[n + start]) & 0xff] ^ (c >> 8);
            }

            return c;
        }

        public ulong crc(ref byte []buf, int start, int len)
        {
            return update_crc(0xffffffffL, ref buf, start, len) ^ 0xffffffffL;
        }
    };
};