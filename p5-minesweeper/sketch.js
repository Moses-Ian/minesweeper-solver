let grid;
// let ROWS = 16;
// let COLS = 30;
// let MINES = 99;
let ROWS = 9;
let COLS = 9;
let MINES = 10;
let W = 20;
let firstClick = true;
let squaresLeftP;

function setup() {
  // put setup code here
	let canvas = createCanvas(COLS*W+2, ROWS*W+2);
	canvas.parent('sketch-container');
	
	colorMode(HSB);
	
	// create the board
	grid = make2DArray(ROWS, COLS);
	for(let i=0; i<ROWS; i++) {
		for(let j=0; j<COLS; j++) {
			grid[i][j] = new Cell(i, j, W);
		}
	}
	fillMines(grid, MINES);
	for(let i=0; i<ROWS; i++) {
		for(let j=0; j<COLS; j++) {
			grid[i][j].countNeighbors();
		}
	}
	
	// create the squaresLeft block
	createP(`Mines: ${MINES}`);
	squaresLeftP = createP();
	
	// for testing
	// for(let i=0; i<=8; i++) {
		// grid[i][0].neighborCount = i;
		// grid[i][0].revealed = true;
		// grid[i][0].mine = false;
	// }
	// grid[9][0].revealed = true;
	// grid[9][0].mine = true;
	
	
}

function draw() {
  // put drawing code here
	background(255);
	
	for(let i=0; i<ROWS; i++) {
		for(let j=0; j<COLS; j++) {
			grid[i][j].show();
		}
	}
	
	let squaresLeft = countSquares();
	
	squaresLeftP.html(`Squares left: ${squaresLeft}`);
	
	if (squaresLeft == MINES) {
		createP("Finished! :)");
		noLoop();
	}
}

function make2DArray(rows, cols) {
	let arr = new Array(rows);
	for (let i=0; i<rows; i++) {
		arr[i] = new Array(cols);
	}
	return arr;
}

function fillMines(arr) {
	for(let i=MINES; i>0; i--) {
		let col = Math.floor(random(COLS));
		let row = Math.floor(random(ROWS));
		if (arr[row][col].mine) {
			i++;
			continue;
		}
		arr[row][col].mine = true;
	}
}

function getCell(x, y) {
	return grid[Math.floor(y/W)][Math.floor(x/W)];
}

function mousePressed() {
	// console.log(`mouse clicked at ${mouseX}, ${mouseY}`);
	
	if (firstClick) {
		moveMines(mouseX, mouseY);
		firstClick = false;
	}
	
	getCell(mouseX, mouseY).reveal();
	
	if (getCell(mouseX, mouseY).mine) {
		gameOver();
	}
}

function moveMines(x, y) {
	console.log("moving mines");
	let row = Math.floor(y/W);
	let col = Math.floor(x/W);
	for(let i=-1; i<=1; i++) {
		if (row+i < 0 || row+i == ROWS)
			continue;
		for(let j=-1; j<=1; j++) {
			if (col+j < 0 || col+j == COLS)
				continue;
			let neighbor = grid[row+i][col+j];
			if (neighbor.mine) {
				let newRow = Math.floor(random(ROWS));
				let newCol = Math.floor(random(COLS));
				if (Math.abs(row - newRow) <= 1 && Math.abs(row - newRow) <= 1) {
					j--;
					continue;
				}
				if (grid[newRow][newCol].mine) {
					j--;
					continue;
				}
				grid[newRow][newCol].mine = true;
				grid[row+i][col+j].mine = false;
				// console.log(`mine moved from ${row+i},${col+j} to ${newRow},${newCol}`);
			}
		}
	}
	
	// update the grid
	for(let i=0; i<ROWS; i++) {
		for(let j=0; j<COLS; j++) {
			grid[i][j].countNeighbors();
		}
	}	
}

function gameOver() {
	for(let i=0; i<ROWS; i++) {
		for(let j=0; j<COLS; j++) {
			if (grid[i][j].mine)
				grid[i][j].reveal();
		}
	}
	noLoop();
}

function countSquares() {
	count = 0;
	for(let i=0; i<ROWS; i++) {
		for(let j=0; j<COLS; j++) {
			if (!grid[i][j].revealed)
				count++;
		}
	}
	return count;
}