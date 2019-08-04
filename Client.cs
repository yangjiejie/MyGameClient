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
        SocketAsyncEventArgs m_recvArgs;
        /// </summary>
        /// 

        ConnectState m_cs = ConnectState.CONNECT_FAIL;
        Socket m_socket;
        //msdn的文档
        //https://docs.microsoft.com/zh-cn/dotnet/api/system.net.sockets.socket.connectasync?view=netframework-4.8#System_Net_Sockets_Socket_ConnectAsync_System_Net_Sockets_SocketAsyncEventArgs_
        SocketAsyncEventArgs m_connEventArg;

        public  GameClient()
        {
            m_connEventArg = new SocketAsyncEventArgs();
            m_sendArgs = new SocketAsyncEventArgs();
            m_recvArgs = new SocketAsyncEventArgs();

            m_sendArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(this.SendCallBack);
            m_sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.SendCallBack);


            

            m_recvArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(this.RecvCallBack);
            m_recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.RecvCallBack);
        }

        
        public void SendCallBack(object target, SocketAsyncEventArgs e)
        {

        }

        public void RecvCallBack(object target, SocketAsyncEventArgs e)
        {
            var str = Encoding.Default.GetString(e.Buffer);
            Console.WriteLine(str);
        }

        public void OnTimer(object o)
        {
            m_sendArgs.BufferList = NetUtil.CreateBuffList<byte>(Encoding.Default.GetBytes("yangjienihaozaoshang"),
                4);

            m_socket.SendAsync(m_sendArgs);
        }
        public void OnSucessLogin()
        {
            //string str = "nihao";
            //var bytes = System.Text.Encoding.Default.GetBytes(str);
            //m_socket.Send(bytes);


            Timer ts = new Timer(this.OnTimer, null, 100, 1000);
           


            //接受消息 
            byte[] buffer = new byte[1000];
            m_recvArgs.SetBuffer(buffer,0,1000);
            m_socket.ReceiveAsync(m_recvArgs);
            
            //m_socket
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
                        create();
                        
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
        public void initcfg(string remoteAddress , int remotePort)
        {
            //string remoteAddress = "127.0.0.1";
            //int remotePort = 9002;
            IPAddress myIp;
           
            if (IPAddress.TryParse(remoteAddress, out myIp))
            {
                m_ipEndPoint = new IPEndPoint(myIp, remotePort);
            }
        }

        public void create()
        {
            Console.WriteLine("创建套接字");
            if (m_ipEndPoint == null) return; 
            
            if(m_socket != null)
            {
                m_socket.Close();
                m_socket = null;
            }
            
            m_socket = new Socket(m_ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
           

            
        }

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
            if (m_connEventArg != null)
            {
                m_connEventArg.Dispose();
                m_connEventArg = null;
            }
            m_connEventArg = new SocketAsyncEventArgs();
            m_connEventArg.RemoteEndPoint = m_ipEndPoint;
            m_connEventArg.UserToken = "";

            m_connEventArg.Completed -= new EventHandler<SocketAsyncEventArgs>(OnCompletedForConnect);
            m_connEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnCompletedForConnect);

            if (m_socket != null)
            {
                if (!m_socket.ConnectAsync(m_connEventArg))
                { //连接失败也要做回调？？
                    Console.WriteLine("进来了吗");
                    OnCompletedForConnectImpl(m_connEventArg);
                }
                
            }
        }

        public void init(string remoteAddress, int remotePort)
        {
            initcfg( remoteAddress,  remotePort);
            //读取配置 获取ip端口这些东西 
            create();
            connectAsync();
        }

        public void OnCompletedForConnect(object sender, SocketAsyncEventArgs s)
        {
            OnCompletedForConnectImpl(s);
        }


    }
}
