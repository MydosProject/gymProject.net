(() => {
    const chat =
        document.querySelector(
            "[data-trainer-chat]"
        );

    if (!chat || !window.signalR) {
        return;
    }

    const conversationId =
        Number(
            chat.dataset.conversationId
        );

    const currentUserId =
        chat.dataset.currentUserId;

    const messagesContainer =
        chat.querySelector(
            "[data-chat-messages]"
        );

    const sendForm =
        chat.querySelector(
            "[data-chat-send-form]"
        );

    const textarea =
        sendForm?.querySelector(
            'textarea[name="body"]'
        );

    const sendButton =
        sendForm?.querySelector(
            '[type="submit"]'
        );

    const errorContainer =
        chat.querySelector(
            "[data-chat-error]"
        );

    const emptyMessage =
        chat.querySelector(
            "[data-chat-empty]"
        );

    if (
        !conversationId ||
        !currentUserId ||
        !messagesContainer
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

    const scrollToBottom = () => {
        messagesContainer.scrollTop =
            messagesContainer.scrollHeight;
    };

    const formatDate = (
        utcValue
    ) => {
        const date =
            new Date(utcValue);

        return new Intl.DateTimeFormat(
            "tr-TR",
            {
                day: "2-digit",
                month: "2-digit",
                year: "numeric",
                hour: "2-digit",
                minute: "2-digit"
            }
        ).format(date);
    };

    const appendMessage = (
        message
    ) => {
        if (
            Number(message.conversationId) !==
            conversationId
        ) {
            return;
        }

        const messageId =
            String(message.messageId);

        if (
            messagesContainer.querySelector(
                `[data-message-id="${messageId}"]`
            )
        ) {
            return;
        }

        emptyMessage?.remove();

        const isMine =
            message.senderApplicationUserId ===
            currentUserId;

        const row =
            document.createElement("div");

        row.className =
            `d-flex ${
                isMine
                    ? "justify-content-end"
                    : "justify-content-start"
            }`;

        row.dataset.messageId =
            messageId;

        const bubble =
            document.createElement("div");

        bubble.className =
            "border rounded-3 px-3 py-2";

        bubble.style.maxWidth =
            "75%";

        const body =
            document.createElement("div");

        body.textContent =
            message.body ?? "";

        const time =
            document.createElement("div");

        time.className =
            "small text-muted mt-1";

        time.textContent =
            formatDate(
                message.sentAtUtc
            );

        bubble.append(
            body,
            time
        );

        row.appendChild(
            bubble
        );

        messagesContainer.appendChild(
            row
        );

        scrollToBottom();
    };

    connection.on(
        "MessageReceived",
        message => {
            appendMessage(message);
        }
    );

    const joinConversation =
        async () => {
            await connection.invoke(
                "JoinConversation",
                conversationId
            );
        };

    const startConnection =
        async () => {
            try {
                await connection.start();

                await joinConversation();
            } catch {
                window.setTimeout(
                    startConnection,
                    3000
                );
            }
        };

    connection.onreconnected(
        async () => {
            try {
                await joinConversation();
            } catch {
                // Bir sonraki reconnect
                // döngüsünde tekrar denenir.
            }
        }
    );

    sendForm?.addEventListener(
        "submit",
        async event => {
            event.preventDefault();

            if (
                !textarea ||
                !textarea.value.trim()
            ) {
                return;
            }

            errorContainer?.replaceChildren();

            sendButton?.setAttribute(
                "disabled",
                "disabled"
            );

            try {
                const response =
                    await fetch(
                        sendForm.action,
                        {
                            method: "POST",

                            body:
                                new FormData(
                                    sendForm
                                ),

                            headers: {
                                "X-Requested-With":
                                    "XMLHttpRequest"
                            }
                        }
                    );

                const payload =
                    await response.json();

                if (
                    !response.ok ||
                    !payload.succeeded
                ) {
                    if (errorContainer) {
                        errorContainer.textContent =
                            payload.message ??
                            "Mesaj gönderilemedi.";
                    }

                    return;
                }

                if (payload.message) {
                    appendMessage(
                        payload.message
                    );
                }

                textarea.value = "";
                textarea.focus();
            } catch {
                if (errorContainer) {
                    errorContainer.textContent =
                        "Bağlantı hatası oluştu. " +
                        "Lütfen tekrar dene.";
                }
            } finally {
                sendButton?.removeAttribute(
                    "disabled"
                );
            }
        }
    );

    scrollToBottom();

    void startConnection();
})();