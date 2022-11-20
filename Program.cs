﻿using System;
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
		static uint GHND = 0x0042;
		
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
			SetCursorPos(0, 0);
			
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
			// var watch = new System.Diagnostics.Stopwatch();
			// watch.Start();
			
			// int[,] gameState = new int[ HEIGHT, WIDTH ];
			// getGameState(location, gameState);
			
			// watch.Stop();
			
			// print(gameState);
			// Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");
			
			int elementSize = Marshal.SizeOf(typeof(IntPtr));
			IntPtr unmanagedArray = Marshal.AllocHGlobal(10 * elementSize);
			
			for(int i=0; i<10; i++)
				Console.WriteLine(Marshal.ReadIntPtr(unmanagedArray, i*elementSize));

			for(int i=0; i<10; i++)
				Marshal.WriteIntPtr(unmanagedArray, i*elementSize, ((IntPtr)(i+1)));
			
			for(int i=0; i<10; i++)
				Console.WriteLine(Marshal.ReadIntPtr(unmanagedArray, i*elementSize));
			
			Marshal.FreeHGlobal(unmanagedArray);

			unsafe {
				int w = location.Right - location.Left;
				int h = location.Bottom - location.Top;
				IntPtr hdcScreen = GetDesktopWindow();
				IntPtr hdcWindow = GetWindowDC(hdcScreen);
				IntPtr hdcMemDC = CreateCompatibleDC(hdcWindow);
				Rect rcClient = location;
				IntPtr hbmScreen = CreateCompatibleBitmap(hdcWindow, rcClient.Right-rcClient.Left, rcClient.Bottom-rcClient.Top);


				SelectObject(hdcMemDC, hbmScreen);
				
				bool result = BitBlt(
					// to
					hdcMemDC, 0, 0, rcClient.Right-rcClient.Left, rcClient.Bottom-rcClient.Top,
					// from
					hdcWindow, location.Left, location.Top,
					// command
					TernaryRasterOperations.SRCCOPY
				);
				Console.WriteLine(result.ToString());

				BITMAP bmpScreen = new BITMAP();
				GCHandle hndl = GCHandle.Alloc(bmpScreen, GCHandleType.Pinned);
				IntPtr ptrToBitmap = hndl.AddrOfPinnedObject();
				int result2 = GetObject(hbmScreen, Marshal.SizeOf<BITMAP>(), ptrToBitmap);
				bmpScreen = Marshal.PtrToStructure<BITMAP>(ptrToBitmap);
				hndl.Free();
				
				Console.WriteLine($"GetObject: {result2}");

				BITMAPINFOHEADER bi = new BITMAPINFOHEADER();
				bi.biSize = (uint) sizeof(BITMAPINFOHEADER);
				bi.biWidth = bmpScreen.bmWidth;
				bi.biHeight = bmpScreen.bmHeight;
				bi.biPlanes = 1;
				bi.biBitCount = 32;
				bi.biCompression = BitmapCompressionMode.BI_RGB;
				bi.biSizeImage = 0;
				bi.biClrUsed = 0;
				bi.biClrImportant = 0;
				
				int dwBmpSize = ((bmpScreen.bmWidth * bi.biBitCount + 31) / 32) * 4 * bmpScreen.bmHeight;
				// IntPtr hDIB = GlobalAlloc(GHND, dwBmpSize);
				byte[] lpbitmap = new byte[dwBmpSize];
				
				BITMAPINFO bmi = new BITMAPINFO();
				bmi.bmiHeader = bi;

				int result3 = GetDIBits(
					hdcWindow, hbmScreen, 0, 
					(uint) h,
					lpbitmap, 
					ref bmi, DIB_Color_Mode.DIB_RGB_COLORS
				);
				
				Console.WriteLine($"GetDIBits: {result3}");
				
				Color c = GetColorAt(hdcWindow, location.Left, location.Top);
				Console.WriteLine(c.ToString());
				
				int _row = 200;
				int i=0;
				while(i<w*4) {
					Console.WriteLine($"{lpbitmap[_row*w*4+i]} {lpbitmap[_row*w*4+i+1]} {lpbitmap[_row*w*4+i+2]} {lpbitmap[_row*w*4+i+3]}");
					i+=4;
				}
				i-=4;
				// Console.WriteLine($"{lpbitmap[i]} {lpbitmap[i+1]} {lpbitmap[i+2]}");
				Console.WriteLine(i/4);
				
				//clean
				DeleteObject(hbmScreen);
				DeleteObject(hdcMemDC);
				ReleaseDC(hdcWindow, hdcScreen);
				
			}
			
			
			
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

		enum TernaryRasterOperations : uint {
				/// <summary>dest = source</summary>
				SRCCOPY = 0x00CC0020,
				/// <summary>dest = source OR dest</summary>
				SRCPAINT = 0x00EE0086,
				/// <summary>dest = source AND dest</summary>
				SRCAND = 0x008800C6,
				/// <summary>dest = source XOR dest</summary>
				SRCINVERT = 0x00660046,
				/// <summary>dest = source AND (NOT dest)</summary>
				SRCERASE = 0x00440328,
				/// <summary>dest = (NOT source)</summary>
				NOTSRCCOPY = 0x00330008,
				/// <summary>dest = (NOT src) AND (NOT dest)</summary>
				NOTSRCERASE = 0x001100A6,
				/// <summary>dest = (source AND pattern)</summary>
				MERGECOPY = 0x00C000CA,
				/// <summary>dest = (NOT source) OR dest</summary>
				MERGEPAINT = 0x00BB0226,
				/// <summary>dest = pattern</summary>
				PATCOPY    = 0x00F00021,
				/// <summary>dest = DPSnoo</summary>
				PATPAINT = 0x00FB0A09,
				/// <summary>dest = pattern XOR dest</summary>
				PATINVERT = 0x005A0049,
				/// <summary>dest = (NOT dest)</summary>
				DSTINVERT = 0x00550009,
				/// <summary>dest = BLACK</summary>
				BLACKNESS = 0x00000042,
				/// <summary>dest = WHITE</summary>
				WHITENESS = 0x00FF0062,
				/// <summary>
				/// Capture window as seen on screen.  This includes layered windows
				/// such as WPF windows with AllowsTransparency="true"
				/// </summary>
				CAPTUREBLT = 0x40000000
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct Bitmap {
				 /// <summary>
				 /// The bitmap type. This member must be zero.
				 /// </summary>
				 public int bmType;

				 /// <summary>
				 /// The width, in pixels, of the bitmap. The width must be greater than zero.
				 /// </summary>
				 public int bmWidth;

				 /// <summary>
				 /// The height, in pixels, of the bitmap. The height must be greater than zero.
				 /// </summary>
				 public int bmHeight;

				 /// <summary>
				 /// The number of bytes in each scan line. This value must be divisible by 2, because the system assumes that the bit
				 /// values of a bitmap form an array that is word aligned.
				 /// </summary>
				 public int bmWidthBytes;

				 /// <summary>
				 /// The count of color planes.
				 /// </summary>
				 public int bmPlanes;

				 /// <summary>
				 /// The number of bits required to indicate the color of a pixel.
				 /// </summary>
				 public int bmBitsPixel;

				 /// <summary>
				 /// A pointer to the location of the bit values for the bitmap. The bmBits member must be a pointer to an array of
				 /// character (1-byte) values.
				 /// </summary>
				 public IntPtr bmBits;
		}

		[StructLayoutAttribute( LayoutKind.Sequential )]
		struct BITMAPINFO {
			 /// <summary>
			 /// A BITMAPINFOHEADER structure that contains information about the dimensions of color format.
			 /// </summary>
			 public BITMAPINFOHEADER bmiHeader;

			 /// <summary>
			 /// An array of RGBQUAD. The elements of the array that make up the color table.
			 /// </summary>
			 [MarshalAsAttribute( UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.Struct )]
			 public RGBQUAD[] bmiColors;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct BITMAPINFOHEADER {
				public uint  biSize;
				public int   biWidth;
				public int   biHeight;
				public ushort   biPlanes;
				public ushort   biBitCount;
				public BitmapCompressionMode  biCompression;
				public uint  biSizeImage;
				public int   biXPelsPerMeter;
				public int   biYPelsPerMeter;
				public uint  biClrUsed;
				public uint  biClrImportant;

				public void Init() {
						biSize = (uint)Marshal.SizeOf(this);
				}
		}

		enum BitmapCompressionMode : uint {
			 BI_RGB = 0,
			 BI_RLE8 = 1,
			 BI_RLE4 = 2,
			 BI_BITFIELDS = 3,
			 BI_JPEG = 4,
			 BI_PNG = 5
		}

		[StructLayout(LayoutKind.Sequential, Pack=1)]
		public struct RGBQUAD {
				public byte rgbBlue;
				public byte rgbGreen;
				public byte rgbRed;
				public byte rgbReserved;
		}

		enum DIB_Color_Mode : uint {
			 DIB_RGB_COLORS = 0,
			 DIB_PAL_COLORS = 1
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct BITMAP {
				 /// <summary>
				 /// The bitmap type. This member must be zero.
				 /// </summary>
				 public int bmType;

				 /// <summary>
				 /// The width, in pixels, of the bitmap. The width must be greater than zero.
				 /// </summary>
				 public int bmWidth;

				 /// <summary>
				 /// The height, in pixels, of the bitmap. The height must be greater than zero.
				 /// </summary>
				 public int bmHeight;

				 /// <summary>
				 /// The number of bytes in each scan line. This value must be divisible by 2, because the system assumes that the bit
				 /// values of a bitmap form an array that is word aligned.
				 /// </summary>
				 public int bmWidthBytes;

				 /// <summary>
				 /// The count of color planes.
				 /// </summary>
				 public int bmPlanes;

				 /// <summary>
				 /// The number of bits required to indicate the color of a pixel.
				 /// </summary>
				 public int bmBitsPixel;

				 /// <summary>
				 /// A pointer to the location of the bit values for the bitmap. The bmBits member must be a pointer to an array of
				 /// character (1-byte) values.
				 /// </summary>
				 public IntPtr bmBits;
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
		
		[DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);
		
		[DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError=true)]
		static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);
		[DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
		static extern IntPtr CreateCompatibleBitmap([In] IntPtr hdc, int nWidth, int nHeight);
		[DllImport("gdi32.dll", EntryPoint = "SelectObject")]
		public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);
		[DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteObject([In] IntPtr hObject);
		[DllImport("gdi32.dll", EntryPoint = "GetDIBits")]
		static extern int GetDIBits([In] IntPtr hdc, [In] IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BITMAPINFO lpbi, DIB_Color_Mode uUsage);
		[DllImport("gdi32.dll")]
		static extern int GetObject(IntPtr hgdiobj, int cbBuffer, IntPtr lpvObject);
		
		[DllImport("kernel32.dll")]
		static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
	}	
}
