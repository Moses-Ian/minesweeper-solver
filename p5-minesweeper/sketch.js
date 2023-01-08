let grid;
let ROWS = 16;
let COLS = 30;
let MINES = 99;
// let ROWS = 9;
// let COLS = 9;
// let MINES = 15;
let W = 20;
let firstClick = true;
// let seed = -1;		// pick random seed -> you can see the seed in the console window
// let seed = 0;	// has to make a guess -> fails
// let seed = 1;			// solved without guessing
// let seed = 3;			// gets down to 4 unsolvable squares -> fails
// let seed = 4;			// has to make a guess -> fails
let squaresLeftP;

function setup() {
  // put setup code here
	let canvas = createCanvas(COLS*W+2, ROWS*W+2);
	canvas.parent('sketch-container');
	
	// turn off the right-click-menu
	canvas.elt.addEventListener('contextmenu', e => e.preventDefault());
	
	colorMode(HSB);
	
	// create the seed block
	if (seed != -1) {
		randomSeed(seed);
		createP(`Seed: ${seed}`);
	} else {
		// this way, we can see the seed in the console
		seed = Math.random() * Number.MAX_SAFE_INTEGER;
		randomSeed(seed);
	}
	
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
	
	// create the automatic refresh toggle
	let checkbox = createCheckbox('Auto-Refresh (Bot Only)', true);
	checkbox.class('auto-refresh');
	
	
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
		let p = createP("Finished! :)");
		p.class('finished');
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
	if (mouseButton === RIGHT) {
		rightClick();
		return;
	}
	
	let cell = getCell(mouseX, mouseY);
	if (cell.flag)
		return;

	if (firstClick) {
		moveMines(mouseX, mouseY);
		firstClick = false;
	}
	
	cell.reveal();
	
	if (cell.mine) {
		cell.clicked = true;
		gameOver();
	}
}

function rightClick() {
	getCell(mouseX, mouseY).toggleFlag();
}

function moveMines(x, y) {
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
	let p = createP("You hit a mine! :(");
	p.class('game-over');
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