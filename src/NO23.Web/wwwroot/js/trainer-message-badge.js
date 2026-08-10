(() => {
    const badges =
        Array.from(
            document.querySelectorAll(
                "[data-unread-message-badge]"
            )
        );

    if (
        badges.length === 0 ||
        !window.signalR
    ) {
        return;
    }

    const connection =
        new signalR.HubConnectionBuilder()
            .withUrl(
                "/hubs/trainer-chat"
            )
            .withAutomaticReconnect()
            .build();

    const updateBadges = count => {
        const unreadCount =
            Math.max(
                0,
                Number(count) || 0
            );

        const text =
            unreadCount > 99
                ? "99+"
                : String(unreadCount);

        for (const badge of badges) {
            badge.textContent =
                text;

            badge.classList.toggle(
                "d-none",
                unreadCount === 0
            );

            badge.setAttribute(
                "aria-label",
                `${unreadCount} okunmamış mesaj`
            );
        }
    };

    const refreshUnreadCount =
        async () => {
            if (
                connection.state !==
                signalR.HubConnectionState.Connected
            ) {
                return;
            }

            try {
                const count =
                    await connection.invoke(
                        "GetUnreadCount"
                    );

                updateBadges(
                    count
                );
            } catch {
                // Sonraki bağlantıda
                // tekrar senkronize edilir.
            }
        };

    connection.on(
        "RefreshUnreadCount",
        () => {
            void refreshUnreadCount();
        }
    );

    const startConnection =
        async () => {
            try {
                await connection.start();

                await refreshUnreadCount();
            } catch {
                window.setTimeout(
                    startConnection,
                    3000
                );
            }
        };

    connection.onreconnected(
        async () => {
            await refreshUnreadCount();
        }
    );

    void startConnection();
})();