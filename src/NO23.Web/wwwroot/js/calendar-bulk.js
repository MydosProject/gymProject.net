(() => {
    const form = document.querySelector('[data-calendar-bulk]');
    if (!form) return;
    const selections = Array.from(document.querySelectorAll('input[form="calendar-bulk-form"][type="checkbox"]'));
    const allMember = form.querySelector('[name="AllMemberLessons"]');
    const scope = form.querySelector('[name="Scope"]');
    const day = form.querySelector('[name="Day"]');
    const time = form.querySelector('[name="Time"]');
    const counter = form.querySelector('[data-calendar-selection-count]');
    const error = form.querySelector('[data-calendar-bulk-error]');
    const selectAll = form.querySelector('[data-calendar-select-all]');
    const memberAvailable = form.dataset.calendarMemberAvailable === 'true';
    const selectedInputs = () => selections.filter(input => input.checked);
    const recurring = () => scope.value !== '0';
    const scopeLabel = () => scope.selectedOptions[0].textContent;
    const sync = () => {
        const selected = selectedInputs();
        const groupCount = selected.filter(input => input.name === 'GroupIds').length;
        counter.textContent = allMember.checked
            ? 'Üyenin tarih aralığındaki birebir dersleri' + (groupCount ? ` + ${groupCount} seçili grup seansı` : '') + '; toplam ders sayısı işlem sırasında belirlenir.'
            : recurring()
                ? `${selected.length} ders seçili; ${scopeLabel().toLocaleLowerCase('tr-TR')} işleme dahil. Etkilenecek toplam ders sayısı işlem sırasında belirlenir.`
                : `${selected.length} ders seçili`;
        selectAll.textContent = selections.length && selected.length === selections.length
            ? 'Görünen seçimi kaldır'
            : 'Görünen dersleri seç';
        selectAll.disabled = selections.length === 0;
    };
    const syncModes = () => {
        if (recurring()) allMember.checked = false;
        allMember.disabled = !memberAvailable || recurring();
        if (allMember.checked) scope.value = '0';
        scope.disabled = allMember.checked;
        sync();
    };
    selections.forEach(input => input.addEventListener('change', sync));
    allMember.addEventListener('change', syncModes);
    scope.addEventListener('change', syncModes);
    selectAll.addEventListener('click', () => {
        const checked = selections.some(input => !input.checked);
        selections.forEach(input => input.checked = checked);
        sync();
    });
    form.addEventListener('submit', event => {
        const deleting = event.submitter?.value === 'Delete';
        const count = selectedInputs().length;
        let message = '';
        if (!count && !allMember.checked) message = 'En az bir ders seçin.';
        else if (allMember.checked && recurring()) message = 'Üye tarih aralığı için “Yalnızca seçilen dersler” kapsamını seçin.';
        else if (!deleting && !day.value && !time.value) message = 'Yeni ders gününü veya saatini girin.';
        error.textContent = message;
        error.hidden = !message;
        if (message) {
            event.preventDefault();
            return;
        }

        const target = allMember.checked
            ? `Üyenin ${form.querySelector('[name="From"]').value} – ${form.querySelector('[name="Until"]').value} tarih aralığındaki birebir dersleri ve seçili grup seansları`
            : recurring()
                ? `${count} seçili dersin tekrarları (kapsam: ${scopeLabel()})`
                : `${count} seçili ders`;
        if (deleting) {
            const confirmation = `${target} kalıcı olarak silinsin mi?${recurring() ? '\nBu kapsamın içerdiği tüm gelecek planlanmış tekrarlar kaldırılır.' : ''}\nGrup seansları tüm katılımcılarıyla kaldırılır; ayrılmış grup ders hakları iade edilir.`;
            if (!confirm(confirmation)) event.preventDefault();
        } else if (recurring() || allMember.checked) {
            const newDay = day.value ? day.selectedOptions[0].textContent : 'Mevcut günü koru';
            const newTime = time.value || 'Mevcut saati koru';
            const confirmation = `${target} güncellensin mi?\nGün: ${newDay}\nSaat: ${newTime}${day.value ? '\nHer ders kendi haftasındaki seçilen güne taşınır.' : ''}${recurring() ? '\nBu kapsamın içerdiği tüm gelecek planlanmış tekrarlar güncellenir.' : ''}\nGrup seansları tüm katılımcıları için güncellenir.`;
            if (!confirm(confirmation)) event.preventDefault();
        }
    });
    syncModes();
})();
