﻿/* ===========================================================================
   GLOBAL FORM HELPERS
   =========================================================================== */

function initializeFormHelpers() {
    $("[type=reset]").on("click", (e) => {
        e.preventDefault();
        location.reload();
    });

    $("[data-trim]").on("change", (e) => {
        e.target.value = e.target.value.trim();
    });
}

/* ===========================================================================
   SIDEBAR MANAGEMENT
   =========================================================================== */

function initializeSidebar() {
    const $body = $("body");
    const $logo = $(".logo");
    const STORAGE_KEY = "sidebar-collapsed";

    if (localStorage.getItem(STORAGE_KEY) === "true") {
        $body.addClass("layout--collapsed");
    }

    $logo.on("click", function (e) {
        e.preventDefault();
        $body.toggleClass("layout--collapsed");
        localStorage.setItem(STORAGE_KEY, $body.hasClass("layout--collapsed"));
    });
}

/* ===========================================================================
   UI COMPONENTS
   =========================================================================== */

window.showToast = (message, type = "info", duration = 5000) => {
    const $container = $("#toast-container");

    const $toast = $("<div>").addClass(`toast toast--${type}`);
    const $message = $("<span>").text(message);
    const $closeButton = $(`
        <div class="toast__close" role="button" aria-label="Close">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" class="toast__close-icon">
                <path d="m256-200-56-56 224-224-224-224 56-56 224 224 224-224 56 56-224 224 224 224-56 56-224-224-224 224Z"/>
            </svg>
        </div>
    `);

    $toast.append($message).append($closeButton);
    $container.append($toast);

    requestAnimationFrame(() => {
        $toast.addClass("toast--visible");
    });

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

window.togglePassword = (inputId, icon) => {
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
/**
 * Checks if the page was loaded via a "Back" button.
 * If not, it displays the toast.
 */
window.showToastOnLoad = (message, type) => {
    let isBackNavigation = false;

    // Check performance entries
    if (window.performance && window.performance.getEntriesByType) {
        const navEntries = window.performance.getEntriesByType("navigation");
        if (navEntries.length > 0 && navEntries[0].type === "back_forward") {
            isBackNavigation = true;
        }
    }
    // Fallback for older browsers
    else if (window.performance && window.performance.navigation) {
        if (window.performance.navigation.type === 2) {
            isBackNavigation = true;
        }
    }

    // Only show if it's a fresh load
    if (!isBackNavigation && message) {
        window.showToast(message, type);
    }
};

/* ===========================================================================
   INITIALIZATION
   =========================================================================== */

$(document).ready(function () {
    initializeFormHelpers();
    initializeSidebar();
});

/* ===========================================================================
   INITIALIZATION
   =========================================================================== */

$(document).ready(function () {
    if (typeof initializeFormHelpers === "function") initializeFormHelpers();
    if (typeof initializeSidebar === "function") initializeSidebar();
});

/* ===========================================================================
   REGISTER VALIDATION (FIELDS + RECAPTCHA)
   =========================================================================== */

$(document).ready(function () {
    const $form = $("#registerForm");

    if ($form.length) {
        $form.on("submit", function (e) {

            // 1. CHECK FIELDS: Are name/email/pass filled correctly?
            if (!$(this).valid()) {
                e.preventDefault(); 

                // Add shake effect to the input fields that have errors
                $(".input-validation-error").addClass("shake-error");

                // Remove the shake class after 500ms so it can shake again later
                setTimeout(() => $(".input-validation-error").removeClass("shake-error"), 500);

                // Show the toast message 
                if (window.showToast) {
                    window.showToast("Please fill in all required information.", "error");
                }
                return false;
            }

            // 2. CHECK RECAPTCHA: Is it ticked?
            const captchaResponse = grecaptcha.getResponse();

            if (captchaResponse.length === 0) {
                e.preventDefault(); 

                const container = document.getElementById("recaptcha-container");

                container.classList.remove("shake-error");
                void container.offsetWidth; 
                container.classList.add("shake-error");

                if (window.showToast) {
                    window.showToast("Please complete the captcha verification.", "error");
                }
                return false;
            }

        });
    }
});
