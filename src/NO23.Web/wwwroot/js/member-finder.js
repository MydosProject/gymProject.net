(() => {
    document.querySelectorAll('[data-member-finder]').forEach(form => {
        const input = form.querySelector('[data-member-search]');
        const id = form.querySelector('[data-member-id]');
        const list = form.querySelector('[data-member-results]');
        const status = form.querySelector('[data-member-status]');
        const submit = form.querySelector('[data-member-submit]');
        let timer, request, results = [], active = -1, version = 0;

        function close() {
            list.hidden = true;
            input.setAttribute('aria-expanded', 'false');
            input.removeAttribute('aria-activedescendant');
            active = -1;
        }
        function select(member) {
            version++;
            clearTimeout(timer);
            request?.abort();
            id.value = member.id;
            input.value = `${member.name} · #${member.id} · ${member.phone || 'Telefon kayıtlı değil'}`;
            submit.disabled = false;
            status.textContent = 'Üye seçildi. Gelişimi göster düğmesiyle devam et.';
            close();
        }
        function highlight(index) {
            active = index;
            Array.from(list.children).forEach((item, i) => item.setAttribute('aria-selected', String(i === active)));
            const option = list.children[active];
            if (option) {
                input.setAttribute('aria-activedescendant', option.id);
                option.scrollIntoView({ block: 'nearest' });
            }
        }
        input.addEventListener('input', () => {
            id.value = '';
            submit.disabled = true;
            clearTimeout(timer);
            request?.abort();
            const current = ++version;
            close();
            const query = input.value.trim();
            if (query.length < 2) {
                status.textContent = 'En az 2 karakter yaz, ardından listeden üyeyi seç.';
                return;
            }
            if (query.length > 80) {
                status.textContent = 'Arama en fazla 80 karakter olabilir.';
                return;
            }
            status.textContent = 'Aranıyor…';
            timer = setTimeout(async () => {
                request = new AbortController();
                try {
                    const url = new URL(form.dataset.searchUrl, location.origin);
                    url.searchParams.set('query', query);
                    const response = await fetch(url, { signal: request.signal, headers: { Accept: 'application/json' } });
                    if (!response.ok || response.redirected) throw new Error('Search failed');
                    const members = await response.json();
                    if (current !== version) return;
                    results = members;
                    list.replaceChildren();
                    results.forEach((member, index) => {
                        const item = document.createElement('li');
                        item.id = `member-result-${member.id}`;
                        item.setAttribute('role', 'option');
                        item.setAttribute('aria-selected', 'false');
                        const name = document.createElement('strong');
                        name.textContent = member.name;
                        const details = document.createElement('span');
                        details.textContent = `#${member.id} · ${member.phone || 'Telefon kayıtlı değil'} · ${member.package}`;
                        item.append(name, details);
                        item.addEventListener('pointerdown', event => event.preventDefault());
                        item.addEventListener('click', () => select(member));
                        item.addEventListener('pointermove', () => highlight(index));
                        list.append(item);
                    });
                    list.hidden = results.length === 0;
                    input.setAttribute('aria-expanded', String(results.length > 0));
                    status.textContent = results.length === 0 ? 'Üye bulunamadı. Başka bir ad veya telefon dene.'
                        : `${results.length} sonuç${results.length === 20 ? ' · İlk 20 üye, daha belirgin ara' : ''}. Listeden bir üye seç.`;
                } catch (error) {
                    if (error.name !== 'AbortError' && current === version) {
                        close();
                        status.textContent = 'Arama tamamlanamadı. Yeniden dene veya üye listesini aç.';
                    }
                }
            }, 250);
        });
        input.addEventListener('keydown', event => {
            if (event.key === 'Escape') { version++; request?.abort(); clearTimeout(timer); close(); }
            if (list.hidden) return;
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                event.preventDefault();
                highlight((active + (event.key === 'ArrowDown' ? 1 : -1) + results.length) % results.length);
            }
            if (event.key === 'Enter' && active >= 0) { event.preventDefault(); select(results[active]); }
        });
        input.addEventListener('blur', () => { version++; request?.abort(); clearTimeout(timer); close(); });
        form.addEventListener('submit', event => { if (!id.value) { event.preventDefault(); input.focus(); } });
    });
})();
