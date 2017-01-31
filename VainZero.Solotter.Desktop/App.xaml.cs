using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;

namespace VainZero.Solotter.Desktop
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var themeManager = new ThemeManager();
            themeManager.Load(Resources, "Indigo");
        }
    }
}
