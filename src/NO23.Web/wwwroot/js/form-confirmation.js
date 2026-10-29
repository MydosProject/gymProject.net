(() => {
    // Capture before page-specific submit handlers so cancellation also stops AJAX.
    document.addEventListener("submit", event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || event.defaultPrevented) return;
        if (form.dataset.submitting === "true") {
            event.preventDefault();
            event.stopImmediatePropagation();
            return;
        }
        if (!form.matches("[data-confirm]")) return;

        const message = form.dataset.confirm;
        if (message && !window.confirm(message)) {
            event.preventDefault();
            event.stopImmediatePropagation();
        }
    }, true);
})();
