using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InfoHelper.ViewModel.DataEntities
{
    public class WindowInfo : IComparable<WindowInfo>
    {
        public Rect Rectangle { get; }

        public WindowState WindowState { get; }

        public bool IsHeroActing { get; }

        public WindowInfo(Rect rect, WindowState winState, bool isHeroActing)
        {
            Rectangle = rect;
            WindowState = winState;
            IsHeroActing = isHeroActing;
        }

        public int CompareTo(WindowInfo other)
        {
            int thisHash = Rectangle.GetHashCode(), otherHash = other.Rectangle.GetHashCode();

            if (thisHash > otherHash)
                return 1;

            if (thisHash < otherHash)
                return -1;

            return 0;
        }
    }

    public enum WindowState
    {
        OkFront,
        OkBack,
        WrongCaptionFront,
        WrongCaptionBack,
        ErrorFront,
        ErrorBack
    }
}
