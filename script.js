window.onload = () => {
	const queryString = window.location.search;
	const urlParams = new URLSearchParams(queryString);
	const isBot = urlParams.get('bot');

	if (isBot) {
		console.log("this is a bot");
		
		const warningElement = document.getElementById("warning");
		console.log(warningElement);
		warningElement.style.display = 'block';
		
		const blinkElement = document.getElementById("blink");
		setInterval(() => blink.style.opacity = 1 - blink.style.opacity, 1000);
	}
}