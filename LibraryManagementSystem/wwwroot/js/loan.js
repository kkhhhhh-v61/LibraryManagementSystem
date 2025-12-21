function initializeLoanPage() {
    const editModal = document.getElementById("modal-edit");
    if (!editModal) return;

    const pickupDateInput = document.getElementById("editPickupDate");
    const returnDateInput = document.getElementById("editReturnDate");
    const loanDateInput = document.getElementById("editLoanDate");
    const loanDueInput = document.getElementById("editLoanDue");

    function toggleReadOnlyFields() {
        const isReturned = returnDateInput.value !== "";

        // Toggle ReadOnly
        loanDueInput.readOnly = isReturned;
        pickupDateInput.readOnly = isReturned;

        // Toggle Styling
        const bg = isReturned ? "var(--color-gray-100)" : "";
        const cursor = isReturned ? "not-allowed" : "";

        loanDueInput.style.backgroundColor = bg;
        loanDueInput.style.cursor = cursor;
        pickupDateInput.style.backgroundColor = bg;
        pickupDateInput.style.cursor = cursor;
    }

    // --- Edit Modal Logic ---
    // Use event delegation for edit buttons that might be loaded via AJAX
    document.body.addEventListener("click", function (e) {
        const editButton = e.target.closest(".js-edit-loan");
        if (editButton) {
            document.getElementById("editLoanId").value = editButton.dataset.id;
            loanDateInput.value = editButton.dataset.loandate;
            loanDueInput.value = editButton.dataset.loandue;
            const pickupDueDate = editButton.dataset.pickupdue;
            document.getElementById("editPickupDue").value = pickupDueDate;
            pickupDateInput.value = editButton.dataset.pickupdate;
            returnDateInput.value = editButton.dataset.returndate;

            // Set Min Dates based on Start Date
            const loanDate = editButton.dataset.loandate;
            pickupDateInput.min = loanDate;
            if (pickupDueDate) {
                pickupDateInput.max = pickupDueDate;
                loanDueInput.min = pickupDueDate;
            } else {
                loanDueInput.min = loanDate;
            }

            // Return date logic: cannot be before pickup (if picked up) or start date
            if (pickupDateInput.value) {
                returnDateInput.min = pickupDateInput.value;
            } else {
                returnDateInput.min = loanDate;
            }

            // Initialize ReadOnly State
            toggleReadOnlyFields();

            editModal.classList.add("active");
        }
    });

    // Dynamic Date Logic: Update Return Date min when Pickup Date changes
    if (pickupDateInput) {
        pickupDateInput.addEventListener("change", function () {
            if (this.value) {
                returnDateInput.min = this.value;
            } else {
                returnDateInput.min = loanDateInput.value;
            }
        });
    }

    if (returnDateInput) {
        returnDateInput.addEventListener("change", toggleReadOnlyFields);
    }

    // Static close buttons for the edit modal
    const closeBtns = editModal.querySelectorAll(".js-close-modal");
    closeBtns.forEach((btn) => {
        btn.addEventListener("click", () =>
            editModal.classList.remove("active")
        );
    });

    editModal.addEventListener("click", (e) => {
        if (e.target === editModal) {
            editModal.classList.remove("active");
        }
    });
}

function initializeMergedConfirmationPage() {
    const userBarcode = document.getElementById("barcode-user");
    if (userBarcode) {
        try {
            JsBarcode(userBarcode, userBarcode.dataset.value, {
                format: "CODE128",
                width: 2,
                height: 50,
                displayValue: true,
            });
        } catch (e) {
            console.error("User JsBarcode rendering failed:", e);
        }
    }

    document.querySelectorAll(".barcode-book").forEach((el) => {
        try {
            JsBarcode(el, el.dataset.value, {
                format: "CODE128",
                width: 1.5,
                height: 40,
                displayValue: true,
                fontSize: 14,
            });
        } catch (e) {
            console.error(
                "Book JsBarcode rendering failed for:",
                el.dataset.value,
                e
            );
        }
    });
}

function initializeConfirmationPage() {
    const userBarcode = document.getElementById("barcode-user");
    const bookBarcode = document.getElementById("barcode-book");

    if (userBarcode && bookBarcode && typeof JsBarcode === "function") {
        try {
            JsBarcode(userBarcode, userBarcode.dataset.value, {
                format: "CODE128",
                width: 2,
                height: 50,
                displayValue: true,
            });
            JsBarcode(bookBarcode, bookBarcode.dataset.value, {
                format: "CODE128",
                width: 2,
                height: 50,
                displayValue: true,
            });
        } catch (e) {
            console.error("JsBarcode rendering failed:", e);
        }
    }
}

/* ===========================================================================
   PROCESS LOAN PAGE (Pickup/Return)
   =========================================================================== */

