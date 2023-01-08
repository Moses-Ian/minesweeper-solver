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
}