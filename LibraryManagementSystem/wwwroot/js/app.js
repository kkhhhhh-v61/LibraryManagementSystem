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
// 2. reCAPTCHA Shake Logic
document.addEventListener("DOMContentLoaded", function () {

    // Find the form
    var registerForm = document.getElementById("registerForm");

    // Only run this code if the form actually exists on this page
    if (registerForm) {

        registerForm.addEventListener("submit", function (event) {

            // Check if reCAPTCHA is ticked
            // Note: grecaptcha is a global object provided by Google's script
            var captchaResponse = grecaptcha.getResponse();

            if (captchaResponse.length === 0) {
                // STOP the form submission
                event.preventDefault();

                // Find the box
                var container = document.getElementById("recaptcha-container");

                // Add shake class
                container.classList.add("shake-error");

                // Remove shake class after 0.4s (so it can be triggered again)
                setTimeout(function () {
                    container.classList.remove("shake-error");
                }, 400);
            }
        });
    }
});

};

/* ===========================================================================
   INITIALIZATION
   =========================================================================== */

$(document).ready(function () {
    initializeFormHelpers();
    initializeSidebar();
});
