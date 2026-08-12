(() => {
    const roots =
        Array.from(
            document.querySelectorAll(
                "[data-notification-root]"
            )
        );

    if (roots.length === 0) {
        return;
    }

    const hasSignalR =
        Boolean(window.signalR);

    const connection =
        hasSignalR
            ? new signalR.HubConnectionBuilder()
                .withUrl(
                    "/hubs/user-notifications"
                )
                .withAutomaticReconnect()
                .build()
            : null;

    const isConnected = () =>
        connection &&
        connection.state ===
            signalR.HubConnectionState.Connected;

    const getAntiForgeryToken = () =>
        document.querySelector(
            'input[name="__RequestVerificationToken"]'
        )?.value ?? "";

    const postNotificationAction =
        async (url, formEntries = {}) => {
            const body =
                new URLSearchParams();

            const token =
                getAntiForgeryToken();

            if (token) {
                body.set(
                    "__RequestVerificationToken",
                    token
                );
            }

            for (const [key, value] of
                Object.entries(formEntries)) {
                body.set(
                    key,
                    String(value)
                );
            }

            const response =
                await fetch(
                    url,
                    {
                        method: "POST",
                        credentials: "same-origin",
                        headers: {
                            "Content-Type":
                                "application/x-www-form-urlencoded; charset=UTF-8",
                            "X-Requested-With":
                                "XMLHttpRequest"
                        },
                        body
                    }
                );

            if (!response.ok) {
                throw new Error(
                    "Notification request failed."
                );
            }

            return await response.json();
        };

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

    const markRenderedNotificationRead =
        notificationId => {
            document
                .querySelectorAll(
                    `[data-notification-id="${notificationId}"]`
                )
                .forEach(item => {
                    item.classList.remove(
                        "is-unread"
                    );
                    item.classList.add(
                        "is-read"
                    );
                });
        };

    const markAllRenderedNotificationsRead =
        () => {
            document
                .querySelectorAll(
                    "[data-notification-id]"
                )
                .forEach(item => {
                    item.classList.remove(
                        "is-unread"
                    );
                    item.classList.add(
                        "is-read"
                    );
                });
        };

    const createNotificationElement =
        notification => {
            const item =
                document.createElement("button");

            item.type = "button";

            item.className =
                "no23-notification-item " +
                (notification.isRead ||
                notification.readAtUtc
                    ? "is-read"
                    : "is-unread");

            item.dataset.notificationId =
                String(notification.id);

            if (notification.url) {
                item.dataset.notificationUrl =
                    notification.url;
            }

            const marker =
                document.createElement("span");

            marker.className =
                "no23-notification-marker";
            marker.setAttribute(
                "aria-hidden",
                "true"
            );

            const copy =
                document.createElement("span");

            copy.className =
                "no23-notification-copy";

            const title =
                document.createElement(
                    "strong"
                );

            title.textContent =
                notification.title ?? "";

            const message =
                document.createElement(
                    "span"
                );

            message.textContent =
                notification.message ?? "";

            const date =
                document.createElement(
                    "small"
                );

            date.textContent =
                formatDate(
                    notification.createdAtUtc
                );

            copy.append(
                title,
                message,
                date
            );

            item.append(
                marker,
                copy
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
        if (!isConnected()) {
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

    const markAsRead =
        async notificationId => {
            if (isConnected()) {
                try {
                    await connection.invoke(
                        "MarkAsRead",
                        notificationId
                    );

                    const unreadCount =
                        await connection.invoke(
                            "GetUnreadCount"
                        );

                    updateCount(
                        unreadCount
                    );
                    markRenderedNotificationRead(
                        notificationId
                    );
                    return;
                } catch {
                    // HTTP fallback kalıcı okundu bilgisini dener.
                }
            }

            const payload =
                await postNotificationAction(
                    "/Notifications/MarkAsRead",
                    {
                        notificationId
                    }
                );

            updateCount(
                payload.unreadCount
            );
            markRenderedNotificationRead(
                notificationId
            );
        };

    const markAllAsRead =
        async () => {
            if (isConnected()) {
                try {
                    await connection.invoke(
                        "MarkAllAsRead"
                    );

                    updateCount(0);
                    markAllRenderedNotificationsRead();
                    return;
                } catch {
                    // HTTP fallback kalıcı okundu bilgisini dener.
                }
            }

            const payload =
                await postNotificationAction(
                    "/Notifications/MarkAllAsRead"
                );

            updateCount(
                payload.unreadCount
            );
            markAllRenderedNotificationsRead();
        };

    connection?.on(
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

    connection?.on(
        "NotificationStateChanged",
        payload => {
            updateCount(
                payload.unreadCount
            );

            void refreshState();
        }
    );

    connection?.on(
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

                if (notificationId) {
                    try {
                        await markAsRead(
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

            event.preventDefault();

            try {
                await markAllAsRead();
            } catch {
                // Bir sonraki senkronizasyonda
                // tekrar doğru durum alınır.
            }
        }
    );

    const startConnection =
        async () => {
            if (!connection) {
                return;
            }

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

    connection?.onreconnected(
        async () => {
            await refreshState();
        }
    );

    void startConnection();
})();
