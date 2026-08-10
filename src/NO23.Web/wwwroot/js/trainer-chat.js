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

    const typingIndicator =
        chat.querySelector(
            "[data-chat-typing]"
        );

    if (
        !conversationId ||
        !currentUserId ||
        !messagesContainer
    ) {
        return;
    }

    /*
     * Aynı trainer-chat.js dosyası hem
     * Trainer hem Member mesaj ekranında
     * kullanılıyor.
     *
     * Burada bulunduğumuz ekranı tespit edip
     * dinamik mesajlarda doğru CSS class'larını
     * kullanıyoruz.
     */
    const isTrainerPage =
        Boolean(
            document.querySelector(
                ".trainer-messages-page"
            )
        );

    const messagePrefix =
        isTrainerPage
            ? "trainer"
            : "member";

    const connection =
        new signalR.HubConnectionBuilder()
            .withUrl(
                "/hubs/trainer-chat"
            )
            .withAutomaticReconnect()
            .build();

    let typingTimeout = null;
    let typingSent = false;


    const scrollToBottom = () => {
        messagesContainer.scrollTop =
            messagesContainer.scrollHeight;
    };


    const formatDate = utcValue => {
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


    const setTypingState =
        async isTyping => {
            if (
                connection.state !==
                signalR.HubConnectionState.Connected
            ) {
                return;
            }

            if (typingSent === isTyping) {
                return;
            }

            typingSent = isTyping;

            try {
                await connection.invoke(
                    "SetTyping",
                    conversationId,
                    isTyping
                );
            } catch {
                // Typing durumu kritik değildir.
            }
        };


    const stopTypingLater = () => {
        window.clearTimeout(
            typingTimeout
        );

        typingTimeout =
            window.setTimeout(
                () => {
                    void setTypingState(
                        false
                    );
                },
                1200
            );
    };


    const markConversationAsRead =
        async () => {
            if (
                connection.state !==
                signalR.HubConnectionState.Connected
            ) {
                return;
            }

            try {
                await connection.invoke(
                    "MarkConversationAsRead",
                    conversationId
                );
            } catch {
                // Read state daha sonra
                // tekrar senkronize olur.
            }
        };


    /*
     * Yeni mesaj SignalR veya AJAX üzerinden
     * geldiğinde Razor tarafındaki mevcut
     * tasarımla aynı DOM yapısını üretir.
     */
    const appendMessage = message => {
        if (
            Number(
                message.conversationId
            ) !== conversationId
        ) {
            return;
        }

        const messageId =
            String(
                message.messageId
            );

        if (
            messagesContainer.querySelector(
                `[data-message-id="${messageId}"]`
            )
        ) {
            return;
        }

        /*
         * İlk mesaj gönderiliyorsa
         * "Henüz mesaj yok" alanını kaldır.
         */
        const currentEmptyMessage =
            messagesContainer.querySelector(
                "[data-chat-empty]"
            );

        currentEmptyMessage?.remove();


        const isMine =
            message.senderApplicationUserId ===
            currentUserId;


        /*
         * Razor tarafında mesajlar:
         *
         * .trainer-message-list
         * veya
         * .member-message-list
         *
         * içerisinde tutuluyor.
         *
         * İlk mesajsa listeyi JS oluşturur.
         */
        let messageList =
            messagesContainer.querySelector(
                `.${messagePrefix}-message-list`
            );

        if (!messageList) {
            messageList =
                document.createElement(
                    "div"
                );

            messageList.className =
                `${messagePrefix}-message-list`;

            messagesContainer.appendChild(
                messageList
            );
        }


        /*
         * MESAJ SATIRI
         */
        const row =
            document.createElement(
                "div"
            );

        row.className =
            `${messagePrefix}-message-row ${
                isMine
                    ? "is-mine"
                    : "is-theirs"
            }`;

        row.dataset.messageId =
            messageId;


        /*
         * MESAJ BALONU
         */
        const bubble =
            document.createElement(
                "div"
            );

        bubble.className =
            `${messagePrefix}-message-bubble`;


        /*
         * MESAJ METNİ
         */
        const body =
            document.createElement(
                "div"
            );

        body.className =
            `${messagePrefix}-message-body`;

        body.textContent =
            message.body ?? "";


        /*
         * TARİH / SAAT / READ STATE
         */
        const meta =
            document.createElement(
                "div"
            );

        meta.className =
            `${messagePrefix}-message-time`;


        const time =
            document.createElement(
                "span"
            );

        time.textContent =
            formatDate(
                message.sentAtUtc
            );

        meta.appendChild(
            time
        );


        /*
         * Mesajı gönderen mevcut kullanıcıysa
         * Gönderildi / Görüldü durumu eklenir.
         */
        if (isMine) {
            const readState =
                document.createElement(
                    "span"
                );

            readState.className =
                `${messagePrefix}-message-read-state`;

            readState.dataset
                .messageReadState = "";

            readState.textContent =
                " · Gönderildi";

            meta.appendChild(
                readState
            );
        }


        bubble.append(
            body,
            meta
        );

        row.appendChild(
            bubble
        );

        messageList.appendChild(
            row
        );

        scrollToBottom();
    };


    /*
     * SIGNALR - YENİ MESAJ
     */
    connection.on(
        "MessageReceived",
        message => {
            appendMessage(message);

            const isIncoming =
                message
                    .senderApplicationUserId !==
                currentUserId;

            if (
                isIncoming &&
                document.visibilityState ===
                    "visible"
            ) {
                void markConversationAsRead();
            }
        }
    );


    /*
     * SIGNALR - MESAJLAR OKUNDU
     */
    connection.on(
        "MessagesRead",
        payload => {
            if (
                Number(
                    payload.conversationId
                ) !== conversationId
            ) {
                return;
            }

            if (
                payload
                    .readerApplicationUserId ===
                currentUserId
            ) {
                return;
            }

            for (
                const messageId of
                payload.messageIds ?? []
            ) {
                const readState =
                    messagesContainer
                        .querySelector(
                            `[data-message-id="${messageId}"] ` +
                            "[data-message-read-state]"
                        );

                if (readState) {
                    readState.textContent =
                        " · Görüldü";
                }
            }
        }
    );


    /*
     * SIGNALR - YAZIYOR...
     */
    connection.on(
        "TypingChanged",
        payload => {
            if (
                Number(
                    payload.conversationId
                ) !== conversationId
            ) {
                return;
            }

            if (
                payload.userId ===
                currentUserId
            ) {
                return;
            }

            if (!typingIndicator) {
                return;
            }

            typingIndicator
                .classList
                .toggle(
                    "d-none",
                    !payload.isTyping
                );

            if (payload.isTyping) {
                scrollToBottom();
            }
        }
    );


    /*
     * Konuşmaya SignalR grubundan katıl.
     */
    const joinConversation =
        async () => {
            await connection.invoke(
                "JoinConversation",
                conversationId
            );

            await markConversationAsRead();
        };


    /*
     * SignalR bağlantısını başlat.
     */
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


    /*
     * Bağlantı kopup tekrar kurulursa
     * konuşmaya yeniden katıl.
     */
    connection.onreconnected(
        async () => {
            try {
                await joinConversation();
            } catch {
                // Sonraki reconnect
                // turunda tekrar denenir.
            }
        }
    );


    /*
     * Kullanıcı yazmaya başladığında
     * karşı tarafa typing durumu gönder.
     */
    textarea?.addEventListener(
        "input",
        () => {
            const hasText =
                Boolean(
                    textarea.value.trim()
                );

            if (!hasText) {
                window.clearTimeout(
                    typingTimeout
                );

                void setTypingState(
                    false
                );

                return;
            }

            void setTypingState(
                true
            );

            stopTypingLater();
        }
    );


    /*
     * Textarea focus kaybederse
     * yazıyor durumunu kapat.
     */
    textarea?.addEventListener(
        "blur",
        () => {
            window.clearTimeout(
                typingTimeout
            );

            void setTypingState(
                false
            );
        }
    );


    /*
     * Kullanıcı sekmeye geri döndüğünde
     * konuşmayı okunmuş olarak işaretle.
     */
    document.addEventListener(
        "visibilitychange",
        () => {
            if (
                document.visibilityState ===
                "visible"
            ) {
                void markConversationAsRead();
            }
        }
    );


    /*
     * MESAJ GÖNDERME
     */
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

            window.clearTimeout(
                typingTimeout
            );

            await setTypingState(
                false
            );

            errorContainer
                ?.replaceChildren();

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
                    if (
                        errorContainer
                    ) {
                        errorContainer
                            .textContent =
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
                sendButton
                    ?.removeAttribute(
                        "disabled"
                    );
            }
        }
    );


    scrollToBottom();

    void startConnection();
})();