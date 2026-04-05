// PDF Download Helper Functions
// Handles downloading PDF files from API endpoints

window.pdfDownload = {
    // Download PDF from API endpoint using POST method with JSON body
    downloadPdfPost: async function (url, data, filename) {
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(data)
            });

            if (!response.ok) {
                const errorText = await response.text();
                console.error('PDF download failed:', errorText);
                return { success: false, error: errorText };
            }

            const blob = await response.blob();
            const downloadUrl = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = downloadUrl;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(downloadUrl);

            return { success: true };
        } catch (error) {
            console.error('PDF download error:', error);
            return { success: false, error: error.message };
        }
    },

    // Download PDF from API endpoint using GET method
    downloadPdfGet: async function (url, filename) {
        try {
            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/pdf',
                }
            });

            if (!response.ok) {
                const errorText = await response.text();
                console.error('PDF download failed:', errorText);
                return { success: false, error: errorText };
            }

            const blob = await response.blob();
            const downloadUrl = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = downloadUrl;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(downloadUrl);

            return { success: true };
        } catch (error) {
            console.error('PDF download error:', error);
            return { success: false, error: error.message };
        }
    },

    // Open PDF in new tab instead of downloading
    openPdfInNewTab: async function (url, method = 'GET', data = null) {
        try {
            const options = {
                method: method,
                headers: method === 'POST' ? { 'Content-Type': 'application/json' } : {}
            };

            if (method === 'POST' && data) {
                options.body = JSON.stringify(data);
            }

            const response = await fetch(url, options);

            if (!response.ok) {
                return { success: false, error: 'Failed to fetch PDF' };
            }

            const blob = await response.blob();
            const pdfUrl = window.URL.createObjectURL(blob);
            window.open(pdfUrl, '_blank');

            return { success: true };
        } catch (error) {
            console.error('PDF open error:', error);
            return { success: false, error: error.message };
        }
    }
};
