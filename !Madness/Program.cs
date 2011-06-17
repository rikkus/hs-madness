using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Madness
{
    public class MainForm : Form
    {
        [STAThread]
        public static void Main()
        {
            Application.Run(new MainForm());
        }

        private readonly NotifyIcon trayIcon;
        private readonly ContextMenu trayMenu;
        private readonly Timer timer;

        private readonly Dictionary<IntPtr, Size> velocities = new Dictionary<IntPtr, Size>();
        private readonly Dictionary<IntPtr, Point> expectedPositions = new Dictionary<IntPtr, Point>();
        private readonly Dictionary<IntPtr, Point> originalPositions = new Dictionary<IntPtr, Point>();
        private const int VelocityPixels = 1;

        public MainForm()
        {
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);

            var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Madness.madness.ico");

            Debug.Assert(iconStream != null);

            trayIcon = new NotifyIcon
                           {
                               Text = @"!Madness",
                               Icon = new Icon(iconStream),
                               ContextMenu = trayMenu,
                               Visible = true
                           };

            timer = new Timer {Interval = 30};
            timer.Tick += (sender, e) => GetMad();
            timer.Start();
        }

        private void GetMad()
        {
            User32.EnumDesktopWindows(User32.CurrentDesktop, Madden, IntPtr.Zero /* Nothing to say */);
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = ShowInTaskbar = false;

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            RestoreOriginalPositions();
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }

        private void RestoreOriginalPositions()
        {
            User32.EnumDesktopWindows(User32.CurrentDesktop, RestoreWindowPosition, IntPtr.Zero /* Nothing to say */);
        }

        private bool RestoreWindowPosition(IntPtr hWnd, int lParam)
        {        
            if (originalPositions.ContainsKey(hWnd))
            {
                Reposition(hWnd, originalPositions[hWnd]);
            }

            return true;
        }

        private static void Reposition(IntPtr hWnd, Point point)
        {
            User32.SetWindowPos
            (
                hWnd,
                IntPtr.Zero,
                point.X,
                point.Y,
                -1,
                -1,
                SetWindowPosFlags.SWP_NOACTIVATE
                | SetWindowPosFlags.SWP_NOOWNERZORDER
                | SetWindowPosFlags.SWP_NOSIZE
                | SetWindowPosFlags.SWP_NOZORDER
            );
        }

        private bool Madden(IntPtr hWnd, int lParam)
        {
            if (BadWindow(hWnd))
                return true;

            RECT win32Rect;

            if (User32.GetWindowRect(hWnd, out win32Rect))
            {
                var windowRect = Win32RectToRectangle(win32Rect);

                if (!TooBig(windowRect))
                    Madden(hWnd, windowRect);
            }

            return true;
        }

        private static bool TooBig(Rectangle windowRect)
        {
            var workingArea = Screen.GetWorkingArea(windowRect);
            return windowRect.Width >= workingArea.Width || windowRect.Height >= workingArea.Height;
        }

        private static Rectangle Win32RectToRectangle(RECT win32Rect)
        {
            return new Rectangle
                (
                new Point(win32Rect.Left, win32Rect.Top),
                new Size(
                    win32Rect.Right - win32Rect.Left,
                    win32Rect.Bottom - win32Rect.Top
                    )
                );
        }

        private void Madden(IntPtr hWnd, Rectangle windowRect)
        {       
            if (!originalPositions.ContainsKey(hWnd))
            {
                originalPositions[hWnd] = windowRect.Location;
                velocities[hWnd] = new Size(Coin.Toss() ? VelocityPixels : -VelocityPixels, Coin.Toss() ? VelocityPixels : -VelocityPixels);
            }
            else
            {
                if (expectedPositions[hWnd] != windowRect.Location)
                {
                    originalPositions[hWnd] = windowRect.Location;
                }
            }

            PushWindowInWorkingArea(hWnd, windowRect, Screen.GetWorkingArea(windowRect));
        }

        private void PushWindowInWorkingArea(IntPtr hWnd, Rectangle windowRect, Rectangle workingArea)
        {
            var velocity = BumpEdges(windowRect, velocities[hWnd], workingArea);

            var newPosition = new Point(windowRect.Location.X + velocity.Width,
                                        windowRect.Location.Y + velocity.Height);

            expectedPositions[hWnd] = newPosition;

            velocities[hWnd] = velocity;

            Reposition(hWnd, newPosition);
        }

        private static Size BumpEdges(Rectangle rect, Size velocity, Rectangle workingArea)
        {
            var x = rect.Left;
            var y = rect.Top;

            var dx = velocity.Width;
            var dy = velocity.Height;

            if (dx < 0 && (x - dx < workingArea.Left))
                dx = -dx;

            else if (dx > 0 && (x + dx + rect.Width > workingArea.Right))
                dx = -dx;

            if (dy < 0 && (y - dy < workingArea.Top))
                dy = -dy;

            else if (dy > 0 && (y + dy + rect.Height > workingArea.Bottom))
                dy = -dy;

            return new Size(dx, dy);
        }

        private static bool BadWindow(IntPtr hWnd)
        {
            var windowStyles = User32.GetWindowLong(hWnd, (int)GWL.STYLE);

            var visible = IsFlagSet((WindowStyles)windowStyles, WindowStyles.WS_VISIBLE);
            var app = IsFlagSet((WindowStylesEx)windowStyles, WindowStylesEx.WS_EX_APPWINDOW);

            return !(visible && app);
        }

        private static bool IsFlagSet<T>(T value, T flag) where T : struct
        {
            return (Convert.ToInt64(value) & Convert.ToInt64(flag)) != 0;
        }
    }
}
