class Cell {
	constructor(i, j, w) {
		this.i = i;	// index
		this.j = j;
		this.x = j*w+1;	// pixel location
		this.y = i*w+1;
		this.w = w;
		this.mine = false;
		this.revealed = false;
		this.neighborCount;
	}
	
	reveal = () => {
		this.revealed = true;
		if (this.neighborCount == 0) {
			this.revealNeighbors();
		}
	}
	
	revealNeighbors = () => {
		for(let i=-1; i<=1; i++) {
			if (this.i+i < 0 || this.i+i == ROWS)
				continue;
			for(let j=-1; j<=1; j++) {
				if (this.j+j < 0 || this.j+j == COLS)
					continue;
				
				if (!grid[this.i+i][this.j+j].revealed)
					grid[this.i+i][this.j+j].reveal();
				
			}
		}
	}
	
	countNeighbors = () => {
		if (this.mine) {
			this.neighborCount -1;
			return;
		}
		let total = 0;
		
		for(let i=-1; i<=1; i++) {
			if (this.i+i < 0 || this.i+i == ROWS)
				continue;
			for(let j=-1; j<=1; j++) {
				if (this.j+j < 0 || this.j+j == COLS)
					continue;
				let neighbor = grid[this.i+i][this.j+j];
				if (neighbor.mine)
					total++;
			}
		}
		this.neighborCount = total;
	}
	
	show = () => {
		stroke(0);
		noFill();
		rect(this.x, this.y, this.w, this.w);
		if (this.revealed) {
			if (this.mine) {
				colorMode(HSB);
				fill(0, 0, 50);
				ellipse(this.x+this.w*.5, this.y+this.w*.5, this.w*.5);
			} else {
				fill(0, 0, 90);
				stroke(0);
				rect(this.x, this.y, this.w, this.w);
				if (this.neighborCount != 0) {
					textAlign(CENTER);
					fill((45 * this.neighborCount + 180) % 360, 80, 80);
					noStroke();
					textSize(18);
					textStyle(BOLD);
					text(this.neighborCount, this.x+this.w*.5, this.y+this.w*.85);
				}
			}
		}
	}
}