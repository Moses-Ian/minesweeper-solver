using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Minesweeper_Solver {
	class Program {
		static int FIRST_TOP = 90;
		static int FIRST_LEFT = 50;
		static int LAST_TOP = 360;
		static int LAST_LEFT = 575;
		static int WIDTH = 30;
		static int HEIGHT = 16;
		static int SPACING = 18;
		
		static void Main(string[] args) {
			// open minesweeper
			string processName = "Minesweeper";
			string processLocation = "C:\\Windows\\winsxs\\amd64_microsoft-windows-s..oxgames-minesweeper_31bf3856ad364e35_6.1.7600.16385_none_fe560f0352e04f48\\";
			Process p = openProcess(processName, processLocation);
			Thread.Sleep(1000);
			
			// get the window's location
			Rect location = new Rect();
			GetWindowRect(p.MainWindowHandle, ref location);
			SetCursorPos(location.Left + FIRST_LEFT, location.Top + FIRST_TOP);
			SetCursorPos(location.Left + LAST_LEFT, location.Top + LAST_TOP);
			
			// click a random square
			Random rnd = new Random();
			int x = rnd.Next(30);
			int y = rnd.Next(16);
			click(location, x, y);
			
			// convert to matrix
			
			
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
		
	}	
}
