(() => {
    document.querySelectorAll('[data-weekly-personal-schedule]').forEach(form => {
        const member = form.querySelector('[data-schedule-member]');
        const manual = form.querySelector('[data-manual-schedule-field]');
        const automatic = form.querySelector('[data-automatic-schedule-field]');
        const weeks = form.querySelector('[name="Weeks"]');
        const output = form.querySelector('[data-automatic-schedule-length]');
        const description = form.querySelector('[data-schedule-description]');
        const account = form.querySelector('[data-schedule-account]');
        const submit = form.querySelector('button[type="submit"], button:not([type])');
        const review = form.parentElement.querySelector('[data-schedule-credit-review]');
        let controller, timer, ready = false;
        const showAccount = data => {
            const storedBalance = data.storedBalance ?? data.remainingCredits;
            account.replaceChildren();
            const items = [
                ['Paketin birebir hakkı', data.packageLessons ?? (data.isUnlimited ? 'Sınırsız' : 'Belirtilmemiş')],
                ['Tamamlanan / kullanılan birebir', data.completedLessons],
                ['Önceden planlanan birebir', data.plannedLessons],
                ['Kalan toplam hak', data.isUnlimited ? 'Sınırsız' : data.remainingCredits],
                ['Yeni planlanabilir birebir', data.availableLessons ?? 'Sınırsız']
            ];
            for (const [label, value] of items) {
                const item = document.createElement('div');
                item.className = 'schedule-account-item';
                const count = document.createElement('strong');
                const name = document.createElement('span');
                count.textContent = value;
                name.textContent = label;
                item.append(count, name);
                account.append(item);
            }
            if (data.unchargedCompletedLessons && !data.isUnlimited) {
                const warning = document.createElement('p');
                warning.className = 'col-12';
                warning.textContent = `${data.unchargedCompletedLessons} tamamlanmış dersin hak düşümü kayıtlı değil. Yeni program hesabında bu dersler de ayrıldı. Yönetici geçmiş bakiyeyi kontrol etmeli.`;
                account.append(warning);
            }
            if (review && data.canReviewCredits) {
                review.hidden = false;
                review.elements.MemberProfileId.value = member.value;
                review.elements.ExpectedCount.value = data.unchargedCompletedLessons;
                review.elements.ExpectedBalance.value = storedBalance;
                review.elements.Week.value = form.elements.Week.value;
                review.querySelector('[data-credit-review-description]').textContent =
                    `${data.unchargedCompletedLessons} sonuçlanmış ders kontrol bekliyor. Haklar düşülmemişse kayıtlı bakiye: ${storedBalance} → ${storedBalance - data.unchargedCompletedLessons}. Önceden manuel düşüldüyse bakiyeyi koruyan seçeneği kullanın (${storedBalance}). İki yöntemde de dersler bir kez işlenmiş sayılır.`;
            }
        };
        async function preview() {
            if (!member.value || !form.elements.Week.value || !form.elements.Time.value ||
                (form.elements.TrainerId && !form.elements.TrainerId.value)) {
                output.textContent = 'Üye ve antrenör seçin';
                description.textContent = 'Üye ve antrenör seçildikten sonra paket hakları ve program hesaplanır.';
                return;
            }
            const currentController = controller = new AbortController();
            try {
                const response = await fetch(form.dataset.previewUrl, {
                    method: 'POST', body: new FormData(form), signal: currentController.signal,
                    credentials: 'same-origin', headers: { 'Accept': 'application/json' }
                });
                if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) throw new Error();
                const data = await response.json();
                if (currentController.signal.aborted) return;
                showAccount(data);
                output.textContent = data.lessonCount ? `${data.weekCount} hafta · ${data.lessonCount} yeni ders` : 'Yeni ders planlanmadı';
                description.textContent = data.message;
                ready = data.succeeded;
                submit.disabled = !ready;
            } catch (error) {
                if (error.name === 'AbortError') return;
                output.textContent = 'Hesaplama yapılamadı';
                description.textContent = 'Bilgileri ve bağlantıyı kontrol edip tekrar deneyin. Sayfayı yenileyebilirsiniz.';
            }
        }
        function sync(event) {
            if (event?.target.name === 'Note') return;
            clearTimeout(timer);
            controller?.abort();
            ready = false;
            submit.disabled = true;
            if (review) { review.hidden = true; review.elements.Confirmed.checked = false; review.elements.AlreadyAdjusted.value = ''; }
            account.replaceChildren();
            const hasEnd = !!member.selectedOptions[0]?.dataset.packageEndUtc;
            manual.hidden = hasEnd || !member.value;
            automatic.hidden = false;
            weeks.disabled = hasEnd || !member.value;
            output.textContent = 'Hesaplanıyor…';
            timer = setTimeout(preview, 250);
        }
        form.addEventListener('input', sync);
        form.addEventListener('change', sync);
        form.addEventListener('submit', event => { if (!ready) event.preventDefault(); });
        sync();
    });
})();
