(() => {
    // Connected overlap groups share lanes; touching endpoints are not overlaps.
    function layout(events) {
        const sorted = [...events].sort((a, b) => a.start - b.start || a.end - b.end);
        let cluster = [], ends = [], clusterEnd = -1;
        const finish = () => { cluster.forEach(event => event.lanes = ends.length); cluster = []; ends = []; };
        for (const event of sorted) {
            if (event.start >= clusterEnd) finish();
            let lane = ends.findIndex(end => end <= event.start);
            if (lane < 0) lane = ends.length;
            ends[lane] = event.end;
            event.lane = lane;
            cluster.push(event);
            clusterEnd = Math.max(clusterEnd, event.end);
        }
        finish();
        return sorted;
    }
    if (typeof module !== 'undefined') module.exports = { layout };
    if (typeof document === 'undefined') return;
    document.querySelectorAll('[data-calendar-timeline]').forEach(source => {
        const days = [...source.querySelectorAll('[data-calendar-day]')];
        if (!days.length) return;
        const allEvents = days.flatMap(day => [...day.querySelectorAll('[data-calendar-start]')]);
        const first = Math.min(8 * 60, ...allEvents.map(el => Number(el.dataset.calendarStart)));
        const last = Math.max(22 * 60, ...allEvents.map(el => Number(el.dataset.calendarStart) + Number(el.dataset.calendarDuration)));
        const startHour = Math.floor(first / 60);
        const endHour = Math.min(24, Math.ceil(last / 60));
        const minuteHeight = 1.15;
        const height = (endHour - startHour) * 60 * minuteHeight;
        const region = document.createElement('section');
        region.className = 'calendar-timeline-region';
        region.setAttribute('aria-label', 'Saat çizelgesi');
        const toolbar = document.createElement('div');
        toolbar.className = 'calendar-layout-switch';
        const clockButton = document.createElement('button');
        const cardsButton = document.createElement('button');
        clockButton.type = cardsButton.type = 'button';
        clockButton.textContent = 'Saat çizelgesi'; cardsButton.textContent = 'Kart listesi';
        toolbar.append(clockButton, cardsButton);
        const hint = document.createElement('p');
        hint.textContent = 'Dersler saatlerine göre hizalanır. Detayları açmak için derse tıklayın. Aynı saatteki dersler yan yana gösterilir.';
        hint.className = 'calendar-timeline-hint';
        const scroller = document.createElement('div'); scroller.className = 'calendar-timeline-scroll';
        const grid = document.createElement('div'); grid.className = 'calendar-time-grid';
        grid.style.setProperty('--calendar-days', days.length);
        const ruler = document.createElement('div'); ruler.className = 'calendar-time-ruler';
        const zone = document.createElement('div'); zone.className = 'calendar-time-heading'; zone.textContent = 'Türkiye saati';
        ruler.append(zone);
        const scale = document.createElement('div'); scale.className = 'calendar-time-scale'; scale.style.height = `${height}px`;
        for (let hour = startHour; hour < endHour; hour++) {
            const tick = document.createElement('span'); tick.textContent = `${String(hour).padStart(2, '0')}:00`;
            tick.style.top = `${(hour - startHour) * 60 * minuteHeight}px`; scale.append(tick);
        }
        ruler.append(scale); grid.append(ruler);
        const dialog = document.createElement('dialog'); dialog.className = 'calendar-event-dialog';
        dialog.setAttribute('aria-label', 'Ders detayları');
        const close = document.createElement('button'); close.type = 'button'; close.textContent = 'Kapat'; close.className = 'calendar-dialog-close';
        const content = document.createElement('div'); dialog.append(close, content);
        close.addEventListener('click', () => dialog.close());
        dialog.addEventListener('click', event => {
            if (event.target.closest('a[href^="#"]')) dialog.close();
        });
        let active;
        dialog.addEventListener('close', () => {
            if (!active) return;
            active.parent.append(active.card);
            active.button.classList.toggle('is-selected', !!active.card.querySelector('input[type="checkbox"]:checked'));
            active.button.focus(); active = null;
        });
        for (const day of days) {
            const column = document.createElement('section'); column.className = 'calendar-time-day';
            const heading = document.createElement('div'); heading.className = 'calendar-time-heading';
            const originalHeading = day.querySelector('header');
            if (originalHeading) heading.append(originalHeading.cloneNode(true));
            const track = document.createElement('div'); track.className = 'calendar-time-track';
            track.style.height = `${height}px`; track.style.backgroundSize = `100% ${60 * minuteHeight}px`;
            const events = [...day.querySelectorAll('[data-calendar-start]')].map(parent => ({
                parent, start: Number(parent.dataset.calendarStart),
                end: Math.min(1440, Number(parent.dataset.calendarStart) + Number(parent.dataset.calendarDuration))
            }));
            for (const event of layout(events)) {
                const card = event.parent.querySelector('article'); if (!card) continue;
                const button = document.createElement('button'); button.type = 'button';
                button.className = `calendar-time-event is-${event.parent.dataset.calendarKind}`;
                button.style.top = `${(event.start - startHour * 60) * minuteHeight}px`;
                button.style.height = `${Math.max(16, (event.end - event.start) * minuteHeight - 2)}px`;
                button.style.left = `calc(${event.lane * 100 / event.lanes}% + 2px)`;
                button.style.width = `calc(${100 / event.lanes}% - 4px)`;
                const title = event.parent.dataset.calendarTitle || 'Ders';
                const time = `${String(Math.floor(event.start / 60)).padStart(2, '0')}:${String(event.start % 60).padStart(2, '0')}`;
                const kind = { personal: 'Birebir PT', group: 'Grup', consultation: 'Ön görüşme' }[event.parent.dataset.calendarKind];
                button.textContent = `${time} · ${kind}\n${title}`;
                button.setAttribute('aria-label', `${time} ${kind}: ${title}. Detayları aç`);
                button.title = `${time} ${kind}: ${title}`;
                button.addEventListener('click', () => {
                    active = { parent: event.parent, card, button };
                    content.append(card); dialog.showModal(); close.focus();
                });
                track.append(button);
            }
            column.append(heading, track); grid.append(column);
        }
        scroller.append(grid); region.append(hint, scroller);
        const navigation = source.closest('.admin-page')?.querySelector('.calendar-navigation');
        if (navigation) navigation.after(toolbar, region, source.closest('.trainer-week-scroll') ?? source);
        else source.before(toolbar, region);
        source.after(dialog);
        const select = timeline => {
            source.hidden = timeline; region.hidden = !timeline;
            source.classList.add('calendar-agenda-source');
            clockButton.setAttribute('aria-pressed', String(timeline)); cardsButton.setAttribute('aria-pressed', String(!timeline));
        };
        clockButton.addEventListener('click', () => select(true)); cardsButton.addEventListener('click', () => select(false));
        select(true);
        scroller.scrollTop = Math.max(0, (8 - startHour) * 60 * minuteHeight);
    });
})();
