﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TechnoFriendClient
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        DataClasses1DataContext d = new DataClasses1DataContext();
        public LogWindow()
        {
            InitializeComponent();
            historyList.ItemsSource = from k in d.histories
                                      select k;
        }
    }
}
