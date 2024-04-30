// videoStatusChecker.js
//det som kollar om videon har blivit processed ännu
document.addEventListener("DOMContentLoaded", function () {
    const videoId = window.videoId; // Assume the videoId is set globally on the window object
    if (videoId) {
        checkProcessingStatus(videoId);
    }
});

async function checkProcessingStatus(videoId) {
    const statusText = document.getElementById('processing-status');
    const maxAttempts = 5;
    let attempts = 0;
    let delay = 5000;

    while (attempts < maxAttempts) {
        try {
            const response = await fetch(`/api/Videos/ProcessingStatus/${videoId}`);
            const status = await response.text();

            if (response.ok) {
                updateStatusDisplay(status === 'Ready');
                if (status === 'Ready') break;
            } else {
                throw new Error(`Error: ${response.status}`);
            }
        } catch (error) {
            console.error('Error while checking processing status:', error);
            updateStatusDisplay(false);
        }

        attempts++;
        await new Promise(resolve => setTimeout(resolve, delay));
        delay = Math.min(delay * 2, 32000); // 32 sec
    }
}

function updateStatusDisplay(isReady) {
    const statusText = document.getElementById('processing-status');
    statusText.textContent = isReady ? 'Video processed' : 'Video is being processed...';
    statusText.style.display = 'block';
}
