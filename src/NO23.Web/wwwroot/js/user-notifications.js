(() => {
    const roots =
        Array.from(
            document.querySelectorAll(
                "[data-notification-root]"
            )
        );

    if (
        roots.length === 0 ||
        !window.signalR
    ) {
        return;
    }

    const connection =
        new signalR.HubConnectionBuilder()
            .withUrl(
                "/hubs/user-notifications"
            )
            .withAutomaticReconnect()
            .build();

    const formatDate = utcValue => {
        const date =
            new Date(utcValue);

        return new Intl.DateTimeFormat(
            "tr-TR",
            {
                day: "2-digit",
                month: "2-digit",
                hour: "2-digit",
                minute: "2-digit"
            }
        ).format(date);
    };

    const updateCount = count => {
        const value =
            Math.max(
                0,
                Number(count) || 0
            );

        for (const root of roots) {
            const badge =
                root.querySelector(
                    "[data-notification-count]"
                );

            if (!badge) {
                continue;
            }

            badge.textContent =
                value > 99
                    ? "99+"
                    : String(value);

            badge.classList.toggle(
                "d-none",
                value === 0
            );
        }
    };

    const createNotificationElement =
        notification => {
            const item =
                document.createElement("button");

            item.type = "button";

            item.className =
                "dropdown-item " +
                "notification-item " +
                "text-wrap";

            item.dataset.notificationId =
                String(notification.id);

            if (notification.url) {
                item.dataset.notificationUrl =
                    notification.url;
            }

            const title =
                document.createElement(
                    "strong"
                );

            title.className =
                "d-block";

            title.textContent =
                notification.title ?? "";

            const message =
                document.createElement(
                    "span"
                );

            message.className =
                "d-block small";

            message.textContent =
                notification.message ?? "";

            const date =
                document.createElement(
                    "small"
                );

            date.className =
                "d-block text-muted mt-1";

            date.textContent =
                formatDate(
                    notification.createdAtUtc
                );

            item.append(
                title,
                message,
                date
            );

            return item;
        };

    const renderNotifications =
        notifications => {
            for (const root of roots) {
                const list =
                    root.querySelector(
                        "[data-notification-list]"
                    );

                const empty =
                    root.querySelector(
                        "[data-notification-empty]"
                    );

                if (!list) {
                    continue;
                }

                list.replaceChildren();

                if (
                    !notifications ||
                    notifications.length === 0
                ) {
                    empty?.classList
                        .remove("d-none");

                    continue;
                }

                empty?.classList
                    .add("d-none");

                for (
                    const notification of
                    notifications
                ) {
                    list.appendChild(
                        createNotificationElement(
                            notification
                        )
                    );
                }
            }
        };

    const refreshState = async () => {
        if (
            connection.state !==
            signalR.HubConnectionState.Connected
        ) {
            return;
        }

        try {
            const [
                unreadCount,
                notifications
            ] =
                await Promise.all([
                    connection.invoke(
                        "GetUnreadCount"
                    ),
                    connection.invoke(
                        "GetRecent"
                    )
                ]);

            updateCount(
                unreadCount
            );

            renderNotifications(
                notifications
            );
        } catch {
            // Reconnect sonrası tekrar denenir.
        }
    };

    connection.on(
        "NotificationReceived",
        payload => {
            updateCount(
                payload.unreadCount
            );

            for (const root of roots) {
                const list =
                    root.querySelector(
                        "[data-notification-list]"
                    );

                const empty =
                    root.querySelector(
                        "[data-notification-empty]"
                    );

                if (!list) {
                    continue;
                }

                empty?.classList
                    .add("d-none");

                list.prepend(
                    createNotificationElement(
                        payload
                    )
                );

                while (
                    list.children.length > 10
                ) {
                    list.lastElementChild
                        ?.remove();
                }
            }
        }
    );

    connection.on(
        "NotificationStateChanged",
        payload => {
            updateCount(
                payload.unreadCount
            );

            void refreshState();
        }
    );

    connection.on(
    "PersonalTrainingChanged",
        payload => {
            window.dispatchEvent(
                new CustomEvent(
                    "no23:personal-training-changed",
                    {
                        detail: payload
                    }
                )
            );
        }
    );

    document.addEventListener(
        "click",
        async event => {
            const notificationItem =
                event.target.closest(
                    "[data-notification-id]"
                );

            if (notificationItem) {
                const notificationId =
                    Number(
                        notificationItem
                            .dataset
                            .notificationId
                    );

                const url =
                    notificationItem
                        .dataset
                        .notificationUrl;

                if (
                    notificationId &&
                    connection.state ===
                        signalR
                            .HubConnectionState
                            .Connected
                ) {
                    try {
                        await connection.invoke(
                            "MarkAsRead",
                            notificationId
                        );
                    } catch {
                        // Navigasyonu engelleme.
                    }
                }

                if (url) {
                    window.location.assign(
                        url
                    );
                }

                return;
            }

            const markAll =
                event.target.closest(
                    "[data-notification-mark-all]"
                );

            if (!markAll) {
                return;
            }

            if (
                connection.state !==
                signalR.HubConnectionState.Connected
            ) {
                return;
            }

            try {
                await connection.invoke(
                    "MarkAllAsRead"
                );
            } catch {
                // Bir sonraki senkronizasyonda
                // tekrar doğru durum alınır.
            }
        }
    );

    const startConnection =
        async () => {
            try {
                await connection.start();

                await refreshState();
            } catch {
                window.setTimeout(
                    startConnection,
                    3000
                );
            }
        };

    connection.onreconnected(
        async () => {
            await refreshState();
        }
    );

    void startConnection();
})();