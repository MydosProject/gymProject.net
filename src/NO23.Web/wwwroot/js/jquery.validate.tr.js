(function ($) {
    if (!$ || !$.validator) {
        return;
    }

    $.extend($.validator.messages, {
        required: "Bu alan zorunludur.",
        remote: "Lütfen bu alanı düzeltin.",
        email: "Geçerli bir e-posta adresi girmelisin.",
        url: "Geçerli bir URL girmelisin.",
        date: "Geçerli bir tarih girmelisin.",
        dateISO: "Geçerli bir tarih girmelisin.",
        number: "Lütfen geçerli bir sayı girin.",
        digits: "Lütfen yalnızca rakam girin.",
        creditcard: "Geçerli bir kart numarası girmelisin.",
        equalTo: "Lütfen aynı değeri tekrar girin.",
        extension: "Lütfen geçerli uzantıya sahip bir dosya seçin.",
        maxlength: $.validator.format("En fazla {0} karakter girebilirsin."),
        minlength: $.validator.format("En az {0} karakter girmelisin."),
        rangelength: $.validator.format("{0} ile {1} karakter arasında bir değer girmelisin."),
        range: $.validator.format("{0} ile {1} arasında bir değer girmelisin."),
        max: $.validator.format("En fazla {0} değerini girebilirsin."),
        min: $.validator.format("En az {0} değerini girmelisin.")
    });
})(window.jQuery);
