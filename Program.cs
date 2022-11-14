using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace Minesweeper_Solver {
	class Program {
		static int FIRST_TOP = 90;
		static int FIRST_LEFT = 50;
		static int WIDTH = 9;
		static int HEIGHT = 9;
		static int SPACING = 18;
		static string processName = "Minesweeper";
		static string processLocation = "C:\\Windows\\winsxs\\amd64_microsoft-windows-s..oxgames-minesweeper_31bf3856ad364e35_6.1.7600.16385_none_fe560f0352e04f48\\";
		static int DELAY = 1500;
		
		static void Main(string[] args) {
			// open minesweeper
			Process p = openProcess(processName, processLocation);
			p.WaitForInputIdle();
			
			// get the window's location
			Rect location = new Rect();
			GetWindowRect(p.MainWindowHandle, ref location);
			SetCursorPos(location.Left + FIRST_LEFT, location.Top + FIRST_TOP);
			Thread.Sleep(500);
			SetCursorPos(
				location.Left + FIRST_LEFT + (WIDTH-1)*SPACING, 
				location.Top  + FIRST_TOP  + (HEIGHT-1)*SPACING
			);
			Thread.Sleep(500);
			
			// click a random square
			Random rnd = new Random();
			int x = rnd.Next(WIDTH);
			int y = rnd.Next(HEIGHT);
			click(location, x, y);
			
			// convert to matrix
			// we need a 2D array. one row for each square, and one column for each square plus another for the data in each square
			byte[,] matrix = new byte[ (WIDTH*HEIGHT), (WIDTH*HEIGHT+1) ];
			// print(matrix);
			
			// get color of the pixels
			Color color = GetColorAt(location.Left + FIRST_LEFT, location.Top + FIRST_TOP);
			Console.WriteLine(color.ToString());
			
			// find image inside image
			
			
			
			
			// reduce matrix
			
			
			// click buttons
			
			
			// repeat
			
			
			
		}
		
		// OBJECTS
		public struct Rect {
			public int Left { get; set; }
			public int Top { get; set; }
			public int Right { get; set; }
			public int Bottom { get; set; }
		}

		// METHODS
		static Process openProcess(string name, string location) {
			int SW_SHOWNORMAL = 1;

			Process p = null;
			
			// Check whether {name} is already open
			Console.WriteLine($"Check whether {name} is already open...");
			if (Process.GetProcessesByName(name).Length > 0) {
				Console.WriteLine($"{name} is running -> Bring it to the front.");
				
				p = Process.GetProcessesByName(name)[0];
				
				// Bring up the window if it's minimized
				ShowWindow(p.MainWindowHandle, SW_SHOWNORMAL);
				// Bring it to the front
				SetForegroundWindow(p.MainWindowHandle.ToInt32());
				
			} else {
				Console.WriteLine($"{name} is NOT running. -> Launch it.");
				
				// if it's not, open it
				p = Process.Start(location + name);		

				// wait for opening animation
				Thread.Sleep(DELAY);
			}
			
			return p;
		}	
		
		static void click(Rect location, int x, int y) {
			int LEFTDOWN = 0x00000002;
			int LEFTUP =   0x00000004;
			SetCursorPos(
				location.Left + FIRST_LEFT + SPACING * x, 
				location.Top  + FIRST_TOP  + SPACING * y
			);
			mouse_event(LEFTDOWN, 0, 0, 0, 0);
			mouse_event(LEFTUP, 0, 0, 0, 0);
		}
		
		static void print(byte[,] matrix) {
			int rows = WIDTH*HEIGHT;
			int cols = WIDTH*HEIGHT+1;
			for(int i=0; i<rows; i++) {
				StringBuilder sb = new StringBuilder();
				sb.Append("[ ");
				for(int j=0; j<cols-1; j++) {
					sb.Append(matrix[i, j].ToString());
					sb.Append(' ');
				}
				sb.Append("| ");
				sb.Append(matrix[i, cols-1].ToString());
				sb.Append(']');
				Console.WriteLine(sb.ToString());
			}
		}

		public static Color GetColorAt(int x, int y)
		{
				IntPtr desk = GetDesktopWindow();
				IntPtr dc = GetWindowDC(desk);
				int a = (int) GetPixel(dc, x, y);
				ReleaseDC(desk, dc);
				return Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
		}
		
		// DLL IMPORTS
		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		
		[DllImport("User32.dll")]
		public static extern Int32 SetForegroundWindow(int hWnd);
		
		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);
		
		[DllImport("user32.dll")]
		static extern bool SetCursorPos(int x, int y);
		
		[DllImport("user32.dll")]
		public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
		
		[DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
		public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
		
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetDesktopWindow();
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetWindowDC(IntPtr window);
		[DllImport("gdi32.dll", SetLastError = true)]
		public static extern uint GetPixel(IntPtr dc, int x, int y);
		[DllImport("user32.dll", SetLastError = true)]
		public static extern int ReleaseDC(IntPtr window, IntPtr dc);
	}	
}
