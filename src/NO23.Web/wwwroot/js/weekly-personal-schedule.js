(() => {
    const dayNumbers = { Sunday: 0, Monday: 1, Tuesday: 2, Wednesday: 3, Thursday: 4, Friday: 5, Saturday: 6 };
    const dayMilliseconds = 24 * 60 * 60 * 1000;
    const clubOffsetMilliseconds = 3 * 60 * 60 * 1000;
    const dateFormatter = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeZone: 'Europe/Istanbul' });

    document.querySelectorAll('[data-weekly-personal-schedule]').forEach(form => {
        const member = form.querySelector('[data-schedule-member]');
        const week = form.elements.namedItem('Week');
        const time = form.elements.namedItem('Time');
        const duration = form.elements.namedItem('DurationMinutes');
        const manualField = form.querySelector('[data-manual-schedule-field]');
        const manualWeeks = form.elements.namedItem('Weeks');
        const automaticField = form.querySelector('[data-automatic-schedule-field]');
        const automaticLength = form.querySelector('[data-automatic-schedule-length]');
        const description = form.querySelector('[data-schedule-description]');
        if (!member || !week || !time || !duration || !manualWeeks || !automaticLength) return;

        function sync() {
            const option = member.selectedOptions[0];
            const selected = Boolean(option?.value);
            const packageEnd = option?.dataset.packageEndUtc ? Date.parse(option.dataset.packageEndUtc) : null;
            const packageStart = option?.dataset.packageStartUtc ? Date.parse(option.dataset.packageStartUtc) : null;
            const isManual = selected && packageEnd === null;
            manualField.hidden = !isManual;
            manualWeeks.disabled = !isManual;
            manualWeeks.required = isManual;
            automaticField.hidden = isManual;

            if (!selected) {
                automaticLength.textContent = 'Üye seçin';
                description.textContent = 'Program süresi, seçilen üyenin paket bitişinden otomatik hesaplanır.';
                return;
            }
            if (isManual) {
                description.textContent = 'Bu eski üyenin paket bitiş tarihi kayıtlı değil. Hafta sayısını girin; ders hakkı ve çakışmalar kontrol edilir.';
                return;
            }

            const days = Array.from(form.querySelectorAll('[name="Days"]:checked')).map(input => dayNumbers[input.value]);
            description.textContent = `Paket bitişi: ${dateFormatter.format(new Date(packageEnd))}. Seçilen günler bu tarihe kadar otomatik planlanır; ders hakkı ve çakışmalar sunucuda kontrol edilir.`;
            if (!week.value || !time.value || !days.length || !duration.value) {
                automaticLength.textContent = 'Sabit günleri ve saati seçin';
                return;
            }
            const [year, month, day] = week.value.split('-').map(Number);
            const [hour, minute] = time.value.split(':').map(Number);
            const selectedDay = Date.UTC(year, month - 1, day);
            const monday = selectedDay - ((new Date(selectedDay).getUTCDay() + 6) % 7) * dayMilliseconds;
            const clockMilliseconds = (hour * 60 + minute) * 60 * 1000;
            const durationMilliseconds = Number(duration.value) * 60 * 1000;
            const scheduledWeeks = new Set();
            let lessons = 0;
            const lastClubDay = new Date(packageEnd + clubOffsetMilliseconds);
            const endDay = Date.UTC(lastClubDay.getUTCFullYear(), lastClubDay.getUTCMonth(), lastClubDay.getUTCDate());
            for (let currentMonday = monday, index = 0; currentMonday <= endDay; currentMonday += 7 * dayMilliseconds, index++) {
                for (const fixedDay of days) {
                    const start = currentMonday + ((fixedDay + 6) % 7) * dayMilliseconds + clockMilliseconds - clubOffsetMilliseconds;
                    if ((packageStart === null || start >= packageStart) && start < packageEnd && start + durationMilliseconds <= packageEnd) {
                        lessons++;
                        scheduledWeeks.add(index);
                    }
                }
            }
            automaticLength.textContent = lessons ? `${scheduledWeeks.size} hafta · ${lessons} ders` : 'Paket döneminde uygun ders yok';
        }
        form.addEventListener('change', sync);
        form.addEventListener('input', sync);
        sync();
    });
})();
