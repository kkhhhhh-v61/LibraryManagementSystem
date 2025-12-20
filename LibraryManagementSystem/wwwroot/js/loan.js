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

document.addEventListener("DOMContentLoaded", () => {
    // This will only run if we are on a page that has the loan management components
    if (document.getElementById("modal-edit")) {
        initializeLoanPage();
    }
});
