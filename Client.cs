using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CClient;

namespace GameClient
{

    public enum ConnectState     
    {
        
        CONNECT_FAIL = 1,
        CONNECT_SUS = 2,
    }
    
    public class GameClient
    {


        object lockobj = new object();
        SocketAsyncEventArgs m_sendArgs;
        SocketAsyncEventArgs m_headBeat;
        SocketAsyncEventArgs m_recvArgs;
        public int lastrecvTime = -1;
        private bool useBufferList = false;
        /// </summary>
        /// 

        ConnectState m_cs = ConnectState.CONNECT_FAIL;
        Socket m_socket;
        //msdn的文档
        //https://docs.microsoft.com/zh-cn/dotnet/api/system.net.sockets.socket.connectasync?view=netframework-4.8#System_Net_Sockets_Socket_ConnectAsync_System_Net_Sockets_SocketAsyncEventArgs_
        SocketAsyncEventArgs m_connEventArg;
        Timer m_tm = null;
        public  GameClient(int remotePort)
        {
            IPHostEntry ipe = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] localIpGroup = ipe.AddressList;
            IPAddress ipdress = null;
            for (int i = 0; i < localIpGroup.Length; i++)
            {

                if (localIpGroup[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    ipdress = localIpGroup[i];
                    break;
                }
            }
            m_ipEndPoint = new IPEndPoint(ipdress, remotePort);

            //读取配置 获取ip端口这些东西 
            Create();
            connectAsync();
            
        }

        public void Create()
        {

            Console.WriteLine("创建套接字");
            if (m_ipEndPoint == null) return;

            if (m_socket == null)
            {
                m_socket = new Socket(m_ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }

           

            if(m_connEventArg == null)
            {
                m_connEventArg = new SocketAsyncEventArgs();

                m_connEventArg.RemoteEndPoint = m_ipEndPoint; // 这个参数不能省去 
                m_connEventArg.UserToken = "";

                m_connEventArg.Completed -= new EventHandler<SocketAsyncEventArgs>(OnCompletedForConnect);
                m_connEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnCompletedForConnect);
            }

            if(m_sendArgs == null)
            {
                m_sendArgs = new SocketAsyncEventArgs();
            }

            if (m_recvArgs == null)
            {
                m_recvArgs = new SocketAsyncEventArgs();
                m_recvArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(this.OnRecv);
                m_recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnRecv);
            }

            
            if(m_tm == null)
            {
                m_tm = new Timer(Update, null, 0, 1000);
            }
        }

        public void Update(object state)
        {
           
            Judge();

        }
        //判断心跳 
        public void Judge()
        {
            if (lastrecvTime != -1 && Environment.TickCount - lastrecvTime > 3000)
            {
                Console.WriteLine("应该关闭socket");
                Close(); // 关闭和服务器的连接 
               
            }
        }

        public void Close()
        {
            lastrecvTime = -1;
            thread_sendHead.Abort();
            thread_sendHead = null;
            m_socket.Close();
            m_socket = null;
            m_sendArgs = null;
            m_recvArgs = null;
            m_tm.Dispose();
            m_tm = null;
            m_cs = ConnectState.CONNECT_FAIL;

            
            Create();
            connectAsync();

        }

        public void SendHeadBeat()
        {

            while (true)
            {
                if(m_socket != null)
                {
                    lock (m_socket)
                    {
                        IOSocket io = new IOSocket();
                        io.WriteInt32(NetMsgDefine.HeadBeat);
                        io.WriteByte(1);
                        if (m_headBeat == null)
                        {
                            m_headBeat = new SocketAsyncEventArgs();
                        }
                        m_headBeat.SetBuffer(io.GetBuffer(), 0, io.GetLength());
                        m_socket.SendAsync(m_headBeat);
                    }
                }
                
                
                
                Thread.Sleep(1000);
            }

        }

