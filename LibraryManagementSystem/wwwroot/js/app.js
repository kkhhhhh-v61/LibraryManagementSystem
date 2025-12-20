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
    var $modal = $("#customCropModal");
    var imageToCrop = document.getElementById("imageToCrop");
    var $photoInput = $("#photoInput");

    function closeCropModal() {
        $modal.hide();
        if (cropper) {
            cropper.destroy();
            cropper = null;
        }
        $photoInput.val("");
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
                    autoCropArea: 1,
                });
            };
            reader.readAsDataURL(file);
        }
    });

    // --- EVENT: Click Crop & Save ---
    $("#btnCrop").click(function () {
        if (!cropper) return;
        var uploadUrl = $(this).data("url");

        var canvas = cropper.getCroppedCanvas({
            width: 300,
            height: 300,
        });

        canvas.toBlob(function (blob) {
            var formData = new FormData();
            formData.append("file", blob, "profile_cropped.jpg");

            $.ajax({
                url: uploadUrl,
                type: "POST",
                data: formData,
                processData: false,
                contentType: false,
                success: function (response) {
                    if (response.success) {
                        var fullPath = "/images/profiles/" + response.newUrl;

                        $("#profileImageDisplay").attr("src", fullPath);
                        $("#headerAvatar").attr("src", fullPath);

                        if (window.showToast)
                            window.showToast(
                                "Profile photo updated!",
                                "success"
                            );

                        closeCropModal();
                    } else {
                        alert("Upload failed: " + response.message);
                    }
                },
                error: function () {
                    alert("Error uploading image.");
                },
            });
        });
    });

    $("#btnCloseIcon, #btnCancelCrop").click(function () {
        closeCropModal();
    });
});
// ==========================================
// 1. TAB SWITCHING LOGIC (Global Function)
// ==========================================
function switchTab(tabName) {
    // 1. Hide all content
    document
        .querySelectorAll(".tab-content")
        .forEach((el) => (el.style.display = "none"));

    // 2. Deactivate all buttons
    document
        .querySelectorAll(".tab-btn")
        .forEach((btn) => btn.classList.remove("active"));

    // 3. Show selected content
    document.getElementById("tab-" + tabName).style.display = "block";

    // 4. Activate clicked button
    const buttons = document.querySelectorAll(".tab-btn");
    // Assuming buttons are in order: Profile [0], Security [1]
    if (tabName === "profile" && buttons[0]) buttons[0].classList.add("active");
    else if (buttons[1]) buttons[1].classList.add("active");
}

// ==========================================
// 2. ACCOUNT MANAGEMENT (Password & Profile)
// ==========================================
$(document).ready(function () {
    $("#profileForm")
        .off("submit")
        .on("submit", function (e) {
            e.preventDefault();

            // Optional: Check validation
            if ($(this).valid && !$(this).valid()) return;

            var $form = $(this);
            var $btn = $("#saveBtn");
            var originalText = $btn.text();

            $btn.prop("disabled", true).html(
                '<i class="fas fa-spinner fa-spin"></i> Saving...'
            );

            $.ajax({
                url: "/Account/UpdateProfile",
                type: "POST",
                data: $form.serialize(),
                success: function (response) {
                    if (response.success) {
                        if (window.showToast)
                            showToast(response.message, "success");

                        // Update the name on the left card immediately
                        var first = $("#FirstName").val();
                        var last = $("#LastName").val();
                        if (first || last) {
                            $(".profile-name").text(first + " " + last);
                        }
                    } else {
                        if (window.showToast)
                            showToast(response.message, "error");
                    }
                },
                error: function (xhr, status, error) {
                    if (window.showToast)
                        showToast(
                            "Server error. Please try again later.",
                            "error"
                        );
                },
                complete: function () {
                    $btn.prop("disabled", false).text(originalText);
                },
            });
        });
});
/* ===========================================================================
   3. PASSWORD CHANGE HANDLING (Vanilla JS)
   =========================================================================== */
