window.exerciseCardHelper = {
    capturePositions: function () {
        const cards = document.querySelectorAll('[data-exercise-card]');
        const positions = {};
        cards.forEach(card => {
            const key = card.getAttribute('data-exercise-card');
            positions[key] = card.getBoundingClientRect().top;
        });
        return positions;
    },
    animateFlip: function (oldPositions) {
        const cards = document.querySelectorAll('[data-exercise-card]');
        cards.forEach(card => {
            const key = card.getAttribute('data-exercise-card');
            const oldTop = oldPositions[key];
            if (oldTop === undefined) return;
            const newTop = card.getBoundingClientRect().top;
            const deltaY = oldTop - newTop;
            if (Math.abs(deltaY) < 1) return;
            card.animate(
                [{ transform: `translateY(${deltaY}px)` }, { transform: 'translateY(0)' }],
                { duration: 250, easing: 'ease-out' }
            );
        });
    }
};
