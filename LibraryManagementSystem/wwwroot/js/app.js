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
};
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
   UNSAVED CHANGES WARNING
   =========================================================================== */

function initializeUnsavedChangesWarning() {
    const forms = document.querySelectorAll("form.js-warn-unsaved");

    forms.forEach((form) => {
        let isDirty = false;

        // Track changes
        form.addEventListener("change", () => (isDirty = true));
        form.addEventListener("input", () => (isDirty = true));

        // Allow submit without warning
        form.addEventListener("submit", () => {
            isDirty = false;
        });

        // Handle Reset buttons (prevent warning on reload)
        const resetBtns = form.querySelectorAll("[type='reset']");
        resetBtns.forEach((btn) => {
            btn.addEventListener("click", () => {
                isDirty = false;
            });
        });

        // Warn on navigation
        window.addEventListener("beforeunload", (e) => {
            if (isDirty) {
                e.preventDefault();
                e.returnValue = ""; // Chrome requires returnValue to be set
            }
        });
    });
}

/* ===========================================================================
   INITIALIZATION
   =========================================================================== */

$(document).ready(function () {
    if (typeof initializeFormHelpers === "function") initializeFormHelpers();
    if (typeof initializeSidebar === "function") initializeSidebar();
    if (typeof initializeUnsavedChangesWarning === "function")
        initializeUnsavedChangesWarning();
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
            if (typeof grecaptcha !== "undefined") {
                var captchaResponse = grecaptcha.getResponse();
                isCaptchaValid = captchaResponse.length > 0;
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
                        window.showToast(
                            "Please fill in required fields and verify captcha.",
                            "error"
                        );
                    } else if (!isCaptchaValid) {
                        window.showToast(
                            "Please verify you are not a robot.",
                            "error"
                        );
                    } else {
                        window.showToast(
                            "Please fill in all required information.",
                            "error"
                        );
                    }
                }
                return false;
            }
        });
    }
});
//Photo Cropping
$(document).ready(function () {
    var cropper;
    var $modal = $('#customCropModal');
    var imageToCrop = document.getElementById('imageToCrop');
    var $photoInput = $('#photoInput');

    function closeCropModal() {
        $modal.hide();
        if (cropper) {
            cropper.destroy();
            cropper = null;
        }
        $photoInput.val('');
    }

    // --- EVENT: File Selected ---
    $photoInput.change(function (e) {
        var files = e.target.files;

        // 1. DESTROY IMMEDIATELY
        if (cropper) {
            cropper.destroy();
            cropper = null;
        }

        // Reset the image src to prevent ghosting while loading
        imageToCrop.src = "";

        if (files && files.length > 0) {
            var file = files[0];
            var reader = new FileReader();

            reader.onload = function (e) {
                // 2. Set new source
                imageToCrop.src = e.target.result;

                // 3. Show modal
                $modal.show();

                // 4. Initialize new Cropper
                cropper = new Cropper(imageToCrop, {
                    aspectRatio: 1,
                    viewMode: 1,
                    autoCropArea: 1
                });
            };
            reader.readAsDataURL(file);
        }
    });

    // --- EVENT: Click Crop & Save ---
    $('#btnCrop').click(function () {
        if (!cropper) return;
        var uploadUrl = $(this).data('url');

        var canvas = cropper.getCroppedCanvas({
            width: 300,
            height: 300
        });

        canvas.toBlob(function (blob) {
            var formData = new FormData();
            formData.append('file', blob, 'profile_cropped.jpg');

            $.ajax({
                url: uploadUrl,
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                success: function (response) {
                    if (response.success) {
                        var fullPath = "/images/profiles/" + response.newUrl;

                        $("#profileImageDisplay").attr("src", fullPath);
                        $("#headerAvatar").attr("src", fullPath);

                        if (window.showToast) window.showToast("Profile photo updated!", "success");

                        closeCropModal();
                    } else {
                        alert("Upload failed: " + response.message);
                    }
                },
                error: function () {
                    alert("Error uploading image.");
                }
            });
        });
    });

    $('#btnCloseIcon, #btnCancelCrop').click(function () {
        closeCropModal();
    });
});