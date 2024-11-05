using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FDC3ChannelsClientProfileDemo
{
    static class Utils
    {
        public static AppMode GetAppMode()
        {
            return (AppMode)int.Parse(GetArg("mode") ?? ((int)AppMode.FDC3).ToString());
        }

        public static bool IsGlueEnabled(AppMode mode)
        {
            return (int)mode >= (int)AppMode.Glue;
        }

        public static bool IsStickyWindowsEnabled(AppMode mode)
        {
            return (int)mode >= (int)AppMode.Sticky;
        }

        public static bool ForceDebug()
        {
            return GetFlag("debug");
        }

        public static bool ShouldLaunchPortfolioApp()
        {
            return GetFlag("launch-portfolio");
        }

        public static bool ShouldDefaultToDarkTheme()
        {
            return !GetFlag("light") && !GetFlag("light-theme");
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static ImageSource ToImageSource(this Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap =
                Imaging
                    .CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

            if (!DeleteObject(hBitmap))
            {
                throw new Win32Exception();
            }

            return wpfBitmap;
        }

        public static string GetArg(string v, string ifEmpty = null)
        {
            string result =
                Environment
                    .GetCommandLineArgs()
                    .FirstOrDefault(a =>
                        a.Trim()
                            .ToLower()
                            .StartsWith("--" + v.ToLower() + "="));
            var toReturn = result?.Substring(3 + v.Length);
            if (string.IsNullOrWhiteSpace(toReturn))
            {
                return ifEmpty;
            }

            return toReturn;
        }

        public static bool GetFlag(string v)
        {
            bool result =
                Environment
                    .GetCommandLineArgs()
                    .Any(a =>
                        a.Trim()
                            .ToLower() == "--" + v.TrimStart('-').ToLower());
            return result;
        }

        /// <summary>
        ///     Finds a Child of a given item in the visual tree.
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>
        ///     The first parent item that matches the submitted type parameter.
        ///     If not matching item can be found,
        ///     a null parent is being returned.
        /// </returns>
        public static DependencyObject FindChild(this DependencyObject parent, string childName)
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            DependencyObject foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (!string.IsNullOrEmpty(childName))
                {
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        foundChild = child;
                        break;
                    }
                }

                foundChild = FindChild(child, childName);

                if (foundChild != null)
                {
                    break;
                }
            }

            return foundChild;
        }

        public static IEnumerable<DependencyObject> FindChildren(this DependencyObject parent, string childName)
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            var foundChildren = new List<DependencyObject>();

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (!string.IsNullOrEmpty(childName))
                {
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        foundChildren.Add(child);
                    }
                }
                else
                {
                    foundChildren.Add(child);
                }

                foundChildren.AddRange(FindChildren(child, childName));
            }

            return foundChildren;
        }
    }
}