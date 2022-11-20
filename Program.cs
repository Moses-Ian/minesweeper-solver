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
		static int FIRST_LEFT = 45;
		static int WIDTH = 9;
		static int HEIGHT = 9;
		static int SPACING = 18;
		static string processName = "Minesweeper";
		static string processLocation = "C:\\Windows\\winsxs\\amd64_microsoft-windows-s..oxgames-minesweeper_31bf3856ad364e35_6.1.7600.16385_none_fe560f0352e04f48\\";
		static int DELAY = 1500;
		static int[,] PIXELS = {
			{1, 0},
			{1, 1},
			{3, 3},
			{3, 4},
			{4, 2},
			{4, 3},
			{4, 4}
		};
		
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
			SetCursorPos(location.Left, location.Top);
			
			//test
			// SetCursorPos(location.Left + FIRST_LEFT + SPACING, location.Top + FIRST_TOP);
			// return;
			
			// click a random square
			Random rnd = new Random();
			int row = rnd.Next(WIDTH);
			int col = rnd.Next(HEIGHT);
			// click(location, row, col);
			
			// convert to matrix
			// we need a 2D array. 
			// one row for each square, plus another for the number of mines
			// one column for each square, plus another for the data in each square
			byte[,] matrix = new byte[ (WIDTH*HEIGHT+1), (WIDTH*HEIGHT+1) ];
			// print(matrix);
			
			// get current game state
			var watch = new System.Diagnostics.Stopwatch();
			watch.Start();
			
			int[,] gameState = new int[ HEIGHT, WIDTH ];
			getGameState(location, gameState);
			
			watch.Stop();
			
			print(gameState);
			Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");
			
			
			
			
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
		
		static void click(Rect location, int row, int col) {
			int LEFTDOWN = 0x00000002;
			int LEFTUP =   0x00000004;
			SetCursorPos(
				location.Left + FIRST_LEFT + SPACING * col, 
				location.Top  + FIRST_TOP  + SPACING * row
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

		static void print(int[,] matrix) {
			int rows = matrix.GetLength(0);
			int cols = matrix.GetLength(1);
			for(int i=0; i<rows; i++) {
				StringBuilder sb = new StringBuilder();
				sb.Append("[ ");
				for(int j=0; j<cols; j++) {
					if (matrix[i, j] == -1) 
						sb.Append('X');
					else if (matrix[i, j] == 0)
						sb.Append(' ');
					else
						sb.Append(matrix[i, j].ToString());
				}
				sb.Append(']');
				Console.WriteLine(sb.ToString());
			}
		}

		static void getGameState(Rect location, int[,] gameState) {
			// lock the desktop
			IntPtr desk = GetDesktopWindow();
			IntPtr dc = GetWindowDC(desk);
			
			// analyze pixels
			for(int i=2; i<HEIGHT-1; i++) {
				for(int j=1; j<WIDTH-3; j++) {
					gameState[i, j] = getValueAt(dc, location, i, j);
				}
			}
			
			// release the desktop
			ReleaseDC(desk, dc);
		}

		static int getValueAt(IntPtr dc, Rect location, int row, int col) {
			Console.WriteLine($"Getting value: {row} {col}");
			int screenX = location.Left + FIRST_LEFT + SPACING*col;
			int screenY = location.Top + FIRST_TOP + SPACING*row;
			int guess = -1;	// -1 means unclicked
			for(int i=0; i<7; i++) {
				Color c = GetColorAt(dc, screenX+PIXELS[i, 0], screenY+PIXELS[i, 1]);
				double hue, saturation, value;
				ColorToHSV(c, out hue, out saturation, out value);
				guess = makeGuess(guess, hue, saturation , value);
				if (guess != -1 && guess != 0)
					return guess;
			}
			return guess;
		}

		static Color GetColorAt(IntPtr dc, int x, int y) {
			// x and y are screen pixel values
			int a = (int) GetPixel(dc, x, y);
			// int a = 0;		// just to check -> the GetPixel method is REALLY slow
			return Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
		}
		
		static void ColorToHSV(Color color, out double hue, out double saturation, out double value)	{
			int max = Math.Max(color.R, Math.Max(color.G, color.B));
			int min = Math.Min(color.R, Math.Min(color.G, color.B));

			hue = color.GetHue();
			saturation = (max == 0) ? 0 : 1d - (1d * min / max);
			value = max / 255d;
		}
		
 		static int makeGuess(int guess, double h, double s, double v) {
			// 3, 5, 7, and 8 are all red, but have slightly different hsv values
			
			// these numbers were determined by closely analyzing the colors as they appear in my game
			
			// guess empty
			if (s < .15)
				return 0;
			// guess 1
			if (h > 230 && h < 240 && s > .50 && v > .70)
				return 1;
			// guess 2
			if (h > 100 && h < 140) 
				return 2;
			// guess 3
			if (h > 330 && h < 350 && s < .30 && v > .50 && v < .60)
				return 3;
			// guess 4
			if (h > 230 && h < 240 && s > .50 && v < .70)
				return 4;
			// guess 5
			if (h > 340 && h < 350 && s > .40 && v <= .50)
				return 5;
			// guess 6
			if (h > 180 && h < 200)
				return 6;
			// guess 7
			if (h > 340 && h < 350 && s < .30 && v > .60)
				return 7;
			// guess 8
			if (h > 340 && h < 350 && s > .30 && s < .40 && v > .60)
				return 8;
			// if i think it's empty, return empty; if i think it's unclicked, return unclicked
			return guess;
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
