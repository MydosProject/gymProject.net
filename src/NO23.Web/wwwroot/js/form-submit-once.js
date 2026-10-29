(() => {
    const pending = new Map();
    const reset = form => {
        const controls = pending.get(form);
        if (!controls) return;
        controls.forEach(({button, disabled, html}) => {
            button.disabled = disabled;
            if (html !== null) button.innerHTML = html;
        });
        delete form.dataset.submitting;
        form.removeAttribute("aria-busy");
        pending.delete(form);
    };

    document.addEventListener("submit", event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || form.method.toLowerCase() !== "post" ||
            !form.matches("[data-submit-once], [data-confirm]") || event.defaultPrevented) return;

        if (form.dataset.submitting === "true") {
            event.preventDefault();
            return;
        }
        form.dataset.submitting = "true";
        form.setAttribute("aria-busy", "true");
        const controls = Array.from(form.elements)
            .filter(control => control instanceof HTMLButtonElement && control.type === "submit" ||
                control instanceof HTMLInputElement && control.type === "submit")
            .map(button => ({button, disabled: button.disabled,
                html: button instanceof HTMLButtonElement ? button.innerHTML : null}));
        pending.set(form, controls);

        // The browser must first serialize the clicked button's name/value.
        setTimeout(() => {
            if (event.defaultPrevented) { reset(form); return; }
            controls.forEach(({button}) => { button.disabled = true; });
            if (event.submitter instanceof HTMLButtonElement) event.submitter.textContent = "İşleniyor…";
        }, 0);
    });

    window.addEventListener("pageshow", () => Array.from(pending.keys()).forEach(reset));
})();
