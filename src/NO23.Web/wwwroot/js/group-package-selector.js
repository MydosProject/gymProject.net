(() => {
    document.querySelectorAll('form[data-group-package-selector]').forEach(form => {
        const controls = form.querySelector('[data-group-package-controls]');
        const fallback = form.querySelector('[data-group-package-fallback]');
        const summary = form.querySelector('[data-group-package-summary]');
        const lessonButtons = Array.from(form.querySelectorAll('[data-group-lesson]'));
        const monthButtons = Array.from(form.querySelectorAll('[data-group-months]'));
        const otherButtons = Array.from(form.querySelectorAll('[data-group-other]'));
        const other = form.querySelector('[data-group-package-other]');
        const radios = Array.from(form.querySelectorAll('input[type="radio"][name="variant"]'));
        const options = Array.from(form.querySelectorAll('template[data-group-variant]')).map(template => ({
            id: template.dataset.groupVariant,
            family: template.dataset.optionFamily || '',
            months: Number(template.dataset.optionMonths),
            bonus: Number(template.dataset.optionBonus),
            template,
            radio: radios.find(radio => radio.value === template.dataset.groupVariant)
        }));
        const grouped = options.filter(option => option.family);
        if (!controls || !fallback || !summary || !grouped.length || options.some(option => !option.radio)) return;
        let current = options.find(option => option.radio.checked) || options[0];
        let family = current.family || grouped[0].family;

        const sync = () => {
            lessonButtons.forEach(button => button.setAttribute('aria-pressed', String(current.family === button.dataset.groupLesson)));
            monthButtons.forEach(button => {
                const option = grouped.find(item => item.family === family && item.months === Number(button.dataset.groupMonths));
                button.disabled = !option;
                button.setAttribute('aria-pressed', String(Boolean(current.family) && current.months === Number(button.dataset.groupMonths)));
                const gift = button.querySelector('[data-group-gift]');
                gift.hidden = !option || !option.bonus;
                gift.textContent = option?.bonus ? `${option.bonus} ay hediye` : '';
            });
            otherButtons.forEach(button => button.setAttribute('aria-pressed', String(current.id === button.dataset.groupOther)));
            summary.replaceChildren(current.template.content.cloneNode(true));
        };
        const choose = option => {
            if (!option) return;
            option.radio.checked = true;
            option.radio.dispatchEvent(new Event('change', { bubbles: true }));
        };
        radios.forEach(radio => radio.addEventListener('change', () => {
            if (!radio.checked) return;
            current = options.find(option => option.id === radio.value);
            if (current.family) family = current.family;
            sync();
        }));
        lessonButtons.forEach(button => button.addEventListener('click', () => {
            const matches = grouped.filter(option => option.family === button.dataset.groupLesson);
            choose(matches.find(option => option.months === current.months) || matches[0]);
        }));
        monthButtons.forEach(button => button.addEventListener('click', () => {
            choose(grouped.find(option => option.family === family && option.months === Number(button.dataset.groupMonths)));
        }));
        otherButtons.forEach(button => button.addEventListener('click', () => {
            choose(options.find(option => option.id === button.dataset.groupOther));
        }));

        // Keep the original radios as the submitted selection and as the usable
        // fallback when JavaScript is unavailable. No prices are calculated here.
        current.radio.checked = true;
        sync();
        if (other && !current.family) other.open = true;
        fallback.hidden = true;
        controls.hidden = false;
    });
})();
