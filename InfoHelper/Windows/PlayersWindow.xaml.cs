﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using InfoHelper.Utils;

namespace InfoHelper.Windows
{
    /// <summary>
    /// Interaction logic for PlayersWindow.xaml
    /// </summary>
    public partial class PlayersWindow : Window
    {
        public PlayersWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            e.Cancel = true;

            Hide();
        }
    }
}
