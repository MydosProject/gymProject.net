// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", () => {
    const locationDataUrl = "/data/turkey-provinces-districts.json";
    let locationDataPromise;

    const loadLocationData = () => {
        locationDataPromise ??= fetch(locationDataUrl, {
            headers: { "Accept": "application/json" }
        }).then(response => {
            if (!response.ok) {
                throw new Error(`İl-ilçe listesi yüklenemedi (${response.status}).`);
            }

            return response.json();
        });

        return locationDataPromise;
    };

    const appendOption = (select, value, label = value) => {
        const option = document.createElement("option");
        option.value = value;
        option.textContent = label;
        select.append(option);
    };

    const populateDistricts = (root, selectedDistrict = "") => {
        const citySelect = root.querySelector("[data-turkey-city]");
        const districtSelect = root.querySelector("[data-turkey-district]");
        const provinces = root._turkeyLocationData;

        if (!citySelect || !districtSelect || !provinces) {
            return;
        }

        const province = provinces.find(item => item.city === citySelect.value);
        districtSelect.replaceChildren();

        if (!province) {
            appendOption(districtSelect, "", "Önce il seçin");
            districtSelect.disabled = true;
            return;
        }

        appendOption(districtSelect, "", "İlçe seçin");
        province.districts.forEach(district =>
            appendOption(districtSelect, district));

        if (province.districts.includes(selectedDistrict)) {
            districtSelect.value = selectedDistrict;
        }

        districtSelect.disabled = citySelect.disabled;
    };

    const syncLocationRoot = root => {
        if (!root?.matches?.("[data-turkey-location-root]")) {
            return;
        }

        const districtSelect = root.querySelector("[data-turkey-district]");
        const citySelect = root.querySelector("[data-turkey-city]");

        if (!districtSelect || !citySelect || !root._turkeyLocationData) {
            return;
        }

        if (!citySelect.value) {
            districtSelect.disabled = true;
        } else if (!citySelect.disabled) {
            districtSelect.disabled = false;
        }
    };

    const initializeLocationRoot = async root => {
        if (root.dataset.turkeyLocationInitialized) {
            return;
        }

        root.dataset.turkeyLocationInitialized = "loading";
        const citySelect = root.querySelector("[data-turkey-city]");
        const districtSelect = root.querySelector("[data-turkey-district]");

        if (!citySelect || !districtSelect) {
            root.dataset.turkeyLocationInitialized = "invalid";
            return;
        }

        const selectedCity = citySelect.dataset.selectedValue ?? "";
        const selectedDistrict = districtSelect.dataset.selectedValue ?? "";

        try {
            const provinces = await loadLocationData();
            root._turkeyLocationData = provinces;
            citySelect.replaceChildren();
            appendOption(citySelect, "", "İl seçin");
            provinces.forEach(province =>
                appendOption(citySelect, province.city));

            if (provinces.some(province => province.city === selectedCity)) {
                citySelect.value = selectedCity;
            }

            populateDistricts(root, selectedDistrict);
            citySelect.addEventListener("change", () =>
                populateDistricts(root));
            root.dataset.turkeyLocationInitialized = "true";
            syncLocationRoot(root);
        } catch (error) {
            root.dataset.turkeyLocationInitialized = "error";
            citySelect.replaceChildren();
            districtSelect.replaceChildren();
            appendOption(citySelect, "", "İl listesi yüklenemedi");
            appendOption(districtSelect, "", "İlçe listesi yüklenemedi");
            citySelect.disabled = true;
            districtSelect.disabled = true;
            console.error(error);
        }
    };

    const initializeDeliveryMethodRoot = root => {
        if (root.dataset.deliveryMethodInitialized) {
            return;
        }

        const form = root.closest("form");
        const radios = Array.from(
            root.querySelectorAll('input[type="radio"]'));

        if (!form || radios.length === 0) {
            return;
        }

        root.dataset.deliveryMethodInitialized = "true";
        const addressFields = Array.from(
            form.querySelectorAll("[data-address-delivery-field]"));

        const syncDeliveryFields = () => {
            const selected = radios.find(radio => radio.checked);
            const isClubPickup = selected?.value === "ClubPickup";

            addressFields.forEach(field => {
                field.hidden = isClubPickup;
                field.querySelectorAll("input, textarea, select")
                    .forEach(control => {
                        control.disabled = isClubPickup;
                    });
            });

            syncLocationRoot(form);
        };

        radios.forEach(radio =>
            radio.addEventListener("change", syncDeliveryFields));
        syncDeliveryFields();
    };

    const initializeScope = scope => {
        if (scope.matches?.("[data-turkey-location-root]")) {
            void initializeLocationRoot(scope);
        }

        scope.querySelectorAll?.("[data-turkey-location-root]")
            .forEach(root => void initializeLocationRoot(root));

        if (scope.matches?.("[data-delivery-method-root]")) {
            initializeDeliveryMethodRoot(scope);
        }

        scope.querySelectorAll?.("[data-delivery-method-root]")
            .forEach(initializeDeliveryMethodRoot);
    };

    initializeScope(document);

    new MutationObserver(mutations => {
        mutations.forEach(mutation => {
            mutation.addedNodes.forEach(node => {
                if (node.nodeType === Node.ELEMENT_NODE) {
                    initializeScope(node);
                }
            });
        });
    }).observe(document.body, { childList: true, subtree: true });
});

document.addEventListener("DOMContentLoaded", () => {
    let activeOpener = null;

    document.addEventListener("click", event => {
        const opener = event.target.closest?.("[data-content-modal-open]");

        if (opener) {
            const dialogId = opener.dataset.contentModalOpen;
            const dialog = dialogId ? document.getElementById(dialogId) : null;

            if (dialog && typeof dialog.showModal === "function") {
                event.preventDefault();
                activeOpener = opener;
                dialog.showModal();
                document.body.classList.add("has-content-reading-modal");
            }

            return;
        }

        const closeButton = event.target.closest?.("[data-content-modal-close]");

        if (closeButton) {
            closeButton.closest("[data-content-modal]")?.close();
        }
    });

    document.querySelectorAll("[data-content-modal]").forEach(dialog => {
        dialog.addEventListener("click", event => {
            if (event.target === dialog) {
                dialog.close();
            }
        });

        dialog.addEventListener("close", () => {
            if (!document.querySelector("[data-content-modal][open]")) {
                document.body.classList.remove("has-content-reading-modal");
            }

            activeOpener?.focus();
            activeOpener = null;
        });
    });
});
