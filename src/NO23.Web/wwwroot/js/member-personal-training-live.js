(() => {
    const requestSelector =
        "[data-personal-training-request]";

    const findRequestCard = requestId => {
        return Array.from(
            document.querySelectorAll(
                requestSelector
            )
        ).find(card =>
            Number(card.dataset.requestId) ===
            Number(requestId)
        );
    };

    const pad = value =>
        String(value).padStart(
            2,
            "0"
        );

    const formatDateTime =
        utcValue => {
            if (!utcValue) {
                return null;
            }

            const date =
                new Date(utcValue);

            if (
                Number.isNaN(
                    date.getTime()
                )
            ) {
                return null;
            }

            return (
                `${pad(date.getDate())}.` +
                `${pad(date.getMonth() + 1)}.` +
                `${date.getFullYear()} · ` +
                `${pad(date.getHours())}:` +
                `${pad(date.getMinutes())}`
            );
        };

    const ensureScheduledDetail =
        (card, scheduledAtUtc) => {
            const details =
                card.querySelector(
                    "[data-personal-training-details]"
                );

            if (!details) {
                return;
            }

            const formatted =
                formatDateTime(
                    scheduledAtUtc
                );

            if (!formatted) {
                return;
            }

            let row =
                details.querySelector(
                    "[data-personal-training-scheduled]"
                );

            if (!row) {
                row =
                    document.createElement(
                        "div"
                    );

                row.setAttribute(
                    "data-personal-training-scheduled",
                    ""
                );

                const label =
                    document.createElement(
                        "span"
                    );

                label.textContent =
                    "Kesin randevu";

                const value =
                    document.createElement(
                        "strong"
                    );

                value.setAttribute(
                    "data-personal-training-scheduled-value",
                    ""
                );

                row.append(
                    label,
                    value
                );

                details.prepend(
                    row
                );
            }

            const value =
                row.querySelector(
                    "[data-personal-training-scheduled-value]"
                );

            if (value) {
                value.textContent =
                    formatted;
            }
        };

    const ensureTrainerNote =
        (card, trainerNote) => {
            if (
                !trainerNote ||
                !String(trainerNote).trim()
            ) {
                return;
            }

            const details =
                card.querySelector(
                    "[data-personal-training-details]"
                );

            if (!details) {
                return;
            }

            let row =
                details.querySelector(
                    "[data-personal-training-trainer-note]"
                );

            if (!row) {
                row =
                    document.createElement(
                        "div"
                    );

                row.setAttribute(
                    "data-personal-training-trainer-note",
                    ""
                );

                const label =
                    document.createElement(
                        "span"
                    );

                label.textContent =
                    "Eğitmen notu";

                const value =
                    document.createElement(
                        "strong"
                    );

                value.setAttribute(
                    "data-personal-training-trainer-note-value",
                    ""
                );

                row.append(
                    label,
                    value
                );

                details.appendChild(
                    row
                );
            }

            const value =
                row.querySelector(
                    "[data-personal-training-trainer-note-value]"
                );

            if (value) {
                value.textContent =
                    String(
                        trainerNote
                    ).trim();
            }
        };

    const updateCancelAction =
        (card, status) => {
            const form =
                card.querySelector(
                    "[data-personal-training-cancel-form]"
                );

            if (!form) {
                return;
            }

            const normalizedStatus =
                String(status ?? "")
                    .toLowerCase();

            if (
                normalizedStatus ===
                    "rejected" ||
                normalizedStatus ===
                    "cancelled" ||
                normalizedStatus ===
                    "completed"
            ) {
                form.remove();
                return;
            }

            const button =
                form.querySelector(
                    "[data-personal-training-cancel-button]"
                );

            if (!button) {
                return;
            }

            button.textContent =
                normalizedStatus ===
                    "scheduled"
                    ? "Randevuyu İptal Et"
                    : "Talebi İptal Et";
        };

    const updateStatus =
        (card, status) => {
            const element =
                card.querySelector(
                    "[data-personal-training-status]"
                );

            if (!element) {
                return;
            }

            const value =
                String(status ?? "");

            element.textContent =
                value;

            element.dataset.status =
                value.toLowerCase();
        };

    const updateRequestCard =
        payload => {
            if (
                !payload ||
                !payload.requestId
            ) {
                return;
            }

            const card =
                findRequestCard(
                    payload.requestId
                );

            if (!card) {
                return;
            }

            updateStatus(
                card,
                payload.status
            );

            if (
                payload.scheduledAtUtc
            ) {
                ensureScheduledDetail(
                    card,
                    payload.scheduledAtUtc
                );
            }

            ensureTrainerNote(
                card,
                payload.trainerNote
            );

            updateCancelAction(
                card,
                payload.status
            );

            card.dataset.status =
                String(
                    payload.status ?? ""
                ).toLowerCase();
        };

    window.addEventListener(
        "no23:personal-training-changed",
        event => {
            updateRequestCard(
                event.detail
            );
        }
    );
})();