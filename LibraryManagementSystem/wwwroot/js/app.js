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

//Toggle Password
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

            // 1. CHECK TEXT FIELDS (Name, Email, Password)
            var isTextFormValid = $(this).valid();

            // 2. CHECK RECAPTCHA
            var isCaptchaValid = false;
            if (typeof grecaptcha !== 'undefined') {
                var captchaResponse = grecaptcha.getResponse();
                isCaptchaValid = (captchaResponse.length > 0);
            }

            // 3. IF RECAPTCHA IS EMPTY -> SHAKE IT (Always)
            if (!isCaptchaValid) {
                var box = document.getElementById("recaptcha-container");
                if (box) {
                    // Reset animation
                    box.classList.remove("shake-error");
                    void box.offsetWidth; // Force Reflow (The magic fix)

                    // Start animation
                    box.classList.add("shake-error");
                }
            }

            // 4. FINAL DECISION
            // If either text inputs are bad OR captcha is bad, stop form.
            if (!isTextFormValid || !isCaptchaValid) {
                e.preventDefault(); // Stop submit

                if (window.showToast) {
                    // Determine which message to show
                    if (!isTextFormValid && !isCaptchaValid) {
                        window.showToast("Please fill in required fields and verify captcha.", "error");
                    } else if (!isCaptchaValid) {
                        window.showToast("Please verify you are not a robot.", "error");
                    } else {
                        window.showToast("Please fill in all required information.", "error");
                    }
                }
                return false;
            }
        });
    }
});