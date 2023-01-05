// this is for testing only
function customGrid() {
	// create the board
	grid = make2DArray(ROWS, COLS);
	for(let i=0; i<ROWS; i++) {
		for(let j=0; j<COLS; j++) {
			grid[i][j] = new Cell(i, j, W);
		}
	}
	
	// one by one, set the cells
	grid[0][0]
	grid[0][1].mine = true;
	grid[0][2].revealed = true;
	grid[0][3].revealed = true;
	grid[0][4]

	grid[1][0].revealed = true;
	grid[1][1].revealed = true;
	grid[1][2]
	grid[1][3].revealed = true;
	grid[1][4].mine = true;

	grid[2][0].revealed = true;
	grid[2][1].revealed = true;
	grid[2][2].mine = true;
	grid[2][3].revealed = true;
	grid[2][4].revealed = true;

	grid[3][0].revealed = true;
	grid[3][1].revealed = true;
	grid[3][2].mine = true;
	grid[3][3].revealed = true;
	grid[3][4]

	grid[4][0].revealed = true;
	grid[4][1].revealed = true;
	grid[4][2].revealed = true;
	grid[4][3]
	grid[4][4].mine = true;
	
	return grid;
}