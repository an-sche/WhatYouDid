// Use wakelock to prevent the users screen from going to sleep
// For Example: When the timer is running, it shouldn't go to sleep

let wakeLock = null;

window.wakeLockHelper = {
    enable: async () => {
        try {
            if ('wakeLock' in navigator) {
                wakeLock = await navigator.wakeLock.request('screen');
            }
        } catch { }
    },
    disable: async () => {
        try {
            if (wakeLock !== null) {
                await wakeLock.release();
                wakeLock = null;
            }
        } catch { }
    }
};