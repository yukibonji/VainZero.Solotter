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
    public sealed class ThemeManager
    {
        static ResourceDictionary[] LoadDictionaries(string colorName)
        {
            var sources =
                new[]
                {
                    $"/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.{colorName}.xaml",
                    $"/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.{colorName}.xaml",
                };
            return
                sources
                .Select(source =>
                {
                    var uri = new Uri(source, UriKind.Relative);
                    return (ResourceDictionary)Application.LoadComponent(uri);
                }).ToArray();
        }

        public void Load(ResourceDictionary resources, string colorName)
        {
            var dictionaries = LoadDictionaries(colorName);

            foreach (var dictionary in dictionaries)
            {
                resources.MergedDictionaries.Add(dictionary);
            }
        }
    }
}
