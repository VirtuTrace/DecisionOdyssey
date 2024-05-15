export function addMediaEventListeners(dotNetReference, id, onMediaPlay, onMediaPause) {
    const media = document.getElementById(id);

    if(onMediaPlay != null) {
        media.addEventListener('play', () => {
            dotNetReference.invokeMethodAsync(onMediaPlay);
        });
    }

    if(onMediaPause != null) {
        media.addEventListener('pause', () => {
            dotNetReference.invokeMethodAsync(onMediaPause);
        });
    }
}