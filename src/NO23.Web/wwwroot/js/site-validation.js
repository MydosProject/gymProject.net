(function () {
    const fieldSelector = "input, select, textarea";

    const getFieldName = (field) => {
        const explicitLabel = field.getAttribute("aria-label");

        if (explicitLabel) {
            return explicitLabel.trim();
        }

        if (field.labels && field.labels.length > 0) {
            return field.labels[0].textContent.replace(/\s+/g, " ").trim();
        }

        return field.placeholder || field.name || "Bu alan";
    };

    const buildMessage = (field) => {
        const validity = field.validity;
        const fieldName = getFieldName(field);

        if (validity.valueMissing) {
            return `${fieldName} alanı zorunludur.`;
        }

        if (validity.typeMismatch) {
            if (field.type === "email") {
                return "Geçerli bir e-posta adresi girmelisin.";
            }

            if (field.type === "url") {
                return "Geçerli bir URL girmelisin.";
            }

            return `${fieldName} için geçerli bir değer girmelisin.`;
        }

        if (validity.badInput) {
            if (field.type === "number") {
                return `${fieldName} sayısal bir değer olmalıdır.`;
            }

            return `${fieldName} için geçerli bir değer girmelisin.`;
        }

        if (validity.rangeUnderflow && field.min) {
            return `${fieldName} en az ${field.min} olmalıdır.`;
        }

        if (validity.rangeOverflow && field.max) {
            return `${fieldName} en fazla ${field.max} olmalıdır.`;
        }

        if (validity.stepMismatch) {
            return `${fieldName} için geçerli aralıkta bir değer girmelisin.`;
        }

        if (validity.tooShort) {
            return `${fieldName} en az ${field.minLength} karakter olmalıdır.`;
        }

        if (validity.tooLong) {
            return `${fieldName} en fazla ${field.maxLength} karakter olabilir.`;
        }

        if (validity.patternMismatch) {
            return `${fieldName} için geçerli formatta bir değer girmelisin.`;
        }

        return `${fieldName} için geçerli bir değer girmelisin.`;
    };

    const applyMessage = (field) => {
        if (!field.matches(fieldSelector) || !field.validity) {
            return;
        }

        field.setCustomValidity("");

        if (!field.validity.valid) {
            field.setCustomValidity(buildMessage(field));
        }
    };

    document.addEventListener(
        "invalid",
        (event) => {
            applyMessage(event.target);
        },
        true
    );

    document.addEventListener("input", (event) => {
        const field = event.target;

        if (field.matches?.(fieldSelector)) {
            field.setCustomValidity("");
        }
    });

    document.addEventListener("change", (event) => {
        const field = event.target;

        if (field.matches?.(fieldSelector)) {
            field.setCustomValidity("");
        }
    });
})();