document.addEventListener("DOMContentLoaded", function () {
    const passwordForm = document.getElementById("passwordForm");
    const alertBox = document.getElementById("passwordAlert");

    if (passwordForm) {
        // --- SAFETY LOCK START ---
        // Check if we have already attached the listener. If so, stop.
        if (passwordForm.dataset.listenerAttached === "true") {
            return;
        }
        // Mark the form as "listening" so this code doesn't run again
        passwordForm.dataset.listenerAttached = "true";
        // --- SAFETY LOCK END ---

        passwordForm.addEventListener("submit", function (e) {
            e.preventDefault();

            // 1. Clear previous errors UI
            if (alertBox) alertBox.style.display = "none";
            document
                .querySelectorAll(".text-danger")
                .forEach((el) => (el.textContent = ""));

            // 2. Prepare Data
            const formData = new FormData(this);

            // 3. Send AJAX Request
            fetch("/Account/ChangePassword", {
                method: "POST",
                body: formData,
                headers: {
                    RequestVerificationToken:
                        passwordForm.querySelector(
                            'input[name="__RequestVerificationToken"]'
                        )?.value ||
                        document.querySelector(
                            'input[name="__RequestVerificationToken"]'
                        )?.value,
                },
            })
                .then((response) => response.json())
                .then((data) => {
                    if (data.success) {
                        // --- SUCCESS ---
                        if (window.showToast) {
                            window.showToast(data.message, "success");
                        } else {
                            alert(data.message);
                        }

                        passwordForm.reset();

                        // Reset icons and inputs
                        document
                            .querySelectorAll(".toggle-icon")
                            .forEach((icon) => {
                                icon.classList.remove("fa-eye-slash");
                                icon.classList.add("fa-eye");
                            });
                        document
                            .querySelectorAll(
                                '#passwordForm input[type="text"]'
                            )
                            .forEach((input) => {
                                input.type = "password";
                            });
                    } else {
                        // --- ERROR ---
                        if (alertBox) {
                            alertBox.style.display = "block";
                            alertBox.className = "alert alert-danger";
                            alertBox.style.backgroundColor = "#f8d7da";
                            alertBox.style.color = "#721c24";
                            alertBox.style.border = "1px solid #f5c6cb";
                            alertBox.style.padding = "10px";
                            alertBox.style.borderRadius = "4px";

                            if (data.errors && data.errors.length > 0) {
                                alertBox.innerHTML =
                                    "<strong>Please fix:</strong><br/>" +
                                    data.errors.join("<br/>");
                            } else {
                                alertBox.textContent = data.message;
                            }
                        }
                    }
                })
                .catch((error) => {
                    console.error("Error:", error);
                    if (window.showToast) {
                        window.showToast(
                            "An unexpected error occurred.",
                            "error"
                        );
                    }
                });
        });
    }
});

/* ===========================================================================
   NOTIFICATION SYSTEM
   =========================================================================== */

window.loadNotifications = function () {
    const dropdown = document.getElementById("notificationDropdown");
    const badge = document.getElementById("notificationBadge");

    if (!dropdown) return;

    // 1. Fetch List
    fetch("/Notification/GetNotifications")
        .then((res) => res.text())
        .then((html) => {
            dropdown.innerHTML = html;
        })
        .catch((err) => console.error("Failed to load notifications", err));

    // 2. Fetch Count (Separate to ensure badge is accurate even if list isn't open)
    fetch("/Notification/GetUnreadCount")
        .then((res) => res.json())
        .then((data) => {
            if (data.count > 0) {
                badge.style.display = "block";
                // Optional: badge.textContent = data.count; // If you want numbers
            } else {
                badge.style.display = "none";
            }
        })
        .catch((err) => console.error("Failed to load count", err));
};

window.handleNotificationClick = function (id, link) {
    // Mark as read
    fetch(`/Notification/MarkAsRead?id=${id}`, { method: "POST" })
        .then(() => {
            // Redirect if link exists
            if (link && link !== "null" && link !== "") {
                window.location.href = link;
            } else {
                // Just reload the list to show read state
                window.loadNotifications();
            }
        })
        .catch((err) => console.error(err));
};

window.markAllNotificationsRead = function () {
    fetch("/Notification/MarkAllRead", { method: "POST" })
        .then(() => {
            window.loadNotifications();
        })
        .catch((err) => console.error(err));
};

window.deleteNotification = function (e, id) {
    e.stopPropagation(); // Prevent triggering the item click (mark as read/redirect)
    fetch(`/Notification/Delete?id=${id}`, { method: "POST" })
        .then(() => window.loadNotifications())
        .catch((err) => console.error(err));
};

