window.onload = () => {
	const queryString = window.location.search;
	const urlParams = new URLSearchParams(queryString);
	const isBot = urlParams.get('bot');

	if (isBot) {
		
		const warningElement = document.getElementById("warning");
		warningElement.style.display = 'block';
		
		const blinkElement = document.getElementById("blink");
		setInterval(() => blink.style.opacity = 1 - blink.style.opacity, 1000);
		
		const downloadElement = document.getElementById("download");
		downloadElement.style.display = 'none';
		
	}

	document.getElementById("games-played").innerText = localStorage.getItem("games-played") || "0";
	document.getElementById("games-won").innerText = localStorage.getItem("games-won") || "0";
	document.getElementById("win-rate").innerText = localStorage.getItem("win-rate") || "0";
}