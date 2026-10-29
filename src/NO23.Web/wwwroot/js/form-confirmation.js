(() => {
    // Capture before page-specific submit handlers so cancellation also stops AJAX.
    document.addEventListener("submit", event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) ||
            !form.matches("[data-confirm]") || event.defaultPrevented) {
            return;
        }

        const message = form.dataset.confirm;
        if (message && !window.confirm(message)) {
            event.preventDefault();
            event.stopImmediatePropagation();
        }
    }, true);
})();
