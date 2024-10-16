﻿export function downloadFileFromByteArray(data, filename, contentType) {
    // Create a Blob from the byte array (Uint8Array)
    const blob = new Blob([data], { type: contentType });

    // Create an anchor element and download the file
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    document.body.appendChild(anchor); // Required for Firefox
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
}