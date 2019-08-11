using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  GameClient
{
    public class NetMsgDefine
    {
        public const int HeadBeat = 1; //心跳 
        public const int GameLogic = 2; // 游戏逻辑 
        public const int sayhello = 3; // 打招呼
    }

    public struct HeadBeatStruct
    {
        public int msgtype; // 消息类型
        public byte data; // 发一个1过来 
    }

    public struct SayHelloStruct // 客户端打招呼
    {
        public int msgtype;
        public string str;
    }

    //游戏逻辑开始

    //游戏逻辑结束

}
