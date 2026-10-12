(() => {
    const initialize = root => {
        if (root.dataset.paymentMethodInitialized) return;
        root.dataset.paymentMethodInitialized = "true";
        const form = root.closest("form");
        const options = Array.from(root.querySelectorAll('input[type="radio"]'));
        const help = root.querySelector("[data-payment-method-help]");
        const synchronize = () => {
            const selected = options.find(option => option.checked && !option.disabled);
            if (!selected) {
                const fallback = options.find(option => !option.disabled);
                if (fallback) fallback.checked = true;
            }
            const isCard = options.some(option => option.checked && option.value === "CreditCard");
            form?.querySelectorAll("[data-payment-submit-label]").forEach(label => {
                label.textContent = isCard ? "Ödemeye geç" : "Siparişi oluştur";
            });
            if (help) help.textContent = isCard
                ? "Siparişini oluşturduğunda iyzico güvenli ödeme ekranına yönlendirilirsin."
                : "Nakit ve havale ödemesi kulüp tarafından onaylanır. Havale hesap bilgileri için kulüple iletişime geç.";
        };
        options.forEach(option => option.addEventListener("change", synchronize));
        synchronize();
    };
    const initializeScope = scope => {
        if (scope.matches?.("[data-payment-method-root]")) initialize(scope);
        scope.querySelectorAll?.("[data-payment-method-root]").forEach(initialize);
    };
    document.addEventListener("DOMContentLoaded", () => {
        initializeScope(document);
        new MutationObserver(mutations => mutations.forEach(mutation =>
            mutation.addedNodes.forEach(node => {
                if (node.nodeType === Node.ELEMENT_NODE) initializeScope(node);
            }))).observe(document.body, { childList: true, subtree: true });
    });
})();