window.clearAllNotifications = function () {
    const modal = document.getElementById("modal-clear-notifications");
    if (modal) {
        modal.classList.add("active");
    }
};

// Initialize on load if user is logged in (check for dropdown existence)
document.addEventListener("DOMContentLoaded", () => {
    if (document.getElementById("notificationDropdown")) {
        window.loadNotifications();

        // Optional: Poll every 60 seconds
        setInterval(window.loadNotifications, 60000);
    }

    // Initialize Clear Notifications Modal
    const clearModal = document.getElementById("modal-clear-notifications");
    if (clearModal) {
        const confirmBtn = document.getElementById("confirmClearNotifications");
        const closeBtns = clearModal.querySelectorAll(".js-close-modal");

        if (confirmBtn) {
            confirmBtn.addEventListener("click", () => {
                fetch("/Notification/ClearAll", { method: "POST" })
                    .then(() => {
                        window.loadNotifications();
                        clearModal.classList.remove("active");
                        if (window.showToast)
                            window.showToast(
                                "All notifications cleared",
                                "success"
                            );
                    })
                    .catch((err) => console.error(err));
            });
        }

        closeBtns.forEach((btn) => {
            btn.addEventListener("click", () => {
                clearModal.classList.remove("active");
            });
        });

        clearModal.addEventListener("click", (e) => {
            if (e.target === clearModal) clearModal.classList.remove("active");
        });
    }
});

/* ===========================================================================
   WISHLIST SYSTEM
   =========================================================================== */

window.loadWishlist = function () {
    const dropdown = document.getElementById("wishlistDropdown");
    if (!dropdown) return;

    fetch("/Wishlist/GetWishlist")
        .then((res) => res.text())
        .then((html) => {
            dropdown.innerHTML = html;
        })
        .catch((err) => console.error("Failed to load wishlist", err));
};

window.toggleWishlist = function (btn, bookId) {
    fetch(`/Wishlist/Toggle?bookId=${bookId}`, { method: "POST" })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                // Update Button State
                const icon = btn.querySelector("i");
                if (data.isInWishlist) {
                    icon.classList.remove("fa-regular");
                    icon.classList.add("fa-solid");
                    if (window.showToast)
                        window.showToast("Added to wishlist", "success");
                } else {
                    icon.classList.remove("fa-solid");
                    icon.classList.add("fa-regular");
                    if (window.showToast)
                        window.showToast("Removed from wishlist", "info");
                }
                // Reload dropdown
                window.loadWishlist();
            }
        })
        .catch((err) => console.error(err));
};

window.removeFromWishlist = function (e, bookId) {
    e.stopPropagation();
    e.preventDefault();

    fetch(`/Wishlist/Toggle?bookId=${bookId}`, { method: "POST" })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                if (window.showToast)
                    window.showToast("Removed from wishlist", "info");
                window.loadWishlist();

                // Update book detail button if present
                const detailBtn = document.querySelector(
                    `.js-wishlist-trigger[onclick*="${bookId}"]`
                );
                if (detailBtn) {
                    const icon = detailBtn.querySelector("i");
                    if (icon) {
                        icon.classList.remove("fa-solid");
                        icon.classList.add("fa-regular");
                    }
                }
            }
        })
        .catch((err) => console.error(err));
};

document.addEventListener("DOMContentLoaded", () => {
    if (document.getElementById("wishlistDropdown")) {
        window.loadWishlist();
    }
});
/* ===========================================================================
   STAFF: LIVE USER SEARCH (ADDED THIS NEW SECTION)
   =========================================================================== */
$(document).ready(function () {
    let typingTimer;
    let doneTypingInterval = 300; // 300ms delay

    $("#ajaxSearch").on("input", function () {
        clearTimeout(typingTimer);
        var query = $(this).val();

        typingTimer = setTimeout(function () {
            performSearch(query);
        }, doneTypingInterval);
    });

    function performSearch(query) {
        $.ajax({
            url: "/Staff/FilterUsers", // Ensure this matches your StaffController
            type: "GET",
            data: { searchString: query },
            success: function (result) {
                $("#tableBody").html(result);
            },
            error: function (xhr, status, error) {
                console.error("Search Error:", error);
            },
        });
    }
});
