(() => {
    const money = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' });
    const cents = value => {
        const parsed = Number(value);
        return value !== '' && Number.isFinite(parsed) ? Math.round((parsed + Number.EPSILON) * 100) : null;
    };
    const monthlyDate = (date, offset) => {
        const first = new Date(`${date}T00:00:00Z`);
        if (Number.isNaN(first.getTime())) return '';
        const totalMonths = first.getUTCFullYear() * 12 + first.getUTCMonth() + offset;
        const year = Math.floor(totalMonths / 12);
        const month = totalMonths % 12;
        const lastDay = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
        const day = Math.min(first.getUTCDate(), lastDay);
        return `${String(year).padStart(4, '0')}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    };

    document.querySelectorAll('[data-manual-payment-plan]').forEach(plan => {
        const form = plan.closest('form');
        const variant = form?.querySelector('[data-package-variant]');
        const category = form?.querySelector('[data-package-category]');
        const family = form?.querySelector('[name="FamilyCode"]');
        const method = plan.querySelector('[data-payment-method]');
        const amount = plan.querySelector('[data-payment-amount]');
        const discount = plan.querySelector('[data-payment-discount]');
        const count = plan.querySelector('[data-payment-count]');
        const firstDue = plan.querySelector('[data-payment-first-due]');
        const rows = plan.querySelector('[data-payment-installments]');
        const summary = plan.querySelector('[data-payment-summary]');
        const error = plan.querySelector('[data-payment-plan-error]');
        const today = plan.dataset.today;
        if (!form || !variant || !method || !amount || !count || !firstDue || !rows) return;

        function setError(message) {
            error.textContent = message;
            error.hidden = !message;
        }

        function summarize(checkDates = false) {
            if (!variant.value && !checkDates) {
                summary.textContent = 'Ödeme tutarı, paket seçildiğinde otomatik hesaplanır.';
                setError('');
                return true;
            }
            const target = cents(amount.value);
            const installments = Array.from(rows.querySelectorAll('.manual-payment-row'));
            let total = 0, collected = 0;
            let message = '';
            for (const row of installments) {
                const rowAmount = cents(row.querySelector('[data-installment-amount]').value);
                const paidOn = row.querySelector('[data-installment-paid]').value;
                const due = row.querySelector('[data-installment-due]').value;
                if (rowAmount === null || rowAmount <= 0) message = 'Her taksit için sıfırdan büyük bir tutar girin.';
                total += rowAmount || 0;
                if (paidOn) collected += rowAmount || 0;
                if (checkDates && !due) message = 'Her taksitin vade tarihini girin.';
                if (paidOn && paidOn > today) message = 'Tahsilat tarihi gelecekte olamaz.';
            }
            summary.textContent = `Taksit toplamı: ${money.format(total / 100)} · Tahsil edilen: ${money.format(collected / 100)} · Kalan: ${money.format(Math.max(0, total - collected) / 100)}`;
            if (target === null || target <= 0) message = 'Paket seçin veya toplam ödeme tutarını girin.';
            else if (total !== target) message = 'Taksit toplamı, toplam ödeme tutarıyla aynı olmalıdır.';
            if (!Number.isInteger(Number(count.value)) || Number(count.value) < 1 || Number(count.value) > 36)
                message = 'Taksit sayısı 1 ile 36 arasında olmalıdır.';
            else if (installments.length !== Number(count.value))
                message = 'Taksit satırları seçilen taksit sayısıyla aynı olmalıdır. Taksitleri yeniden oluşturun.';
            setError(message);
            return !message;
        }

        function makeField(index, property, labelText, value, type) {
            const field = document.createElement('div');
            const name = `Payment.Installments[${index}].${property}`;
            const id = `Payment_Installments_${index}__${property}`;
            const label = document.createElement('label');
            label.className = 'form-label';
            label.htmlFor = id;
            label.textContent = labelText;
            const input = document.createElement('input');
            input.name = name;
            input.id = id;
            input.type = type;
            input.value = value;
            input.className = 'form-control';
            input.required = property !== 'PaidOn';
            if (property === 'Amount') {
                input.min = '0.01';
                input.step = '0.01';
                input.dataset.installmentAmount = '';
                input.dataset.valNumber = 'Geçerli bir tutar girin.';
            } else if (property === 'DueDate') {
                input.dataset.installmentDue = '';
                input.min = plan.dataset.earliestDate;
                input.max = plan.dataset.latestDue;
            } else {
                input.dataset.installmentPaid = '';
                input.min = plan.dataset.earliestDate;
                input.max = today;
            }
            if (input.required) {
                input.dataset.val = 'true';
                input.dataset.valRequired = `${labelText} alanı zorunludur.`;
            }
            const validation = document.createElement('span');
            validation.className = 'text-danger field-validation-valid';
            validation.dataset.valmsgFor = name;
            validation.dataset.valmsgReplace = 'true';
            field.append(label, input, validation);
            if (property === 'PaidOn') {
                const note = document.createElement('small');
                note.className = 'form-text text-muted';
                note.textContent = 'Boşsa tahsilat bekliyor.';
                field.append(note);
            }
            return field;
        }

        function generateRows() {
            const total = cents(amount.value);
            const number = Number(count.value);
            if (!monthlyDate(firstDue.value, 0)) {
                setError('Taksit planı için ilk vade tarihini girin.');
                return;
            }
            if (total === null || total <= 0 || !Number.isInteger(number) || number < 1 || number > 36) {
                summarize();
                return;
            }
            if (total < number) {
                setError('Toplam tutar bu taksit sayısı için yeterli değil. Taksit sayısını azaltın.');
                return;
            }
            const regular = Math.floor(total / number);
            const fragment = document.createDocumentFragment();
            for (let index = 0; index < number; index++) {
                const row = document.createElement('div');
                row.className = 'manual-payment-row';
                const title = document.createElement('strong');
                title.className = 'manual-payment-row-number';
                title.textContent = `${index + 1}. taksit`;
                const installment = index === number - 1 ? total - regular * (number - 1) : regular;
                row.append(title,
                    makeField(index, 'Amount', 'Tutar', (installment / 100).toFixed(2), 'number'),
                    makeField(index, 'DueDate', 'Vade', monthlyDate(firstDue.value, index), 'date'),
                    makeField(index, 'PaidOn', 'Tahsilat tarihi', '', 'date'));
                fragment.append(row);
            }
            rows.replaceChildren(fragment);
            if (window.jQuery?.validator?.unobtrusive) {
                window.jQuery(form).removeData('validator').removeData('unobtrusiveValidation');
                window.jQuery.validator.unobtrusive.parse(form);
            }
            summarize();
        }

        function syncPackage(regenerate, initial = false) {
            const selected = variant.selectedOptions[0];
            const packageCategory = selected?.dataset.category || category?.value;
            Array.from(method.options).forEach(option => option.disabled = false);
            plan.querySelector('[data-payment-method-help]').textContent = 'Yönetici tüm paketlerde kredi kartı, nakit veya havale / EFT seçebilir.';
            amount.readOnly = false;
            const customPrice = selected?.dataset.priceOnRequest === 'true';
            const base = selected?.value ? cents(selected.dataset.paymentAmount || '') : null;
            const siblingDiscount = packageCategory === 'KidsClub' && Boolean(family?.value.trim());
            const extraDiscount = Number(discount?.value || 0);
            if (!initial || !amount.value) {
                amount.value = customPrice || base === null ? '' :
                    (Math.round(base * (siblingDiscount ? .75 : 1) * (100 - extraDiscount) / 100) / 100).toFixed(2);
            }
            plan.querySelector('[data-payment-amount-help]').textContent = base === null || customPrice
                ? 'Anlaşılan toplam satış tutarını girin.'
                : `Katalog bedeli: ${money.format(base / 100)}. Anlaşılan toplamı doğrudan yazabilir veya indirim yüzdesi girebilirsiniz.${siblingDiscount ? ' %25 kardeş indirimi hesaba katıldı.' : ''}`;
            if (regenerate) generateRows();
            else summarize();
        }

        form.addEventListener('change', event => {
            if (event.target.matches('[data-package-category], [data-package-choice], [data-package-variant]')) syncPackage(true);
        });
        family?.addEventListener('input', () => {
            const previous = amount.value;
            syncPackage(false);
            if (amount.value !== previous) generateRows();
        });
        discount?.addEventListener('input', () => syncPackage(true));
        count.addEventListener('change', generateRows);
        firstDue.addEventListener('change', generateRows);
        amount.addEventListener('input', () => { if (discount) discount.value = '0'; });
        amount.addEventListener('change', generateRows);
        rows.addEventListener('input', () => summarize());
        plan.querySelector('[data-payment-regenerate]').addEventListener('click', generateRows);
        form.addEventListener('submit', event => {
            if (!summarize(true)) event.preventDefault();
        });
        syncPackage(plan.dataset.hasInstallmentRows !== 'true', true);
    });
})();
