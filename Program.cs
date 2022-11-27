using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;

using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;

namespace Minesweeper_Solver {
	class Program {
		static int FIRST_TOP = 90;
		static int FIRST_LEFT = 45;
		// static int WIDTH = 30;
		// static int HEIGHT = 16;
		static int WIDTH = 9;
		static int HEIGHT = 9;
		static int SPACING = 20;
		static sbyte MINES = 10;
		// static string processName = "MinesweeperClassic.exe";
		// static string processLocation = "C:\\Program Files\\WindowsApps\\61424ShailendraSinghSoftw.44386E29E9F0D_1.0.0.0_x64__wr4tvb9qd6vv4\\MinesweeperClassic";
		static int DELAY = 1500;
		static IWebElement canvas;
		static ChromeDriver driver;
		static int x0;
		static int y0;
		static int XOFF = 13;
		static int YOFF = 10;
		
		static void Main(string[] args) {
			// setup chrome driver
			new DriverManager().SetUpDriver(new ChromeConfig());
			driver = new ChromeDriver(@".\\tools\\chromedriver.exe");
			
			// navigate to web page
      driver.Navigate().GoToUrl(Path.GetFullPath(@".\\p5-minesweeper\\index.html"));
			
			// find the canvas
			canvas = driver.FindElement(By.Id("defaultCanvas0"));
			Console.WriteLine(canvas.Size.Width);
			
			// setup offsets
			int canvasWidth = canvas.Size.Width;
			int canvasHeight = canvas.Size.Height;
			
			int canvasCenterX = canvasWidth / 2;
			int canvasCenterY = canvasHeight / 2;
			
			x0 = -canvasCenterX;
			y0 = -canvasCenterY;
			
			int canvasX = canvas.Location.X;
			int canvasY = canvas.Location.Y;
			
			// first click
			initialClicks();
			
			// take screenshot
				
			string fileName = DateTime.Now.ToString("yyy-MM-dd HH-mm-ss") + ".png";
			
			byte[] byteArray = ((ITakesScreenshot)driver).GetScreenshot().AsByteArray;
			Bitmap screenshot = new System.Drawing.Bitmap(new System.IO.MemoryStream(byteArray));
			Rectangle crop = new Rectangle(canvasX, canvasY, canvasWidth, canvasHeight);
			screenshot = screenshot.Clone(crop, screenshot.PixelFormat);
			// screenshot.Save(String.Format(@"path" + fileName, System.Drawing.Imaging.ImageFormat.Png));
			Color c = screenshot.GetPixel(10, 10);
			Console.WriteLine(c.ToString());
			
			// get all the colors
			// for(int i=0; i<=9; i++) {
				// c = screenshot.GetPixel(13, i*20+10);
				// double hue, saturation, value;
				// ColorToHSV(c, out hue, out saturation, out value);
				// int guess = makeGuess(hue, saturation, value);
				// Console.WriteLine($"{i} [h={hue} s={saturation} v={value}] {guess}");
			// }
			
			// use the data
			int [,] gameState = new int[HEIGHT, WIDTH];
			getGameState(screenshot, gameState);
			print(gameState);
			
			// turn the game state into a matrix
			// convert to matrix
			// we need a 2D array. 
			// one row for each square, plus another for the number of mines
			// one column for each square, plus another for the data in each square
			sbyte[,] matrix = new sbyte[ WIDTH*HEIGHT+1, WIDTH*HEIGHT+1 ];
			buildMatrix(matrix, gameState);
			print(matrix);
				
			// reduce matrix
			reduce(matrix);
			print(matrix);
			
			return;
			
			
			
			// click buttons
			
			
			// repeat
			
			
			
		}
		
		// METHODS
		static void initialClicks() {
			// click the center
			canvas.Click();
			
			// these are for testing
/*			
			// click the top left
			Actions actions = new Actions(driver);
			actions.MoveToElement(canvas, x0, y0).Click().Build().Perform();
			
			// click the bottom right
			actions.MoveToElement(canvas, x0+(WIDTH-1)*SPACING, y0+(HEIGHT-1)*SPACING).Click().Build().Perform();
			
			// click the bottom left and top right in one goto
			actions.MoveToElement(canvas, x0+(WIDTH-1)*SPACING, y0).Click()
				.MoveToElement(canvas, x0, y0+(HEIGHT-1)*SPACING).Click()
				.Build().Perform();
*/
		}
		
		static void print(sbyte[,] matrix) {
			int rows = WIDTH*HEIGHT+1;
			int cols = WIDTH*HEIGHT+1;
			for(int i=0; i<rows; i++) {
				StringBuilder sb = new StringBuilder();
				sb.Append("[ ");
				for(int j=0; j<cols-1; j++) {
					sb.Append(matrix[i, j].ToString());
					// sb.Append(' ');
				}
				sb.Append(" | ");
				sb.Append(matrix[i, cols-1].ToString());
				sb.Append(" ]");
				Console.WriteLine(sb.ToString());
			}
			Console.WriteLine();
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
				sb.Append(" ]");
				Console.WriteLine(sb.ToString());
			}
		}

		static void getGameState(Bitmap map, int[,] gameState) {
			for (int i=0; i<HEIGHT; i++) {
				for (int j=0; j<WIDTH; j++) {
					gameState[i, j] = getValueAt(map, i, j);
				}
			}
		}

