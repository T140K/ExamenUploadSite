// script för upload page

document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById('uploadForm');
    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        const fileInput = document.getElementById('file');//måste ha med en video
        if (fileInput.files.length === 0) {
            alert('Please select a video file to upload.');
            return;
        }

        const file = fileInput.files[0];//max 50 mb som blev en limit vi senare fick
        if (file.size > 50 * 1024 * 1024) {
            alert('Please select a video file smaller than 50MB.');
            return;
        }

        const formData = new FormData(form);

        try {
            const response = await fetch(form.action, {//post
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                alert('Video uploaded successfully!');
                window.location.href = '/';
            } else {
                throw new Error('Upload failed');
            }
        } catch (error) {
            alert('An error occurred during upload.');
            console.error('Upload error:', error);
        }
    });
});
