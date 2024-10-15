namespace Cryptography
{
    class Program
    {
        static void Main(string[] args)
        {
            PNG.Encoder.Put("images/cake.png", "images/output.png", "Hello World");
            Console.WriteLine(PNG.Decoder.Get("images/output.png"));
        }
    };
};