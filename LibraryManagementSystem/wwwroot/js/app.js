﻿$("[type=reset]").on("click", (e) => {
    e.preventDefault();
    location = location;
});

$("[data-trim]").on("change", (e) => {
    e.target.value = e.target.value.trim();
});

$(document).ready(function () {
    const $body = $("body");
    const $logo = $(".logo");

    const isCollapsed = localStorage.getItem("sidebar-collapsed") === "true";
    if (isCollapsed) {
        $body.addClass("layout--collapsed");
    }

    $logo.on("click", function (e) {
        e.preventDefault();
        $body.toggleClass("layout--collapsed");
        const hasClass = $body.hasClass("layout--collapsed");
        localStorage.setItem("sidebar-collapsed", hasClass);
    });
});

/**
 * Displays a global toast notification.
 * @param {string} message - The message to display.
 * @param {string} type - 'success', 'error', 'warning', or 'info' (default).
 * @param {number} duration - Duration in ms (default 3000).
 */
window.showToast = (message, type = "info", duration = 3000) => {
    const $container = $("#toast-container");

    // 1. Create elements
    const $toast = $("<div>").addClass(`toast toast--${type}`);
    const $message = $("<span>").text(message);
    const $closeButton = $(`
        <div class="toast__close" role="button" aria-label="Close">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" class="toast__close-icon">
                <path d="m256-200-56-56 224-224-224-224 56-56 224 224 224-224 56 56-224 224 224 224-56 56-224-224-224 224Z"/>
            </svg>
        </div>
    `);

    // 2. Assemble toast
    $toast.append($message).append($closeButton);

    $container.append($toast);

    // 3. Animate in
    void $toast[0].offsetWidth;
    $toast.addClass("toast--visible");

    // 4. Set up removal logic
    const hideToast = () => {
        $toast.removeClass("toast--visible");
        setTimeout(() => $toast.remove(), 300);
    };

    const timeoutId = setTimeout(hideToast, duration);

    $closeButton.on("click", () => {
        clearTimeout(timeoutId);
        hideToast();
    });
};
// Toggle Password Visibility
function togglePassword(inputId, icon) {
    var input = document.getElementById(inputId);

    if (input.type === "password") {
        input.type = "text";
        icon.classList.remove("fa-eye");
        icon.classList.add("fa-eye-slash");
    } else {
        input.type = "password";
        icon.classList.remove("fa-eye-slash");
        icon.classList.add("fa-eye");
    }
}

