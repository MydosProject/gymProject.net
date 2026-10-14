(() => {
    document.querySelectorAll('[data-pt-package-selector]').forEach(form => {
        const controls = form.querySelector('[data-pt-controls]');
        const select = form.querySelector('[data-pt-select]');
        const summary = form.querySelector('[data-pt-summary]');
        const fallback = form.querySelector('[data-pt-fallback]');
        const radios = Array.from(form.querySelectorAll('input[name="variant"]'));
        const options = Array.from(form.querySelectorAll('template[data-pt-variant]')).map(template => ({
            id: template.dataset.ptVariant,
            template,
            radio: radios.find(radio => radio.value === template.dataset.ptVariant)
        }));
        if (!controls || !select || !summary || !fallback || !options.length || options.some(option => !option.radio)) return;

        const sync = () => {
            const current = options.find(option => option.radio.checked);
            if (!current) return;
            select.value = current.id;
            summary.replaceChildren(current.template.content.cloneNode(true));
        };
        select.addEventListener('change', () => {
            const option = options.find(option => option.id === select.value);
            if (!option) return;
            option.radio.checked = true;
            option.radio.dispatchEvent(new Event('change', { bubbles: true }));
        });
        radios.forEach(radio => radio.addEventListener('change', sync));
        // Restore both the price and selection when the browser restores a form.
        window.addEventListener('pageshow', sync);
        form.addEventListener('reset', () => queueMicrotask(sync));
        sync();
        fallback.hidden = true;
        controls.hidden = false;
    });
})();
