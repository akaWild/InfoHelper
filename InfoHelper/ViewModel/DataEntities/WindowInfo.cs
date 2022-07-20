using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InfoHelper.ViewModel.DataEntities
{
    public class WindowInfo
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
    }

    public enum WindowState
    {
        OkFront,
        OkBack,
        WrongCaption,
        Error
    }
}