        public void OnRecv(object target, SocketAsyncEventArgs e)
        {
            IOSocket io = new IOSocket(e.Buffer);
            io.Seek(0);
            int itype = io.ReadInt32();
            switch (itype)
            {
                case NetMsgDefine.HeadBeat: // 心跳包
                    {
                        byte b=  io.ReadByte();
                        lastrecvTime = Environment.TickCount;
                        Console.WriteLine("这是心跳包，收到了服务器发来的回复{0}",b);
                    }
                    
                    break;
                case NetMsgDefine.sayhello:
                    {
                        string str = io.ReadString8();
                        Console.WriteLine(str);
                    }
                    break;
                case NetMsgDefine.GameLogic: // 游戏逻辑

                    break;
                default:
                    break;
            }
            
           
           
            
            Recv();

           




        }
        Thread thread_sendHead = null;


        public void OnSucessLogin()
        {
          


            IOSocket io = new IOSocket();

            io.WriteInt32(NetMsgDefine.sayhello);
            io.WriteString16("im client");

            
            //这是我测试bufflist的方法 我们
            if(useBufferList)
            {
                m_sendArgs.BufferList = NetUtil.CreateBuffList<byte>(Encoding.Default.GetBytes("我是客户端"),
                4);
            }
            else
            {
                m_sendArgs.SetBuffer(io.GetBuffer(), 0, io.GetLength());
            }
            m_socket.SendAsync(m_sendArgs);

            Recv();

            ////方法一：使用Thread类
            ThreadStart threadStart = new ThreadStart(SendHeadBeat);//通过ThreadStart委托告诉子线程执行什么方法　　
            if(thread_sendHead == null)
            {
                thread_sendHead =  new Thread(threadStart);
                thread_sendHead.Start();//启动新线程
            }
        }

        public void Recv()
        {

            //接受消息的线程 
            byte[] buffer = new byte[1000];
            m_recvArgs.RemoteEndPoint = m_socket.RemoteEndPoint;
            m_recvArgs.SetBuffer(buffer, 0, 1000);
            m_socket.ReceiveAsync(m_recvArgs);



           

        }
       
        //这个应该不在主线程 
        private void OnCompletedForConnectImpl(SocketAsyncEventArgs e)
        {
            
            if(e.SocketError == SocketError.Success)
            {

                lock (lockobj)
                {
                    m_cs = ConnectState.CONNECT_SUS;
                    Console.WriteLine("连接成功");
                    OnSucessLogin();

                }
            }
            else
            {
                lock(lockobj)
                {
                    m_cs = ConnectState.CONNECT_FAIL;
                    //改为同步去连接服务器 
                    
                    while(true)
                    {
                        Create();
                        
                        connectSync();
                        if (m_cs == ConnectState.CONNECT_FAIL)
                        {

                        }
                        else
                        {
                            break;
                        }
                        Thread.Sleep(1000);
                    }
                    
                }
            }
            
        }
        IPEndPoint m_ipEndPoint = null;
      

        

        public void connectSync()
        {
            if (m_socket != null)
            {
                try
                {
                    m_socket.Connect(m_ipEndPoint);
                    lock (lockobj)
                    {
                        m_cs = ConnectState.CONNECT_SUS;
                        Console.WriteLine("连接成功");
                        OnSucessLogin();
                    }
                }
                catch(Exception e)
                {
                    lock (lockobj)
                    {
                        m_cs = ConnectState.CONNECT_FAIL;
                        Console.WriteLine("连接失败");
                    }
                }
                
               
            }
        }

        public void connectAsync()
        {
            
            
           

            if (m_socket != null)
            {
                if (!m_socket.ConnectAsync(m_connEventArg))
                { //连接失败也要做回调？？
                    Console.WriteLine("进来了吗");
                    OnCompletedForConnectImpl(m_connEventArg);
                }
                
            }
        }

        

        public void OnCompletedForConnect(object sender, SocketAsyncEventArgs s)
        {
            OnCompletedForConnectImpl(s);
        }
                   

    }
}