function initializeProcessPage() {
    let currentMode = null;
    let html5QrcodeScanner;
    let isProcessing = false;
    let isScannerActive = false;

    function setMode(mode) {
        currentMode = mode;
        document
            .querySelectorAll(".mode-tab")
            .forEach((t) => t.classList.remove("active"));
        document
            .querySelector(`.mode-tab[data-mode="${mode}"]`)
            .classList.add("active");

        const input = document.getElementById("manualInput");
        const title = document.getElementById("resultsTitle");
        const content = document.getElementById("resultsContent");

        if (mode === "pickup") {
            input.placeholder = "Scan User ID...";
            title.textContent = "Pickup: Waiting for User Scan...";
            content.innerHTML =
                '<div class="empty-state"><p class="empty-state__text">Scan a User ID to view reservations.</p></div>';
        } else {
            input.placeholder = "Scan Book Copy Barcode...";
            title.textContent = "Return: Ready to Scan";
            content.innerHTML =
                '<div class="empty-state"><p class="empty-state__text">Scan books to process returns instantly.</p></div>';
        }
        input.value = "";
        input.focus();

        if (!isScannerActive && html5QrcodeScanner) {
            html5QrcodeScanner.render(onScanSuccess, (error) => {
                /* ignore errors */
            });
            isScannerActive = true;
        }
    }

    function onScanSuccess(decodedText, decodedResult) {
        if (isProcessing) return;
        handleInput(decodedText);
    }

    function handleInput(text) {
        if (!text) return;
        isProcessing = true;
        document.getElementById(
            "scanFeedback"
        ).textContent = `Processing: ${text}`;

        if (currentMode === "pickup") {
            fetchUserReservations(text);
        } else {
            processReturn(text);
        }

        // Debounce slightly to prevent double scans
        setTimeout(() => {
            isProcessing = false;
        }, 1000);
    }

    function fetchUserReservations(userId) {
        fetch(`/Loan/GetUserReservations?userId=${encodeURIComponent(userId)}`)
            .then((response) => {
                if (!response.ok)
                    throw new Error("User not found or error fetching.");
                return response.text();
            })
            .then((html) => {
                document.getElementById(
                    "resultsTitle"
                ).textContent = `Reservations for User: ${userId}`;
                document.getElementById("resultsContent").innerHTML = html;
                document.getElementById("scanFeedback").textContent =
                    "User scanned successfully.";
            })
            .catch((err) => {
                window.showToast(
                    "User not found or has no reservations.",
                    "error"
                );
                document.getElementById("scanFeedback").textContent =
                    "Scan failed.";
            });
    }

    function processReturn(barcode) {
        const formData = new FormData();
        formData.append("barcode", barcode);

        fetch("/Loan/ProcessReturn", {
            method: "POST",
            body: formData,
        })
            .then((res) => res.json())
            .then((data) => {
                if (data.success) {
                    window.showToast(`Returned: ${data.bookTitle}`, "success");
                    const logItem = document.createElement("div");
                    logItem.className = "delete-item";
                    logItem.style.marginBottom = "8px";
                    logItem.innerHTML = `
                    <div class="delete-item__info">
                        <span class="delete-item__name">${data.bookTitle}</span>
                        <span class="delete-item__status" style="color: var(--color-gray-600);">User: ${data.username}</span>
                    </div>
                    <svg class="delete-item__icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" fill="#22c55e"><path d="M382-240 154-468l57-57 171 171 367-367 57 57-424 424Z"/></svg>
                `;
                    // Prepend to results
                    const container = document.getElementById("resultsContent");
                    if (container.querySelector(".empty-state"))
                        container.innerHTML = "";
                    container.insertBefore(logItem, container.firstChild);
                    document.getElementById("scanFeedback").textContent =
                        "Return processed.";
                } else {
                    window.showToast(data.message, "error");
                    document.getElementById("scanFeedback").textContent =
                        data.message;
                }
            })
            .catch((err) => console.error(err));
    }

    // Initialize Scanner
    html5QrcodeScanner = new Html5QrcodeScanner(
        "reader",
        { fps: 10, qrbox: { width: 250, height: 250 } },
        /* verbose= */ false
    );

    // Bind Events
    document.getElementById("manualSubmit").addEventListener("click", () => {
        handleInput(document.getElementById("manualInput").value);
    });

    document.getElementById("manualInput").addEventListener("keypress", (e) => {
        if (e.key === "Enter") handleInput(e.target.value);
    });

    document.querySelectorAll(".mode-tab").forEach((tab) => {
        tab.addEventListener("click", () => setMode(tab.dataset.mode));
    });

    // Initialize Mode Selection Modal
    const modeModal = document.getElementById("modal-mode-selection");
    if (modeModal) {
        modeModal.classList.add("active");

        modeModal.querySelectorAll(".js-select-mode").forEach((btn) => {
            btn.addEventListener("click", () => {
                setMode(btn.dataset.mode);
                modeModal.classList.remove("active");
            });
        });
    }
}

// Global functions for dynamic content in _ListPickup.cshtml
window.toggleAllPickup = function (source) {
    document
        .querySelectorAll(".pickup-check")
        .forEach((cb) => (cb.checked = source.checked));
};

window.submitPickup = function (e) {
    e.preventDefault();
    const form = e.target;
    const formData = new FormData(form);

    fetch("/Loan/ConfirmPickup", {
        method: "POST",
        body: formData,
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                window.showToast(data.message, "success");
                // Clear the view
                document.getElementById("resultsContent").innerHTML =
                    '<div class="empty-state"><p class="empty-state__text">Pickup confirmed successfully.</p></div>';
                document.getElementById("resultsTitle").textContent =
                    "Process Complete";
            } else {
                window.showToast(data.message, "error");
            }
        });
};

document.addEventListener("DOMContentLoaded", () => {
    // This will only run if we are on a page that has the loan management components
    if (document.getElementById("modal-edit")) {
        initializeLoanPage();
    }
    // Initialize Process Page if reader element exists
    if (document.getElementById("reader")) {
        initializeProcessPage();
    }
    // Initialize Confirmation Page (Single)
    if (document.getElementById("barcode-book")) {
        initializeConfirmationPage();
    }
    // Initialize Merged Confirmation Page
    else if (document.querySelectorAll(".barcode-book").length > 0) {
        initializeMergedConfirmationPage();
    }
});
