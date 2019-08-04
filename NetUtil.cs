using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CClient
{
    public class NetUtil
    {
        //size 多少个字节为一个buffer
        public static IList<ArraySegment<T>> CreateBuffList<T>(T[] buffer,int size)
        {
            IList<ArraySegment<T>> tt = new List<ArraySegment<T>>();

            int fen = buffer.Length / size;
            int leftfen = buffer.Length % size;

            for(int i = 0; i < fen; i++)
            {
                tt.Add(new ArraySegment<T>(buffer, i * size, size));
            }
            if(leftfen != 0)
            {
                tt.Add(new ArraySegment<T>(buffer,fen*size , leftfen));
            }

            return tt;
        }
    }
}
