namespace Cryptography
{
    class Program
    {
        static void Main(string[] args)
        {
            PNG.Encoder.Put("cake.png", "output.png", "Hello World");
            Console.WriteLine(PNG.Decoder.Get("output.png"));
        }
    };
};