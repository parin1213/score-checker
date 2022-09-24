using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SkiaSharp;
using System.Linq;
using System.Numerics;
using Google.Cloud.Vision.V1;
using System.ComponentModel;

namespace genshin_relic.Extentions
{
    public static class MyExtentions
    {
        public static void AddRange<T>(this BindingList<T> list, IEnumerable<T> add)
        {
            foreach (var item in add)
            {
                list.Add(item);
            }
        }
    }
}