		static int getValueAt(Bitmap map, int row, int col) {
			int pixelX = col*SPACING + XOFF;
			int pixelY = row*SPACING + YOFF;
			Color c = map.GetPixel(pixelX, pixelY);
			double hue, saturation, value;
			ColorToHSV(c, out hue, out saturation, out value);
			return makeGuess(hue, saturation, value);
		}
		
		static void ColorToHSV(Color color, out double hue, out double saturation, out double value)	{
			int max = Math.Max(color.R, Math.Max(color.G, color.B));
			int min = Math.Min(color.R, Math.Min(color.G, color.B));

			hue = color.GetHue();
			saturation = (max == 0) ? 0 : 1d - (1d * min / max);
			value = max / 255d;
		}
		
		static int makeGuess(double h, double s, double v) {
			if (h == 0 && s == 0 && v < .95) 
				return 0;
			if (h > 220 && h < 230)
				return 1;
			if (h > 265 && h < 275)
				return 2;
			if (h > 310 && h < 320)
				return 3;
			if (h == 0 && s > .50)
				return 4;
			if (h > 40 && h < 50)
				return 5;
			if (h > 85 && h < 95)
				return 6;
			if (h > 130 && h < 141)
				return 7;
			if (h > 175 && h < 185)
				return 8;
			if (h == 0 && s == 0 && v == 1)
				return -1;
			return -2;
		}
		
 		static void buildMatrix(sbyte[,] matrix, int[,] gameState) {
			int len = WIDTH*HEIGHT;
			
			// walk the matrix rows
			for(int mRow=0; mRow<len; mRow++) {
				// which game element am i talking about?
				int gRow = mRow / WIDTH;
				int gCol = mRow % WIDTH;
				// Console.WriteLine($"gRow={gRow} gCol={gCol}");
				
				// if the square is unclicked, skip
				if (gameState[gRow, gCol] == -1 || gameState[gRow, gCol] == 0)
					continue;
				// the value of the square goes into the last column
				matrix[mRow, len] = (sbyte) gameState[gRow, gCol];

				// get game element's neighbors
				for(int i=-1; i<=1; i++) {
					if (gRow+i < 0 || gRow+i == HEIGHT) continue;
					for(int j=-1; j<=1; j++) {
						if (i == 0 && j == 0) continue;
						if (gCol+j < 0 || gCol+j == WIDTH) continue;
						
						// Console.WriteLine($"r={gRow+i} c={gCol+j} e={(gRow+i)*WIDTH + gCol+j}");
						// only set the matrix element to 1 if it's unclicked
						if (gameState[gRow+i, gCol+j] == -1)
							matrix[mRow, (gRow+i)*WIDTH + gCol+j] = 1;
					}
				}
			}
			
			// the equation for the total number of mines
			for(int mCol=0; mCol<len; mCol++) {
				int gRow = mCol / WIDTH;
				int gCol = mCol % WIDTH;
				if (gameState[gRow, gCol] == -1)
					matrix[len, mCol] = 1;
			}
			matrix[len, len] = MINES;
		}
		
		static void reduce(sbyte[,] matrix) {
			int len = WIDTH*HEIGHT+1;
			// get first element to 1
			int col = 0;
			int row = 0;
			for(row=0; row<len && col<len; row++, col++) {
				if (matrix[row, col] == 0) {
					findAndSwap(matrix, row, col);
				}
				// if it's still zero, move to next row
				if (matrix[row, col] == 0) {
					row--;
					continue;
				}
				
				// get first column to 0
				zeroColumn(matrix, row, col);
			}
			// Console.WriteLine($"{row} {col}");
			// get last column to 0
			for(row--, col-=2; row>=0 && col>=0; row--, col--) {
				if (matrix[row, col] != 1) {
					row++;
					continue;
				}
				
				zeroColumnUpward(matrix, row, col);
				
				
			}
		}
		
		static void findAndSwap(sbyte[,] matrix, int row, int col) {
			int len = WIDTH*HEIGHT+1;
			for(int i=row+1; i<len; i++) {
				if (matrix[i, col] != 0) {
					swap(matrix, row, i);
					return;
				}
			}
		}
		
		static void swap(sbyte[,] matrix, int a, int b) {
			int len = WIDTH*HEIGHT+1;
			sbyte temp;
			for(int j=0; j<len; j++) {
				temp = matrix[a, j];
				matrix[a, j] = matrix[b, j];
				matrix[b, j] = temp;
			}
		}
		
		static void zeroColumn(sbyte[,] matrix, int row, int col) {
			int len = WIDTH*HEIGHT+1;
			// for each row below the current row, get this column to zero
			for(int i=row+1; i<len; i++) {
				sbyte multiplier = matrix[i, col];
				for(int j=col; j<len; j++) {
					matrix[i, j] -= (sbyte) (matrix[row, j] * multiplier);
				}
			}
		}
		
		static void zeroColumnUpward(sbyte[,] matrix, int row, int col) {
			int len = WIDTH*HEIGHT+1;
			// for each row below the current row, get this column to zero
			for(int i=row-1; i>=0; i--) {
				sbyte multiplier = matrix[i, col];
				for(int j=col; j<len; j++) {
					matrix[i, j] -= (sbyte) (matrix[row, j] * multiplier);
				}
			}
		}
		
	}	
}
