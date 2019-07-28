using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient
{
    class main
    {
        static void Main(string[] args)
        {
            GameClient gc = new GameClient();
           
            gc.init("127.0.0.1",9002);
            Console.ReadKey();
        }
    }
}
