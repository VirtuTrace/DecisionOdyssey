/**
 * Creates a blob url from the given data and content type.
 * @param data {Uint8Array} The data to create the blob url from.
 * @param contentType {string} The content type of the data.
 * @returns {string} The blob url.
 */
export function createBlobUrl(data, contentType) {
    const blob = new Blob([data], { type: contentType });
    return URL.createObjectURL(blob);
}

/**
 * Revokes the given blob url.
 * @param url {string} The blob url to revoke.
 */
export function revokeBlobUrl(url) {
    URL.revokeObjectURL(url);
}