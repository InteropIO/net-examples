using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using DOT.Core.Extensions;
using DOT.Core.Isolation.System.Threading;
using DOT.Core.Native;
using Tick42;
using Tick42.StartingContext;
using Image = System.Drawing.Image;

namespace io_connect_anything
{
    /// <summary>
    ///     Demonstrates glueifying of 3rd party applications - in this case Notepad and Paint.
    ///     The applications are launched with a channel support and controlled by the host app, they can be saved, restored.
    ///     The interprocess communication to the apps is done via window messages and windows clipboard.
    /// </summary>
    internal class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className,
            string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, StringBuilder lParam);

        // for clipboard to work, we have to be in STA thread
        [STAThread]
        static void Main(string[] args)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke((Action)MainThread);
            Dispatcher.Run();
        }

        /// <summary>
        ///     Renders text to image
        /// </summary>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="textColor"></param>
        /// <param name="backColor"></param>
        /// <returns></returns>
        public static Image DrawText(string text, Font font, Color textColor, Color backColor)
        {
            if (text == null)
            {
                return null;
            }

            using (Bitmap dummyBitmap = new Bitmap(1, 1))
            {
                using (Graphics dummyGraphics = Graphics.FromImage(dummyBitmap))
                {
                    SizeF textSize = dummyGraphics.MeasureString(text, font);

                    Bitmap bitmap = new Bitmap((int)textSize.Width, (int)textSize.Height);
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.Clear(backColor);

                        using (Brush textBrush = new SolidBrush(textColor))
                        {
                            graphics.DrawString(text, font, textBrush, 0, 0);
                        }
                    }

                    return bitmap;
                }
            }
        }

        /// <summary>
        ///     Simple routine to draw text on image and paste it to a window given by its handle
        /// </summary>
        /// <param name="text"></param>
        /// <param name="windowHandle"></param>
        public static void DrawTextAndPaste(string text, IntPtr windowHandle)
        {
            var img = DrawText(text, new Font("Arial", 16), Color.Black, Color.White);
            if (img == null)
            {
                return;
            }

            Clipboard.SetImage(img);
            Win32.SetForegroundWindow(windowHandle);
            Thread.Sleep(100);
            SendKeys.SendWait("^v");
        }

        private static async void MainThread()
        {
            var disp = Dispatcher.CurrentDispatcher;
            var glue = await Glue42.InitializeGlue(new InitializeOptions
            {
                AppDefinition = new AppDefinition
                {
                    // we're just launcher for other apps, so we don't need to be saved and restored
                    // otherwise we will be closed upon restoration
                    // if this is desired - put this to false
                    IgnoreFromLayouts = true,
                    // the factory app can be marked as singleton, to make sure there's only one instance
                    // this is up to the developer
                    AllowMultiple = false
                }
            });
            Console.WriteLine(":::glue ready");

            await glue.AppManager.RegisterAppFactoryAsync<object, LambdaApp<string>, string, object>(app => app
                    .WithFolder("Anything")
                    .WithAllowMultiple(true)
                    .WithName("Notepad")
                    .WithChannelSupport(true)
                    .WithDispatcher(disp),
                async (context, builder, __) =>
                {
                    Console.WriteLine(":::launching notepad");
                    Process notepad = Process.Start("notepad.exe");
                    notepad.WaitForInputIdle();

                    IntPtr notepadHandle = notepad.MainWindowHandle;
                    IntPtr editHandle = FindWindowEx(notepadHandle, IntPtr.Zero, "Edit", null);

                    return new LambdaApp<string>(notepadHandle)
                    {
                        Shutdown = () => notepad.Kill(),
                        Init = state => Win32.SendMessage(editHandle, Win32.WM_SETTEXT, IntPtr.Zero, state),
                        ChannelChanged = (channelContext, channel, arg3) =>
                        {
                            // or read from channelContext.GetValue<string>("partyPortfolio.ric");
                            Win32.SendMessage(editHandle, Win32.WM_SETTEXT, IntPtr.Zero, channel?.Name);
                        },
                        ChannelUpdate = (channelContext, channel, arg3) =>
                        {
                            var ric = channelContext.GetValue<string>("partyPortfolio.ric");
                            Win32.SendMessage(editHandle, Win32.WM_SETTEXT, IntPtr.Zero, ric);
                        },
                        GetState = () =>
                        {
                            int length = (int)Win32.SendMessage(editHandle, Win32.WM_GETTEXTLENGTH, IntPtr.Zero,
                                IntPtr.Zero);
                            StringBuilder sb = new StringBuilder(length + 1);

                            SendMessage(editHandle, Win32.WM_GETTEXT, (IntPtr)sb.Capacity, sb);
                            return sb.ToString().AsCompletedTask();
                        }
                    };
                });

            await glue.AppManager.RegisterAppFactoryAsync<object, LambdaApp<string>, string, object>(app => app
                    .WithFolder("Anything")
                    .WithAllowMultiple(true)
                    .WithName("Paint")
                    .WithChannelSupport(true)
                    .WithDispatcher(disp),
                async (context, builder, __) =>
                {
                    Console.WriteLine(":::launching paint");
                    Process paint = Process.Start("mspaint.exe");
                    paint.WaitForInputIdle();

                    // wait a bit for main window to be ready
                    await Task.Delay(500);
                    IntPtr paintHandle = paint.MainWindowHandle;

                    return new LambdaApp<string>(paintHandle)
                    {
                        Shutdown = () => paint.Kill(),
                        Init = state => DrawTextAndPaste("Loaded: " + state, paintHandle),
                        ChannelChanged =
                            (channelContext, channel, arg3) => DrawTextAndPaste(channel?.Name, paintHandle),
                        ChannelUpdate = (channelContext, channel, arg3) =>
                        {
                            var ric = channelContext.GetValue<string>("partyPortfolio.ric");
                            DrawTextAndPaste(ric, paintHandle);
                        },
                        GetState = () => Guid.NewGuid().ToString("N").AsCompletedTask()
                    };
                });

            Console.WriteLine(":::io-connect-anything ready");
        }
    }
}