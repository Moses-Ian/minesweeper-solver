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
		// static sbyte MINES = 99;
		static int WIDTH = 9;
		static int HEIGHT = 9;
		static sbyte MINES = 10;
		static int SPACING = 20;
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
			
			// setup matrices
			int [,] gameState = new int[HEIGHT, WIDTH];
				// one row for each square, plus another for the number of mines
				// one column for each square, plus another for the data in each square
			sbyte[,] matrix = new sbyte[ WIDTH*HEIGHT+1, WIDTH*HEIGHT+1 ];
			int[,] safeSquares = new int[HEIGHT*WIDTH, 2];
			byte[] byteArray;
			Bitmap screenshot;
			Rectangle crop = new Rectangle(canvasX, canvasY, canvasWidth, canvasHeight);
			
			// first click
			initialClicks();
			
			// take screenshot
			while(true) {
			// for(int runs=1; runs<=2; runs++) {
				byteArray = ((ITakesScreenshot)driver).GetScreenshot().AsByteArray;
				screenshot = new System.Drawing.Bitmap(new System.IO.MemoryStream(byteArray));
				screenshot = screenshot.Clone(crop, screenshot.PixelFormat);
				
				// get all the colors
				// for(int i=0; i<=10; i++) {
					// Color c = screenshot.GetPixel(13, i*20+10);
					// double hue, saturation, value;
					// ColorToHSV(c, out hue, out saturation, out value);
					// int guess = makeGuess(hue, saturation, value);
					// Console.WriteLine($"{i} [h={hue} s={saturation} v={value}] {guess}");
				// }
				
				// use the data
				bool result = getGameState(screenshot, gameState);
				if (!result) {
					break;
				}
				// print(gameState);
				
				// turn the game state into a matrix
				// convert to matrix
				// we need a 2D array. 
				cleanMatrix(matrix);
				buildMatrix(matrix, gameState);
				// print(matrix);
					
				// reduce matrix
				reduce(matrix);
				// print(matrix);
				
				// generate list of safe squares
				// this is written for readability
				int count = findSafeSquares(safeSquares, matrix);
				if (count == 0)
					break;
				printSafeSquares(safeSquares, count);
				
				// click buttons
				clickSquares(safeSquares, count);
				// Thread.Sleep(5000);
			}
			
			print(matrix);
			Console.WriteLine("done");
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
				sb.Append("[");
				bool allZero = true;
				for(int j=0; j<cols-1; j++) {
					if (j % WIDTH == 0)
						sb.Append(' ');
					sb.Append(matrix[i, j].ToString());
					if (matrix[i, j] != 0)
						allZero = false;
				}
				sb.Append(" | ");
				sb.Append(matrix[i, cols-1].ToString());
				sb.Append(" ]");
				Console.WriteLine(sb.ToString());
				if (allZero) 
					break;
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

		static void printSafeSquares(int[,] matrix, int count) {
			Console.WriteLine($"Safe Squares: {count}");
			int rows = matrix.GetLength(0);
			int cols = matrix.GetLength(1);
			for(int i=0; i<count; i++) {
				StringBuilder sb = new StringBuilder();
				sb.Append("[ ");
				for(int j=0; j<cols; j++) {
					if (matrix[i, j] == -1) 
						sb.Append('X');
					else
						sb.Append(matrix[i, j].ToString());
					sb.Append(' ');
				}
				sb.Append("]");
				Console.WriteLine(sb.ToString());
			}
		}

		static void print(int[] array) {
			StringBuilder sb = new StringBuilder();
			sb.Append("[ ");
			for(int i=0; i<array.Length; i++) {
				sb.Append(array[i].ToString());
				sb.Append(' ');
			}
			sb.Append("]");
			Console.WriteLine(sb.ToString());
		}

		static void print(sbyte[] array) {
			StringBuilder sb = new StringBuilder();
			sb.Append("[ ");
			for(int i=0; i<array.Length; i++) {
				sb.Append(array[i].ToString());
				sb.Append(' ');
			}
			sb.Append("]");
			Console.WriteLine(sb.ToString());
		}

		static bool getGameState(Bitmap map, int[,] gameState) {
			for (int i=0; i<HEIGHT; i++) {
				for (int j=0; j<WIDTH; j++) {
					gameState[i, j] = getValueAt(map, i, j);
					if (gameState[i, j] == -2) {
						Console.WriteLine($"hit mine at {i}, {j}");
						return false;
					}
				}
			}
			return true;
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
			if (h == 0 && s == 0 && v > .80 && v < .95) 
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
			if (h == 0 && s == 0 && v < .80)
				return -2;
			return -3;
		}
		
		static void cleanMatrix(sbyte[,] matrix) {
			int len = WIDTH*HEIGHT+1;
			for(int i=0; i<len-1; i++) {
				for(int j=0; j<len-1; j++) {
					matrix[i, j] = 0;
				}
			}
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
				// if it's zero, find a row with a 1 in this column and swap it
				if (matrix[row, col] == 0) {
					findAndSwap(matrix, row, col);
				}
				// if it's still zero, move to next column, but stay on this row
				if (matrix[row, col] == 0) {
					row--;
					continue;
				}
				
				// for each row below this, get this column to zero
				zeroColumn(matrix, row, col);
			}
			
			// print(matrix);

			// now go back upwards
			for(row--; row>=0; row--) {
				// find the leftmost 1
				for(col=0; col<len-1; col++) {
					if (matrix[row, col] == 1) {
						// for each row above this one, get this column to zero
						zeroColumnUpward(matrix, row, col);
						break;
					}
				}
				
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
				// you have to apply the multiplier and subtraction to every element in the row
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
				// you have to apply the multiplier and subtraction to every element in the row
				for(int j=col; j<len; j++) {
					matrix[i, j] -= (sbyte) (matrix[row, j] * multiplier);
				}
			}
		}
		
		static int findSafeSquares(int[,] safeSquares, sbyte[,] matrix) {
			// uses a brute-force method to solve each equation
			// if there's only one solution, we can use it
			// if there's more, we skip and move on
			
			int len = WIDTH*HEIGHT+1;
			int count = 0;
			int safe = 0;
			// for each row, brute force to see if there's a solution
			for(int row=0; row<len; row++) {
				// set up an array of variables, to point to the elements
				count = 0;
				for(int col=0; col<len-1; col++) {
					if (matrix[row, col] != 0) {
						count++;
					}
				}
				int[] pointers = new int[count];
				
				// put the pointers in the array
				count = 0;
				for(int col=0; col<len-1; col++) {
					if (matrix[row, col] != 0) {
						pointers[count] = col;
						count++;
					}
				}
				
				// if the row is all zeros, get out of here
				if (count == 0)
					break;
				
				// create a solution array
				sbyte[] solution = new sbyte[count+1];	// the +1 acts as a done flag
				sbyte[] prevSolution = new sbyte[count];
				
				int solutionCount = 0;
				while(solution[count] == 0) {
					
					// add up the solution
					int total = 0;
					for(int i=0; i<pointers.Length; i++) {
						int col = pointers[i];
						total += solution[i] * matrix[row, col];
					}
					
					// check the solution
					if (total == matrix[row, len-1]) {
						solutionCount++;
						// if there's two or more solutions, we can't validate any squares
						if (solutionCount >= 2)
							break;
						
						// save the solution for later
						copyArray(solution, prevSolution);
					}
					
					// iterate through the solutions
					for(int i=0; i<solution.Length; i++) {
						solution[i]++;
						if (solution[i] > 1) {
							solution[i] = 0;
						} else {
							break;
						}
					}
					// print(solution);
				}
				if (solutionCount == 1) {
					Console.WriteLine($"row {row}");
					print(pointers);
					print(prevSolution);
				}
				
				// if there's too many solutions, we can't validate any squares
				if (solutionCount >= 2)
					continue;
				
				// add safe squares to the safeSquares array
				for (int i=0; i<prevSolution.Length; i++) {
					if (prevSolution[i] == 0) {
						int pointer = pointers[i];
						int x = pointer / WIDTH;
						int y = pointer % WIDTH;
						safeSquares[safe, 0] = x;
						safeSquares[safe, 1] = y;
						safe++;
					}
				}

			}
			return safe;
		}
		
		static void copyArray(sbyte[] a, sbyte[] b) {
			// from a to b
			// b is shorter than a
			for(int i=0; i<b.Length; i++) {
				b[i] = a[i];
			}
		}
		
		static void clickSquares(int[,] safeSquares, int count) {
			Actions actions = new Actions(driver);
			for(int i=0; i<count; i++) {
				int pixelX = safeSquares[i, 1]*SPACING + x0;
				int pixelY = safeSquares[i, 0]*SPACING + y0;
				actions.MoveToElement(canvas, pixelX, pixelY).Click();
			}
			actions.Build().Perform();
		}
		
	}	
}
