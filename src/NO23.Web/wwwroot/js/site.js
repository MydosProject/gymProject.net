// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(() => {
  const passwordToggles = document.querySelectorAll("[data-password-toggle]");

  passwordToggles.forEach((toggle) => {
    const field = toggle.closest(".no23-password-field");
    const input = field?.querySelector("input");

    if (!input) {
      return;
    }

    const setVisibility = (isVisible) => {
      input.type = isVisible ? "text" : "password";
      toggle.setAttribute("aria-pressed", isVisible ? "true" : "false");
      toggle.setAttribute("aria-label", isVisible ? "Parolayı gizle" : "Parolayı göster");
    };

    toggle.addEventListener("click", () => {
      setVisibility(input.type === "password");
      input.focus();
    });
  });
})();
