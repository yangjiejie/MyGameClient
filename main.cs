using System;

using System.Net;
using System.Net.Sockets;


namespace GameClient
{
    class main
    {
        static void Main(string[] args) // 这套代码肯定是嵌入到unity或者ue4或者自研引擎里面的
        { // 如何模拟update的流程 ，游戏网络消息的处理是应该有缓存的  在update中对队列进行处理 

            GameClient gc = new GameClient(9002);
            
            Console.ReadKey();
        }
    }
}
