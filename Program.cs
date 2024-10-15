using System.Text;

namespace Cryptography
{
    class Program
    {
        static void Main(string[] args)
        {
            EncodeMessage.PutMessage("cake.png", "output.png", "Hello World");
            Console.WriteLine(EncodeMessage.GetMessage("output.png"));
        }
    };
};