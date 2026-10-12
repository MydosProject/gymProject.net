(() => {
    const form = document.querySelector('[data-calendar-bulk]');
    if (!form) return;
    const selections = Array.from(document.querySelectorAll('input[form="calendar-bulk-form"][type="checkbox"]'));
    const allMember = form.querySelector('[name="AllMemberLessons"]');
    const counter = form.querySelector('[data-calendar-selection-count]');
    const error = form.querySelector('[data-calendar-bulk-error]');
    const sync = () => {
        const groupCount = selections.filter(input => input.checked && input.name === 'GroupIds').length;
        counter.textContent = allMember.checked
            ? 'Üyenin tarih aralığındaki birebir dersleri' + (groupCount ? ` + ${groupCount} grup seansı` : '')
            : `${selections.filter(input => input.checked).length} ders seçili`;
    };
    selections.forEach(input => input.addEventListener('change', sync));
    allMember.addEventListener('change', sync);
    form.querySelector('[data-calendar-select-all]').addEventListener('click', () => {
        const checked = selections.some(input => !input.checked);
        selections.forEach(input => input.checked = checked);
        sync();
    });
    form.addEventListener('submit', event => {
        const deleting = event.submitter?.value === 'Delete';
        const count = selections.filter(input => input.checked).length;
        let message = '';
        if (!count && !allMember.checked) message = 'En az bir ders seçin.';
        if (!deleting && !form.querySelector('[name="Time"]').value) message = 'Yeni ders saatini girin.';
        error.textContent = message;
        error.hidden = !message;
        if (message) event.preventDefault();
        else if (deleting && !confirm('Seçilen planlanmış dersler kalıcı olarak silinsin mi? Seçili grup seansları tüm katılımcılarıyla kaldırılır; ders hakları iade edilir.')) event.preventDefault();
    });
    sync();
})();
