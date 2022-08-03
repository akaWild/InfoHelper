using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.Utils
{
    public class BitmapsContainer
    {
        private readonly Queue<Bitmap> _bitmaps = new Queue<Bitmap>();

        private readonly object _bmpLock = new object();

        public int Capacity { get; set; }

        public int Count { get; private set; }

        public BitmapsContainer(int capacity)
        {
            Capacity = capacity;
        }

        public void Add(Bitmap bmp)
        {
            lock (_bmpLock)
            {
                if (_bitmaps.Count == Capacity)
                {
                    Bitmap outBmp = _bitmaps.Dequeue();

                    Count--;

                    outBmp.Dispose();
                }

                _bitmaps.Enqueue(bmp);

                Count++;
            }
        }

        public Bitmap[] GetImages()
        {
            List<Bitmap> outBmps = new List<Bitmap>();

            lock (_bmpLock)
            {
                while (Count > 0)
                {
                    Bitmap bmp = _bitmaps.Dequeue();

                    outBmps.Add(bmp);

                    Count--;
                }
            }

            return outBmps.ToArray();
        }
    }
}
