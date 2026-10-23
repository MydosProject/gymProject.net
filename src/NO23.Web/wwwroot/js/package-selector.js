(() => {
    document.querySelectorAll('[data-package-selector]').forEach(container => {
        const category = container.querySelector('[data-package-category]');
        const packageSelect = container.querySelector('[data-package-choice]');
        const variantSelect = container.querySelector('[data-package-variant]');
        const summary = container.querySelector('[data-package-summary]');
        const family = document.getElementById(container.dataset.familyField || '');
        if (!packageSelect || !variantSelect) return;

        const packages = Array.from(packageSelect.options).map(option => option.cloneNode(true));
        const variants = Array.from(variantSelect.options).map(option => option.cloneNode(true));
        const variantPlaceholder = variants.find(option => !option.value);
        const packagePlaceholder = packages.find(option => !option.value);

        function showSummary() {
            const selected = variantSelect.selectedOptions[0];
            const hasVariant = Boolean(selected?.value);
            if (summary) {
                summary.hidden = !hasVariant;
                const values = {
                    price: selected?.dataset.price || '',
                    'price-note': selected?.dataset.priceNote || '',
                    'total-price': selected?.dataset.totalPrice || '',
                    duration: selected?.dataset.duration || 'Seçilen paket koşullarına göre',
                    rights: selected?.dataset.rights || 'Paket kapsamına göre'
                };
                for (const [name, value] of Object.entries(values)) {
                    const field = summary.querySelector(`[data-package-${name}]`);
                    if (field) field.textContent = value;
                }
                const totalRow = summary.querySelector('[data-package-total-row]');
                if (totalRow) totalRow.hidden = !selected?.dataset.totalPrice;
            }
            if (family) family.hidden = !hasVariant || selected.dataset.category !== 'KidsClub';
        }

        function syncVariants(preserveSelection) {
            if (container.hasAttribute('data-kitchen-registration')) {
                const kitchen = packageSelect.value === 'kitchen';
                container.querySelector('[data-package-variant-field]').hidden = kitchen;
                container.querySelector('[data-kitchen-notice]').hidden = !kitchen;
                variantSelect.disabled = kitchen;
                variantSelect.required = !kitchen;
                const submitLabel = container.closest('form')?.querySelector('[data-registration-submit-label]');
                if (submitLabel) submitLabel.textContent = kitchen ? 'Kitchen hesabımı oluştur' : 'Hesabımı oluştur';
            }
            const previous = preserveSelection ? variantSelect.value : '';
            const available = variants.filter(option => option.value && option.dataset.package === packageSelect.value);
            variantSelect.replaceChildren();
            if (variantPlaceholder) variantSelect.append(variantPlaceholder.cloneNode(true));
            available.forEach(option => {
                const clone = option.cloneNode(true);
                if (clone.dataset.label) clone.textContent = clone.dataset.label;
                variantSelect.append(clone);
            });
            variantSelect.value = available.some(option => option.value === previous) ? previous : '';
            showSummary();
        }

        function syncPackages(preserveSelection) {
            if (!category) {
                syncVariants(preserveSelection);
                return;
            }
            const previous = preserveSelection ? packageSelect.value : '';
            const available = packages.filter(option => option.value && option.dataset.category === category.value);
            packageSelect.replaceChildren();
            if (packagePlaceholder) packageSelect.append(packagePlaceholder.cloneNode(true));
            available.forEach(option => packageSelect.append(option.cloneNode(true)));
            packageSelect.value = available.some(option => option.value === previous) ? previous : '';
            syncVariants(preserveSelection);
        }

        category?.addEventListener('change', () => syncPackages(false));
        packageSelect.addEventListener('change', () => syncVariants(false));
        variantSelect.addEventListener('change', showSummary);
        syncPackages(true);
    });
})();
