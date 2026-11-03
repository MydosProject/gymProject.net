(() => {
    // Google Calendar–like admin / trainer calendar on top of FullCalendar. Every change still goes
    // through the server rules (credits, package period, conflicts); a refused move snaps back.
    const root = document.querySelector('[data-fc-calendar]');
    if (!root || typeof FullCalendar === 'undefined') return;

    const config = root.dataset;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const mobileQuery = window.matchMedia('(max-width: 767.98px)');
    const storage = {
        get(key) { try { return window.localStorage.getItem(key); } catch { return null; } },
        set(key, value) { try { window.localStorage.setItem(key, value); } catch { /* private mode */ } }
    };
    const trainers = JSON.parse(config.trainers || '[]');
    const scope = trainers.length ? 'admin' : 'trainer';
    const viewKey = `no23.calendar.${scope}.view`;
    const modeKey = `no23.calendar.${scope}.mode`;
    const hiddenKey = 'no23.calendar.admin.hiddenTrainers';
    const hidden = new Set(JSON.parse(storage.get(hiddenKey) || '[]'));
    const palette = ['#4f8ef7', '#e5734f', '#3fb27f', '#c98bdb', '#e2b33c', '#2fb8c4', '#ef5d8b', '#8d9cf2', '#a3c157', '#d98a3d'];
    const kindLabel = { personal: 'PT', group: 'Grup', consultation: 'Ön görüşme' };
    const dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

    const pad = value => String(value).padStart(2, '0');
    const localDate = date => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
    const localTime = date => `${pad(date.getHours())}:${pad(date.getMinutes())}`;
    const localIso = date => `${localDate(date)}T${localTime(date)}:00`;
    const minutesBetween = (start, end) => Math.round((end - start) / 60000);
    const mondayOf = value => {
        const date = new Date(`${value}T12:00:00`);
        date.setDate(date.getDate() - ((date.getDay() + 6) % 7));
        return localDate(date);
    };
    const normalize = text => (text ?? '').toLocaleLowerCase('tr');
    const element = (tag, className, text) => {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    };
    const button = (text, className) => {
        const node = element('button', className, text);
        node.type = 'button';
        return node;
    };
    const colorOf = trainerId => {
        const index = trainers.findIndex(trainer => trainer.id === trainerId);
        return palette[(index >= 0 ? index : Math.abs(trainerId ?? 0)) % palette.length];
    };

    // Page chrome: search, calendar / card switch, trainer list, toasts.
    const toolbar = element('div', 'fc-no23-toolbar');
    const search = element('input', 'form-control fc-no23-search');
    search.type = 'search';
    search.placeholder = 'Üye, ders veya antrenör ara';
    search.setAttribute('aria-label', 'Takvimde ara');
    const modeSwitch = element('div', 'fc-no23-mode');
    modeSwitch.setAttribute('role', 'group');
    modeSwitch.setAttribute('aria-label', 'Görünüm');
    const calendarButton = button('Takvim');
    const cardsButton = button('Kart listesi');
    modeSwitch.append(calendarButton, cardsButton);
    toolbar.append(search, modeSwitch);
    const layout = element('div', 'fc-no23-layout');
    const host = element('div', 'fc-no23-host');
    const toasts = element('div', 'fc-no23-toasts');
    toasts.setAttribute('role', 'status');
    toasts.setAttribute('aria-live', 'polite');
    if (trainers.length) layout.append(buildTrainerList());
    layout.append(host);
    root.append(toolbar, layout);
    document.body.append(toasts);

    function buildTrainerList() {
        const panel = element('details', 'fc-no23-side');
        panel.open = !mobileQuery.matches;
        panel.append(element('summary', '', 'Antrenörler'));
        const list = element('div', 'fc-no23-side-list');
        for (const trainer of trainers) {
            const label = element('label', 'fc-no23-trainer');
            const checkbox = element('input');
            checkbox.type = 'checkbox';
            checkbox.checked = !hidden.has(trainer.id);
            checkbox.addEventListener('change', () => {
                if (checkbox.checked) hidden.delete(trainer.id); else hidden.add(trainer.id);
                storage.set(hiddenKey, JSON.stringify([...hidden]));
                calendar.refetchEvents();
            });
            const swatch = element('span', 'fc-no23-swatch');
            swatch.style.background = colorOf(trainer.id);
            label.append(checkbox, swatch, element('span', '', trainer.name));
            list.append(label);
        }
        panel.append(list);
        return panel;
    }

    const notify = (message, kind = 'success', action = null) => {
        const item = element('div', `fc-no23-toast is-${kind}`);
        item.append(element('span', '', message));
        if (action) {
            const actionButton = button(action.label, 'fc-no23-toast-action');
            actionButton.addEventListener('click', () => { item.remove(); action.run(); });
            item.append(actionButton);
        }
        const close = button('×', 'fc-no23-toast-close');
        close.setAttribute('aria-label', 'Bildirimi kapat');
        close.addEventListener('click', () => item.remove());
        item.append(close);
        toasts.append(item);
        window.setTimeout(() => item.remove(), action ? 10000 : 6000);
    };

    const post = async (url, body) => {
        try {
            const response = await fetch(url, {
                method: 'POST', credentials: 'same-origin', body,
                headers: { 'Content-Type': 'application/x-www-form-urlencoded', RequestVerificationToken: token, Accept: 'application/json' }
            });
            if (!response.ok) return { succeeded: false, message: `İşlem yapılamadı (${response.status}). Sayfayı yenileyip tekrar deneyin.` };
            return await response.json();
        } catch {
            return { succeeded: false, message: 'Bağlantı kurulamadı. Tekrar deneyin.' };
        }
    };

    // Events: one request per visible range; search and trainer toggles filter the cached copy.
    let cache = { key: '', events: [] };
    const matches = event => {
        if (event.trainerId != null && hidden.has(event.trainerId)) return false;
        const query = normalize(search.value.trim());
        if (!query) return true;
        return normalize([event.title, event.trainerName, event.statusName, event.progress, ...(event.participants ?? [])].join(' ')).includes(query);
    };
    const decorate = event => {
        const color = event.kind === 'consultation' ? '#c9a15b' : colorOf(event.trainerId);
        const solid = event.kind === 'personal';
        return {
            ...event,
            backgroundColor: solid ? color : `${color}33`,
            borderColor: color,
            textColor: solid ? '#0b0e0c' : '#f6f7f2',
            classNames: [`is-${event.kind}`, event.isDone ? 'is-done' : ''].filter(Boolean)
        };
    };
    const loadEvents = async info => {
        const key = `${info.startStr}|${info.endStr}`;
        if (cache.key !== key) {
            const url = new URL(config.eventsUrl, window.location.origin);
            url.searchParams.set('start', info.startStr);
            url.searchParams.set('end', info.endStr);
            if (config.trainerId) url.searchParams.set('trainerId', config.trainerId);
            if (config.memberId) url.searchParams.set('memberId', config.memberId);
            if (config.includeCompleted === 'true') url.searchParams.set('includeCompleted', 'true');
            const response = await fetch(url, { headers: { Accept: 'application/json' }, credentials: 'same-origin' });
            if (!response.ok) throw new Error('Takvim yüklenemedi. Sayfayı yenileyin.');
            cache = { key, events: await response.json() };
        }
        return cache.events.filter(matches).map(decorate);
    };
    const refresh = () => { cache.key = ''; calendar.refetchEvents(); };
    let searchTimer;
    search.addEventListener('input', () => {
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(() => calendar.refetchEvents(), 200);
    });

    // Drag and drop / resize with undo.
    const sendMove = (props, start, end) => {
        const body = new URLSearchParams({ kind: props.kind, id: String(props.entityId), start: localIso(start) });
        if (props.kind === 'personal' && end) body.set('durationMinutes', String(minutesBetween(start, end)));
        return post(config.moveUrl, body);
    };
    const onMove = async info => {
        const props = info.event.extendedProps;
        const previous = { start: info.oldEvent.start, end: info.oldEvent.end };
        const result = await sendMove(props, info.event.start, info.event.end);
        if (!result.succeeded) {
            info.revert();
            notify(result.message, 'error');
            return;
        }
        refresh();
        notify(result.message, 'success', {
            label: 'Geri al',
            run: async () => {
                const undo = await sendMove(props, previous.start, previous.end);
                notify(undo.succeeded ? 'Değişiklik geri alındı.' : undo.message, undo.succeeded ? 'success' : 'error');
                refresh();
            }
        });
    };

    // Details: the lesson card of the calendar page (with its forms and history) inside a dialog.
    const detail = element('dialog', 'fc-no23-dialog fc-no23-detail');
    detail.setAttribute('aria-labelledby', 'fc-detail-title');
    const detailHead = element('header', 'fc-no23-dialog-head');
    const detailTitle = element('h2', '', 'Ders');
    detailTitle.id = 'fc-detail-title';
    const detailClose = button('Kapat', 'fc-no23-close');
    detailClose.addEventListener('click', () => detail.close());
    detailHead.append(detailTitle, detailClose);
    const detailBody = element('div', 'fc-no23-detail-body');
    detail.append(detailHead, detailBody);
    detail.addEventListener('click', event => { if (event.target === detail) detail.close(); });
    document.body.append(detail);

    const openDetails = async event => {
        const props = event.extendedProps;
        detailTitle.textContent = `${kindLabel[props.kind] ?? 'Ders'} · ${event.title}`;
        detailBody.replaceChildren(element('p', 'fc-no23-hint', 'Ayrıntılar yükleniyor…'));
        if (!detail.open) detail.showModal();
        try {
            const url = new URL(config.cardUrl, window.location.origin);
            url.searchParams.set('view', 'day');
            url.searchParams.set('date', localDate(event.start));
            if (config.trainerId) url.searchParams.set('trainerId', config.trainerId);
            if (config.memberId) url.searchParams.set('memberId', config.memberId);
            if (props.isDone || config.includeCompleted === 'true') url.searchParams.set('includeCompleted', 'true');
            const response = await fetch(url, { credentials: 'same-origin' });
            if (!response.ok) throw new Error();
            const page = new DOMParser().parseFromString(await response.text(), 'text/html');
            const card = page.querySelector(`[data-calendar-entry="${event.id}"] article`);
            if (!card) throw new Error();
            const nodes = [document.importNode(card, true)];
            const actions = page.querySelector(`[data-calendar-actions="${event.id}"]`);
            if (actions) nodes.push(document.importNode(actions, true));
            detailBody.replaceChildren(...nodes);
            // In-page links point at the card list; the dialog already shows that content.
            detailBody.querySelectorAll('a[href^="#"]').forEach(link => link.remove());
        } catch {
            detailBody.replaceChildren(element('p', 'fc-no23-hint', 'Ders ayrıntısı yüklenemedi. Sayfayı yenileyip tekrar deneyin.'));
        }
    };

    // Quick add: click or drag over empty time opens the create form with date and time filled in.
    const quickAdd = document.querySelector('[data-fc-quick-add]');
    const quickTitle = quickAdd?.querySelector('[data-fc-quick-title]');
    const syncDay = () => {
        const dateInput = quickAdd?.querySelector('[data-fc-panel="group"] [data-fc-date]');
        const dayInput = quickAdd?.querySelector('[data-fc-day]');
        if (dateInput?.value && dayInput) dayInput.value = dayNames[new Date(`${dateInput.value}T12:00:00`).getDay()];
    };
    const openQuickAdd = (start, end, allDay) => {
        if (!quickAdd) return;
        const begin = new Date(start);
        if (allDay) begin.setHours(9, 0, 0, 0);
        let duration = allDay ? 50 : minutesBetween(start, end);
        if (duration <= 30) duration = 50; // a single click selects one half-hour slot
        duration = Math.min(240, Math.max(15, duration));
        quickAdd.querySelectorAll('[data-fc-start]').forEach(input => { input.value = `${localDate(begin)}T${localTime(begin)}`; });
        quickAdd.querySelectorAll('[data-fc-duration]').forEach(input => { input.value = String(duration); });
        quickAdd.querySelectorAll('[data-fc-date]').forEach(input => { input.value = localDate(begin); });
        quickAdd.querySelectorAll('[data-fc-time]').forEach(input => { input.value = localTime(begin); });
        syncDay();
        const trainerSelect = quickAdd.querySelector('[data-fc-trainer]');
        if (trainerSelect && !trainerSelect.value && config.trainerId) trainerSelect.value = config.trainerId;
        if (quickTitle) quickTitle.textContent = new Intl.DateTimeFormat('tr-TR', {
            weekday: 'long', day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit'
        }).format(begin);
        quickAdd.showModal();
    };
    quickAdd?.querySelectorAll('[data-fc-tab]').forEach(tab => tab.addEventListener('click', () => {
        quickAdd.querySelectorAll('[data-fc-tab]').forEach(other => other.setAttribute('aria-selected', String(other === tab)));
        quickAdd.querySelectorAll('[data-fc-panel]').forEach(panel => { panel.hidden = panel.dataset.fcPanel !== tab.dataset.fcTab; });
    }));
    quickAdd?.querySelector('[data-fc-panel="group"] [data-fc-date]')?.addEventListener('change', syncDay);
    quickAdd?.querySelectorAll('[data-fc-close]').forEach(close => close.addEventListener('click', () => quickAdd.close()));
    quickAdd?.addEventListener('click', event => { if (event.target === quickAdd) quickAdd.close(); });

    // Views: phones get a two-day view, wider screens three days and a full week.
    const views = {
        timeGridDay: { buttonText: 'Gün' },
        timeGridTwoDay: { type: 'timeGrid', duration: { days: 2 }, buttonText: '2 gün' },
        timeGridThreeDay: { type: 'timeGrid', duration: { days: 3 }, buttonText: '3 gün' },
        timeGridWeek: { buttonText: 'Hafta' },
        dayGridMonth: { buttonText: 'Ay' },
        listTwoWeeks: { type: 'list', duration: { days: 14 }, buttonText: 'Liste' }
    };
    const wideViews = ['timeGridThreeDay', 'timeGridWeek'];
    const toolbars = mobile => mobile
        ? { headerToolbar: { left: 'prev,next', center: 'title', right: 'today' }, footerToolbar: { center: 'timeGridDay,timeGridTwoDay,dayGridMonth,listTwoWeeks' } }
        : { headerToolbar: { left: 'prev,next today', center: 'title', right: 'timeGridDay,timeGridThreeDay,timeGridWeek,dayGridMonth,listTwoWeeks' }, footerToolbar: false };
    const pickView = mobile => {
        const stored = storage.get(viewKey);
        if (stored && views[stored] && !(mobile && wideViews.includes(stored)) && !(!mobile && stored === 'timeGridTwoDay')) return stored;
        return mobile ? 'timeGridTwoDay' : 'timeGridWeek';
    };
    const turkish = {
        code: 'tr', week: { dow: 1, doy: 7 },
        buttonText: { prev: 'Önceki', next: 'Sonraki', today: 'Bugün', month: 'Ay', week: 'Hafta', day: 'Gün', list: 'Liste' },
        allDayText: 'Tüm gün', moreLinkText: 'ders daha', noEventsText: 'Bu aralıkta gösterilecek ders yok'
    };

    const calendar = new FullCalendar.Calendar(host, {
        locale: turkish,
        timeZone: 'local',
        firstDay: 1,
        initialDate: config.initialDate || undefined,
        initialView: pickView(mobileQuery.matches),
        views,
        ...toolbars(mobileQuery.matches),
        height: mobileQuery.matches ? 640 : 760,
        allDaySlot: false,
        nowIndicator: true,
        navLinks: true,
        dayMaxEvents: true,
        slotDuration: '00:30:00',
        snapDuration: '00:15:00',
        scrollTime: `${pad(Math.max(0, new Date().getHours() - 1))}:00:00`,
        slotLabelFormat: { hour: '2-digit', minute: '2-digit', hour12: false },
        eventTimeFormat: { hour: '2-digit', minute: '2-digit', hour12: false },
        editable: true,
        eventResizableFromStart: false,
        selectable: Boolean(quickAdd),
        selectMirror: true,
        longPressDelay: 350,
        events: (info, success, failure) => {
            loadEvents(info).then(success).catch(error => { notify(error.message, 'error'); failure(error); });
        },
        eventContent: arg => {
            const props = arg.event.extendedProps;
            const box = element('div', 'fc-no23-event');
            if (!arg.view.type.startsWith('list') && arg.timeText) box.append(element('span', 'fc-no23-event-time', arg.timeText));
            const seats = props.kind === 'group' ? ` (${props.reservedCount}/${props.capacity})` : '';
            box.append(element('strong', 'fc-no23-event-title', `${kindLabel[props.kind] ?? ''} · ${arg.event.title}${seats}`));
            if (props.trainerName && !arg.view.type.startsWith('dayGrid')) box.append(element('span', 'fc-no23-event-meta', props.trainerName));
            return { domNodes: [box] };
        },
        eventDidMount: arg => {
            const props = arg.event.extendedProps;
            arg.el.title = [
                `${kindLabel[props.kind] ?? ''} · ${arg.event.title}`, props.trainerName, props.statusName, props.progress,
                props.kind === 'group' ? `Katılımcı ${props.reservedCount}/${props.capacity}` : null
            ].filter(Boolean).join('\n');
        },
        eventClick: info => { info.jsEvent.preventDefault(); openDetails(info.event); },
        eventAllow: span => span.start >= new Date(),
        eventDrop: onMove,
        eventResize: onMove,
        select: info => { calendar.unselect(); openQuickAdd(info.start, info.end, info.allDay); },
        datesSet: info => {
            storage.set(viewKey, info.view.type);
            const current = localDate(calendar.getDate());
            const url = new URL(window.location.href);
            url.searchParams.set('date', current);
            url.searchParams.delete('week');
            window.history.replaceState(null, '', url);
            const dateField = document.getElementById('calendar-date');
            if (dateField) dateField.value = current;
        },
        loading: busy => root.classList.toggle('is-loading', busy)
    });

    mobileQuery.addEventListener('change', event => {
        const options = toolbars(event.matches);
        calendar.setOption('headerToolbar', options.headerToolbar);
        calendar.setOption('footerToolbar', options.footerToolbar);
        calendar.setOption('height', event.matches ? 640 : 760);
        if (event.matches && wideViews.includes(calendar.view.type)) calendar.changeView('timeGridTwoDay');
        if (!event.matches && calendar.view.type === 'timeGridTwoDay') calendar.changeView('timeGridWeek');
    });

    // The server-rendered card list stays available for bulk selection of one week.
    const cards = [...document.querySelectorAll('[data-fc-cards]')];
    const setMode = showCards => {
        layout.hidden = showCards;
        search.hidden = showCards;
        cards.forEach(card => { card.hidden = !showCards; });
        calendarButton.setAttribute('aria-pressed', String(!showCards));
        cardsButton.setAttribute('aria-pressed', String(showCards));
        storage.set(modeKey, showCards ? 'cards' : 'calendar');
        if (!showCards) calendar.updateSize();
    };
    calendarButton.addEventListener('click', () => setMode(false));
    cardsButton.addEventListener('click', () => {
        const current = localDate(calendar.getDate());
        if (config.initialDate && mondayOf(current) !== mondayOf(config.initialDate)) {
            storage.set(modeKey, 'cards');
            const url = new URL(window.location.href);
            url.searchParams.set('date', current);
            window.location.assign(url);
            return;
        }
        setMode(true);
    });

    document.querySelectorAll('.calendar-navigation-bar, .calendar-navigation-hint').forEach(node => { node.hidden = true; });
    calendar.render();
    setMode(storage.get(modeKey) === 'cards');
})();
