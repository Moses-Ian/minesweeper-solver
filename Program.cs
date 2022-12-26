using System;
using System.Collections;
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
		static int WIDTH = 30;
		static int HEIGHT = 16;
		static sbyte MINES = 99;
		// static int WIDTH = 9;
		// static int HEIGHT = 9;
		// static sbyte MINES = 15;
		static int SPACING = 20;
		static string processLocation = @".\\tools\\chromedriver.exe";
		static string processURL = @".\\p5-minesweeper\\index.html";
		static IWebElement canvas;
		static ChromeDriver driver;
		static int x0;	// this is the location of the canvas on the screen
		static int y0;
		static int XOFF = 13;	// this is the location of the pixel to check to determine the value
		static int YOFF = 10;
		
		static void Main(string[] args) {
			// setup chrome driver
			new DriverManager().SetUpDriver(new ChromeConfig());
			driver = new ChromeDriver(processLocation);
			
			// navigate to web page
      driver.Navigate().GoToUrl(Path.GetFullPath(processURL));
			
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
			
			// first click -> click the center
			canvas.Click();
			
			while(true) {
				// take screenshot
				Console.WriteLine("taking screenshot...");
				byteArray = ((ITakesScreenshot)driver).GetScreenshot().AsByteArray;
				screenshot = new System.Drawing.Bitmap(new System.IO.MemoryStream(byteArray));
				screenshot = screenshot.Clone(crop, screenshot.PixelFormat);
				
				// use the data
				Console.WriteLine("getting game state...");
				bool result = getGameState(screenshot, gameState);
				// if we hit a mine, break
				if (!result)
					break;
				print(gameState);
				
				// turn the game state into a matrix
				// convert to matrix
				// we need a 2D array. 
				Console.WriteLine("building matrix...");
				cleanMatrix(matrix);
				buildMatrix(matrix, gameState);
				Console.WriteLine("built:");
				print(matrix);
				
				// echelon form
				Console.WriteLine("reducing to echelon form...");
				bool wasSplit;
				int lastRow;
				do {
					lastRow = echelon(matrix);
					simpleReduce(matrix, lastRow);
					wasSplit = splitRows(matrix);
				} while (wasSplit);
				print(matrix);
				// generate list of safe squares
				// this is written for readability
				Console.WriteLine("finding safe squares...");
				int count = findSafeSquares(safeSquares, matrix, lastRow);
				if (count != 0) {
					printSafeSquares(safeSquares, count);
					
					// click buttons
					Console.WriteLine("clicking squares...");
					clickSquares(safeSquares, count);
					continue;
				}
				
				// reduce matrix
				Console.WriteLine("reducing matrix...");
				do {
					reduce(matrix, lastRow);
					wasSplit = splitRows(matrix);
				} while (wasSplit);
				print(matrix);
				
				// chop matrix
				Console.WriteLine("chopping matrix...");
				int goodRows = chopRows(matrix);
				print(matrix);
				
				// generate list of safe squares
				// this is written for readability
				Console.WriteLine("finding safe squares...");
				count = findSafeSquares(safeSquares, matrix, goodRows);
				if (count != 0) {
					printSafeSquares(safeSquares, count);
					
					// click buttons
					Console.WriteLine("clicking squares...");
					clickSquares(safeSquares, count);
					continue;
				}
				
				// if there are no safe squares, do a more in-depth search
				Console.WriteLine("...doing in-depth search");
				int[] pointers;
				long[] entropy;		// a list of relative probabilities that each square is a mine -> the lower the number, the more likely it's safe
				Console.WriteLine("fully chopping matrix...");
				sbyte[,] choppedMatrix = chopRowsAndColumns(matrix, out pointers);
				print(choppedMatrix);
				count = bruteForceSolveWithPruning(safeSquares, choppedMatrix, pointers, out entropy);
				if (count != 0 && count != -1) {
					printSafeSquares(safeSquares, count);
					
					// click buttons
					Console.WriteLine("clicking squares...");
					clickSquares(safeSquares, count);
					continue;
				} 
				
				// if there are STILL no safe squares, we need to guess
				Console.WriteLine("guessing...");
				if (count == 0) {
					// we will pick the square with the lowest entropy value
					long min = entropy[0];
					int index = 0;
					for (int i=1; i<entropy.Length; i++) {
						if (entropy[i] < min) {
							min = entropy[i];
							index = i;
						}
					}
					safeSquares[0, 0] = pointers[index] / WIDTH;
					safeSquares[0, 1] = pointers[index] % WIDTH;
					count = 1;
					printSafeSquares(safeSquares, count);
					
					// click buttons
					Console.WriteLine("clicking squares...");
					clickSquares(safeSquares, count);
					continue;
				}
				
				// count == -1
				// -> just loop around again
			}
			
			print(gameState);
			Console.WriteLine("done");
		}
		
		// METHODS
		static void print(sbyte[,] matrix) {
			int rows = matrix.GetLength(0);
			int cols = matrix.GetLength(1);
			for(int i=0; i<rows; i++) {
				StringBuilder sb = new StringBuilder();
				sb.Append("{ ");
				bool allZero = true;
				for(int j=0; j<cols-1; j++) {
					sb.Append(matrix[i, j].ToString());
					if (matrix[i, j] != 0)
						allZero = false;
				}
				sb.Append(" | ");
				sb.Append(matrix[i, cols-1].ToString());
				sb.Append($" }} - {i}");
				if (allZero) 
					continue;
				Console.WriteLine(sb.ToString());
			}
			Console.WriteLine();
		}

		static void printCommas(sbyte[,] matrix) {
			int rows = WIDTH*HEIGHT+1;
			int cols = WIDTH*HEIGHT+1;
			for(int i=0; i<rows; i++) {
				StringBuilder sb = new StringBuilder();
				sb.Append("{ ");
				bool allZero = true;
				for(int j=0; j<cols; j++) {
					sb.Append(matrix[i, j].ToString());
					if (matrix[i, j] != 0)
						allZero = false;
					sb.Append(", ");
				}
				sb.Append(" },");
				if (allZero) 
					continue;
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

		static void print(long[] array) {
			StringBuilder sb = new StringBuilder();
			sb.Append("[ ");
			for(int i=0; i<array.Length; i++) {
				sb.Append(array[i].ToString());
				sb.Append(' ');
			}
			sb.Append("]");
			Console.WriteLine(sb.ToString());
		}

		static void printSolution(ulong solution, ulong doneFlagMask) {
			StringBuilder sb = new StringBuilder();
			sb.Append("[ ");
			for(ulong mask=1; mask<doneFlagMask; mask<<=1) {
				sb.Append((solution & mask) != 0 ? 1 : 0);
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
			return determineNumber(hue, saturation, value);
		}
		
		static void ColorToHSV(Color color, out double hue, out double saturation, out double value)	{
			int max = Math.Max(color.R, Math.Max(color.G, color.B));
			int min = Math.Min(color.R, Math.Min(color.G, color.B));

			hue = color.GetHue();
			saturation = (max == 0) ? 0 : 1d - (1d * min / max);
			value = max / 255d;
		}
		
		static int determineNumber(double h, double s, double v) {
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
				if (mRow == 4)
					Console.WriteLine("equation 4");
				// which game element am i talking about?
				int gRow = mRow / WIDTH;
				int gCol = mRow % WIDTH;
				// Console.WriteLine($"gRow={gRow} gCol={gCol}");
				
				// if the square is unclicked, skip
				if (mRow == 4)
					Console.WriteLine(gameState[gRow, gCol] == -1 || gameState[gRow, gCol] == 0);
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
		
		static int echelon(sbyte[,] matrix) {
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
				
				// divide each element of this row by the value of the target column
				sbyte val = matrix[row, col];
				for (int j=0; j<len; j++) {
					if (matrix[row, j] == 1 && val == 2) {
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("BAD MATH");
						Console.ForegroundColor = ConsoleColor.White;
					}
					matrix[row, j] /= val;
				}
				
				// for each row below this, get this column to zero
				zeroColumn(matrix, row, col);
			}
			
			return row;
		}

		static void reduce(sbyte[,] matrix, int lastRow) {
			int row = lastRow;
			int len = WIDTH*HEIGHT+1;
			
			// now go back upwards
			for(row--; row>=0; row--) {
				// find the leftmost 1
				for(int col=0; col<len-2; col++) {
					if (matrix[row, col] == 1) {
						// for each row above this one, get this column to zero
						zeroColumnUpward(matrix, row, col);
						break;
					}
				}
				
			}
		}
		
		static void simpleReduce(sbyte[,] matrix, int lastRow) {
			// this is not the actual step of rref.
			// this is modified, and only goes upward if the equation is *simple*
			// -> it says 'square = mine' and nothing else
			
			int row = lastRow;
			int len = WIDTH*HEIGHT+1;
			
			// now go back upwards
			for(row--; row>=0; row--) {
				if (!simpleRow(matrix, row))
					continue;
				// find the leftmost 1
				for(int col=0; col<len-2; col++) {
					if (matrix[row, col] == 1) {
						// for each row above this one, get this column to zero
						zeroColumnUpward(matrix, row, col);
						break;
					}
				}
				
			}
		}
		
		static bool simpleRow(sbyte[,] matrix, int row) {
			int cols = matrix.GetLength(1);
			// the last element must be 1
			if (matrix[row, cols-1] != 1)
				return false;
			
			int oneCount = 0;
			for (int j=0; j<cols-1; j++) {
				// elements must be zero or one
				if (matrix[row, j] != 1 && matrix[row, j] != 0)
					return false;
				// there can only be 1 one
				if (matrix[row, j] == 1) {
					oneCount++;
					if (oneCount > 1)
						return false;
				}
			}
			
			// there must be one 1
			if (oneCount == 0)
				return false;
			
			// all checks passed!
			return true;
		}
		
		static bool splitRows(sbyte[,] matrix) {
			Console.WriteLine("splitting rows...");
			// if rows have multiple mines in them, they don't get calculated correctly
			int rows = matrix.GetLength(0);
			int cols = matrix.GetLength(1);
			bool wasSplit = false;
			
			// we need to find the first row that is all zeros
			int targetRow = 0;
			for (int i=0; i<rows; i++) {
				bool allZeros = true;
				for (int j=0; j<cols; j++) {
					if (matrix[i, j] != 0) {
						allZeros = false;
						break;
					}
				}
				if (!allZeros)
					continue;
				targetRow = i;
				break;
			}
			
			Console.WriteLine($"targetRow={targetRow}");
			
			// for each row...
			for (int i=0; i<targetRow; i++) {
				// ...we need to see if this row is all ones and has multiple ones
				int count = 0;
				bool allOnes = true;
				for (int j=0; j<cols-1; j++) {
					if (matrix[i, j] == 1)
						count++;
					if (matrix[i, j] != 0 && matrix[i, j] != 1) {
						allOnes = false;
						break;
					}
				}
				
				if (!allOnes)
					continue;
				
				if (count != matrix[i, cols-1])
					continue;
				
				if (count == 1)
					continue;
				
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"splitting row {i}");
				Console.ForegroundColor = ConsoleColor.White;
				
				// this row needs to be split
				wasSplit = true;
				for (int j=0; j<cols-1; j++) {
					if (matrix[i, j] != 1) 
						continue;
					// split this row
					matrix[i, j] = 0;
					matrix[i, cols-1]--;
					// put the split in the target row
					matrix[targetRow, j] = 1;
					matrix[targetRow, cols-1] = 1;
					targetRow++;
					// check to see if it's fully split
					if (matrix[i, cols-1] == 1) 
						break;
				}
			}
			
			return wasSplit;
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
			int len = matrix.GetLength(1);
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
				for(int j=0; j<len; j++) {
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
				for(int j=0; j<len; j++) {
					matrix[i, j] -= (sbyte) (matrix[row, j] * multiplier);
				}
			}
		}
		
		static int findSafeSquares(int[,] safeSquares, sbyte[,] matrix, int goodRows) {
			// uses a brute-force method to solve each equation
			// if there's only one solution, we can use it
			// if there's more, we skip and move on
			
			int rows = matrix.GetLength(0);
			int cols = matrix.GetLength(1);
			int count = 0;
			int safe = 0;
			// for each row, brute force to see if there's a solution
			for(int row=0; row<goodRows; row++) {
				Console.WriteLine($"--> solving row {row}");
				// set up an array of variables, to point to the elements
				count = 0;
				bool allOnes = true;
				for(int col=0; col<cols-1; col++) {
					if (matrix[row, col] != 0)
						count++;
					if (matrix[row, col] != 0 && matrix[row, col] != 1)
						allOnes = false;
				}

				// if the row is all zeros, get out of here
				if (count == 0)
					break;
				
				// if the row is too crazy, move on to the next row
				// but if the row is 'simple' try it anyways
				if (count > 10 && !(allOnes && matrix[row, cols-1] == 0))
				// if (count > 10)
					continue;
				
				
				// put the pointers in the array
				int[] pointers = new int[count];
				count = 0;
				for(int col=0; col<cols-1; col++) {
					if (matrix[row, col] != 0) {
						pointers[count] = col;
						count++;
					}
				}
				
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
					if (total == matrix[row, cols-1]) {
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
				}

				// this shouldn't happen
				if (solutionCount == 0) {
					Console.ForegroundColor = ConsoleColor.Blue;
					Console.WriteLine("zero solutions");
					Console.ForegroundColor = ConsoleColor.White;
				}
				
				// if there's too many solutions, we can't validate any squares
				if (solutionCount != 1)
					continue;
				
				// add safe squares to the safeSquares array
				Console.ForegroundColor = ConsoleColor.Red;
				for (int i=0; i<prevSolution.Length; i++) {
					if (prevSolution[i] == 0) {
						int pointer = pointers[i];
						int x = pointer / WIDTH;
						int y = pointer % WIDTH;
						safeSquares[safe, 0] = x;
						safeSquares[safe, 1] = y;
						safe++;
						Console.WriteLine($"adding {x} {y}");
					}
				}
				Console.ForegroundColor = ConsoleColor.White;

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
		
		static int chopRows(sbyte[,] matrix) {
			int rows = matrix.GetLength(0);
			int cols = matrix.GetLength(1);
			int targetRow = 0;
			// for each row...
			for(int i=0; i<rows; i++) {
				// if the elements are all 1, and the number of 1s equals the total
				bool allOnes = true;
				int count = 0;
				for(int j=0; j<cols-1; j++) {
					if (matrix[i, j] != 0 && matrix[i, j] != 1) {
						allOnes = false;
						break;
					}
					count += matrix[i, j];
				}

				if (allOnes && count == matrix[i, cols-1]) {
					// it's a simple row -> chop it
					continue;
				}
				
				// put the current row in the target row
				for (int j=0; j<cols; j++) {
					matrix[targetRow, j] = matrix[i, j];
				}
				
				// increment the target row -> only do this if the row wasn't chopped
				targetRow++;
			}
			
			// finally, zero out everything after the target row
			for (int i=targetRow; i<rows; i++)
				for (int j=0; j<cols; j++)
					matrix[i, j] = 0;
				
			// return the number of rows of valid data
			return targetRow;
		}
		
		static sbyte[,] chopRowsAndColumns(sbyte[,] matrix, out int[] pointers) {
			int rows = matrix.GetLength(0);
			int cols = matrix.GetLength(1);
			// chop rows
			int targetRow = 0;
			// for each row...
			for(int i=0; i<rows; i++) {
				// Console.WriteLine($"row {i}");
				// if the elements are all 1, and the number of 1s equals the total
				bool allOnes = true;
				int onesCount = 0;
				for(int j=0; j<cols-1; j++) {
					if (matrix[i, j] != 0 && matrix[i, j] != 1) {
						allOnes = false;
					}
					onesCount += Math.Abs(matrix[i, j]);
				}
				
				// Console.WriteLine($"allOnes: {allOnes} onesCount: {onesCount} element: {matrix[i, cols-1]}");

				if (allOnes && onesCount == matrix[i, cols-1]) {
					// it's a simple row -> chop it
					continue;
				}
				
				// if there are too many ones, it'll slow down the solve -> chop it
				if (onesCount > 10)
					continue;
				
				// put the current row in the target row
				for (int j=0; j<cols; j++) {
					matrix[targetRow, j] = matrix[i, j];
				}
				
				// increment the target row -> only do this if the row wasn't chopped
				targetRow++;
			}
			
			// need to set targetRow back one to get the correct number of valid rows
			targetRow--;
			
			// finally, zero out everything after the target row
			for (int i=targetRow; i<rows; i++)
				for (int j=0; j<cols; j++)
					matrix[i, j] = 0;
				
			// chop columns
			pointers = new int[cols];
			int targetCol = 0;
			for (int j=0; j<cols-1; j++) {
				// if every element in this column is zero...
				bool allZero = true;
				for (int i=0; i<targetRow; i++) {
					if (matrix[i, j] != 0) {
						allZero = false;
						break;
					}
				}
				
				// ...chop it
				if (allZero) {
					continue;
				}
				
				// put the current column in the target column
				for (int i=0; i<targetRow; i++) {
					matrix[i, targetCol] = matrix[i, j];
				}
				
				// put a reference to the current column in the list of pointers
				pointers[targetCol] = j;
				
				// increment the target column -> only do this if the column wasn't chopped
				targetCol++;
			}
			
			// put the last column in the new matrix
			for (int i=0; i<targetRow; i++) {
				matrix[i, targetCol] = matrix[i, cols-1];
			}
			// pointers[targetCol] = cols-1;
			targetCol++;
			
			// finally, zero out everything after the target column
			for (int i=0; i<targetRow; i++)
				for (int j=targetCol; j<cols; j++)
					matrix[i, j] = 0;
			
			// create a new, shrunk matrix to be returned
			sbyte[,] chopped = new sbyte[targetRow, targetCol];
			for(int i=0; i<targetRow; i++)
				for(int j=0; j<targetCol; j++)
					chopped[i,j] = matrix[i,j];
			
			// slice the pointers array
			pointers = pointers[..(targetCol-1)];
			
			return chopped;
		}
		
		static int bruteForceSolveWithPruning(int[,] safeSquares, sbyte[,] matrix, int[] pointers, out long[] finalSolution) {
			if (pointers.Length > 63) {
				Console.WriteLine($"solution space too large! ({pointers.Length})");
				finalSolution = new long[0];
				return 0;
			}
			int rows = matrix.GetLength(0);
			int cols = matrix.GetLength(1);
			int doneFlagCol = cols-1;
			int totalValid = 0;
			
			// solution is a mask
			// the done flag is element 31 -> 
			ulong solution = 0;
			
			ulong doneFlagMask = (ulong)(1UL << (cols-1));
			int doneFlagMaskLog2 = (int)Math.Log2(doneFlagMask);
			
			// finalSolution is an array with meaningful values
			//  0: every solution has zero as the answer
			//	>0: some solutions contain a mine -> the greater the number, the more often there's a mine
			finalSolution = new long[pointers.Length];
			
			// prune list
			var pruneList = new ArrayList();
			
			// for each solution...
			while((solution & doneFlagMask) != doneFlagMask) {
				// check whether this solution should be pruned
				ulong newSolution;
				bool result = prune(pruneList, solution, doneFlagMask, out newSolution);
				if (result) {
					// it should -> set solution to the updated value and try again
					solution = newSolution;
					continue;
				}

				// check each row
				bool validSolution = true;
				for (int row=0; row<rows; row++) {
					// dot multiply the row by the solution
					int total = 0;
					for (ulong mask = 1; mask<doneFlagMask; mask <<= 1) {
						int col = (int)Math.Log2(mask);
						int multiplier = (solution & mask) != 0 ? 1 : 0;
						total += matrix[row, col] * multiplier;
					}
					
					// if the solution does not match the equation...
					if (total != matrix[row, cols-1]) {
						// this solution is invalid 
						validSolution = false;
						// -> add it to the prune list
						addToPruneList(pruneList, solution, doneFlagMask, matrix, row);
						// -> break
						break;
					}
					// keep going!
				}

				if (validSolution) {
					totalValid++;
					// update finalSolution with our current solution
					for (ulong mask = 1; mask<doneFlagMask; mask <<= 1) {
						int col = (int)Math.Log2(mask);
						sbyte bit = (sbyte)((solution & mask) != 0 ? 1 : 0);
						finalSolution[col] += bit;
					}
				}
				
				// iterate through the solutions
				solution++;
			}
			
			// this shouldn't happen
			if (totalValid == 0) {
				Console.WriteLine("there were no valid solutions");
				return -1;
			}
			
			// create an int array with the correct size
			int safeCount = 0;
			for(int i=0; i<finalSolution.Length; i++) 
				if (finalSolution[i] == 0)
					safeCount++;
			
			// fill it with the safe pointers
			int safe = 0;
			for (int i=0; i<finalSolution.Length; i++) {
				if (finalSolution[i] == 0) {
					int pointer = pointers[i];
					int x = pointer / WIDTH;
					int y = pointer % WIDTH;
					safeSquares[safe, 0] = x;
					safeSquares[safe, 1] = y;
					safe++;
					Console.WriteLine($"adding {x} {y}");
				}
			}
			
			return safeCount;
			
		}
		
		static void addToPruneList(ArrayList pruneList, ulong solution, ulong doneFlagMask, sbyte[,] matrix, int row) {
			int doneFlagMaskLog2 = (int)Math.Log2(doneFlagMask);
			// build the mask
			ulong mask = 0;
			for (int i=0; i<doneFlagMaskLog2; i++) {
				if (matrix[row, i] != 0) {
					mask |= (ulong)(1UL << i);
				}
			}
			// Console.WriteLine("solution:");
			// printSolution(solution, doneFlagMask);
			// Console.WriteLine("mask:");
			// printSolution(mask, doneFlagMask);
			// Console.WriteLine("solution & mask:");
			// printSolution(solution & mask, doneFlagMask);
			
			// build the tuple
			var tuple = new Tuple<ulong, ulong>(mask, solution & mask);
			
			// put the tuple in the array
			pruneList.Add(tuple);
			// Console.WriteLine($"added to pruneList <{mask}, {solution & mask}>");
			
		}
		
		// this is purely for testing and visualization, so I added it here
		static int skipped = 0;
		
		static bool prune(ArrayList pruneList, ulong solution, ulong doneFlagMask, out ulong newSolution) {
			// Console.ForegroundColor = ConsoleColor.Red;
			// Console.WriteLine("checking whether to prune...");
			// for each element in the pruneList...
			for(int i=0; i<pruneList.Count; i++) {
				// get the tuple
				var tuple = (Tuple<ulong, ulong>)pruneList[i];
				
				// see if this solution is in the pruneList
				ulong mask = tuple.Item1;
				ulong pruneSolution = tuple.Item2;
				if ((solution & mask) != pruneSolution) {
					// don't prune it
					continue;
				}
				
				// Console.WriteLine("pruned by");
				// printSolution(mask, doneFlagMask);
				// printSolution(pruneSolution, doneFlagMask);
				// Console.WriteLine("");

				// prune it

				// -> find the lsb
				long negMask = (long)mask;
				negMask = -negMask;
				ulong lsb = (mask & (ulong)negMask);
				// Console.WriteLine("lsb:");
				// printSolution(lsb, doneFlagMask);
				
				// -> increment that
				newSolution = solution + lsb;
				
				// -> set everything below the lsb back to zero, so we don't skip anything
				int negLSB = (int)lsb;
				negLSB = -negLSB;
				// Console.WriteLine("negLSB:");
				// printSolution((ulong)negLSB, doneFlagMask);
				newSolution = newSolution & (ulong)negLSB;
				
				skipped += (int)(newSolution - solution);
				// Console.WriteLine($"skipped {skipped}");
				
				// Console.ForegroundColor = ConsoleColor.White;
				// -> return it
				return true;
			}
			// Console.WriteLine("...don't prune");
			// Console.ForegroundColor = ConsoleColor.White;
			newSolution = solution;
			return false;
		}
		
		static int guess(int[,] safeSquares, int[,] gameState) {
			int rows = gameState.GetLength(0);
			int cols = gameState.GetLength(1);
			// we need to find a completely unchecked square
			// one where it and every square around it are -1
			for(int i=1; i<rows-1; i++) {
				for(int j=1; j<cols-1; j++) {
					bool result = isUnchecked(gameState, i, j);
					if (!result)
						continue;
					
					// we found a good square -> return it
					safeSquares[0, 0] = i;
					safeSquares[0, 1] = j;
					return 1;
				}
			}
			
			// there are no such squares -> we need to make a different kind of guess
			return 0;
		}
		
		static bool isUnchecked(int[,] gameState, int row, int col) {
			// check that every square in this 3x3 grid is -1
			for (int i=-1; i<=1; i++) 
				for (int j=-1; j<=1; j++) 
					if (gameState[row+i, col+j] != -1)
						return false;
			return true;
		}

	}	
}
