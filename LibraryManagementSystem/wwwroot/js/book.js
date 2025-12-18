/* ===========================================================================
   MULTI-SELECT DROPDOWN
   =========================================================================== */

/**
 * Initializes a custom multi-select dropdown component.
 * @param {HTMLElement} wrapper - The main container element for the multi-select component.
 */
function loadMultiSelect(wrapper) {
    const searchContainer = wrapper.querySelector(".multi-select__search");
    const searchInput = wrapper.querySelector(".search__input");
    const dropdown = wrapper.querySelector(".multi-select__dropdown");
    const select = wrapper.querySelector(".multi-select__select");

    Array.from(select.options).forEach((option) => {
        if (option.selected) {
            createChip(option.value, option.text);
            const dropdownOption = dropdown.querySelector(
                `.dropdown__option[data-value="${option.value}"]`
            );
            if (dropdownOption) dropdownOption.classList.add("selected");
        }
    });

    searchInput.addEventListener("focus", () => {
        dropdown.classList.add("active");
    });

    document.addEventListener("click", (e) => {
        if (!wrapper.contains(e.target)) {
            dropdown.classList.remove("active");
        }
    });

    searchInput.addEventListener("input", (e) => {
        const term = e.target.value.toLowerCase();
        const options = dropdown.querySelectorAll(".dropdown__option");
        let hasVisibleOptions = false;

        options.forEach((opt) => {
            const text = opt.innerText.toLowerCase();
            const isVisible = text.includes(term);
            opt.style.display = isVisible ? "block" : "none";
            if (isVisible) hasVisibleOptions = true;
        });

        dropdown.classList.toggle("active", hasVisibleOptions);
    });

    dropdown.addEventListener("click", (e) => {
        const option = e.target.closest(".dropdown__option");
        if (!option) return;

        const value = option.dataset.value;
        const text = option.innerText;

        toggleOption(value, text, option);

        searchInput.value = "";
        dropdown
            .querySelectorAll(".dropdown__option")
            .forEach((o) => (o.style.display = "block"));
        searchInput.focus();
    });

    searchContainer.addEventListener("click", (e) => {
        const removeButton = e.target.closest(".chip__remove");
        if (removeButton) {
            e.stopPropagation();
            const chip = removeButton.closest(".multi-select__chip");
            const value = chip.dataset.value;

            const dropdownOption = dropdown.querySelector(
                `.dropdown__option[data-value="${value}"]`
            );
            toggleOption(value, null, dropdownOption);
        }
    });

    function toggleOption(value, text, option) {
        const selectOption = select.querySelector(`option[value="${value}"]`);
        if (!selectOption) return;

        const isNowSelected = !selectOption.selected;
        selectOption.selected = isNowSelected;

        if (isNowSelected) {
            createChip(value, text);
            option.classList.add("selected");
        } else {
            const chip = searchContainer.querySelector(
                `.multi-select__chip[data-value="${value}"]`
            );
            if (chip) chip.remove();
            option.classList.remove("selected");
        }
    }

    function createChip(value, text) {
        const chip = document.createElement("div");
        chip.className = "multi-select__chip";
        chip.dataset.value = value;
        chip.innerHTML = `
            ${text}
            <span class="chip__remove">
                <svg class="chip__remove-icon" xmlns="http://www.w3.org/2000/svg" height="16" width="16" viewBox="0 -960 960 960" fill="currentColor">
                    <path d="m256-200-56-56 224-224-224-224 56-56 224 224 224-224 56 56-224 224 224 224-56 56-224-224-224 224Z"/>
                </svg>
            </span>
        `;
        searchContainer.insertBefore(chip, searchInput);
    }
}

/* ===========================================================================
   IMAGE UPLOAD HANDLING
   =========================================================================== */

/**
 * Sets up the image upload component with preview and session storage persistence.
 * @param {HTMLElement} wrapper - The main container element for the upload component.
 */
function setupImageUpload(wrapper) {
    const input = wrapper.querySelector(".upload__input");
    const preview = wrapper.querySelector(".upload__preview");
    const removeButton = wrapper.querySelector(".upload__remove");
    const form = wrapper.closest("form");
    const STORAGE_KEY = "temp_image_data";
    const SUBMIT_KEY = "form_submitting";

    input.addEventListener("change", (e) => {
        const file = e.target.files[0];
        handleFileUpdate(file);
    });

    removeButton.addEventListener("click", (e) => {
        e.preventDefault();
        e.stopPropagation();
        clearFile();
    });

    if (form) {
        $(form)
            .find("[type=reset]")
            .on("click", () => {
                clearFile();
            });

        form.addEventListener("submit", () => {
            sessionStorage.setItem(SUBMIT_KEY, "true");
        });
    }

    if (!sessionStorage.getItem(SUBMIT_KEY)) {
        sessionStorage.removeItem(STORAGE_KEY);
    }
    sessionStorage.removeItem(SUBMIT_KEY);

    const storedBase64 = sessionStorage.getItem(STORAGE_KEY);
    if (storedBase64) {
        restoreFileState(storedBase64);
    } else {
        const existingImage = wrapper.dataset.existingImage;
        if (existingImage) {
            preview.src = existingImage;
            wrapper.classList.add("has-image");
        }
    }

    function handleFileUpdate(file) {
        if (!file) return;

        const reader = new FileReader();
        reader.onload = (e) => {
            const base64String = e.target.result;
            preview.src = base64String;
            wrapper.classList.add("has-image");
            sessionStorage.setItem(STORAGE_KEY, base64String);
        };
        reader.readAsDataURL(file);
    }

    function clearFile() {
        input.value = "";
        preview.src = "";
        wrapper.classList.remove("has-image");
        sessionStorage.removeItem(STORAGE_KEY);
    }

    function restoreFileState(base64String) {
        preview.src = base64String;
        wrapper.classList.add("has-image");

        const arr = base64String.split(",");
        const mime = arr[0].match(/:(.*?);/)[1];
        const bstr = atob(arr[1]);
        let n = bstr.length;
        const u8arr = new Uint8Array(n);
        while (n--) {
            u8arr[n] = bstr.charCodeAt(n);
        }

        const file = new File([u8arr], "restored-image.jpg", { type: mime });

        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);
        input.files = dataTransfer.files;
    }
}

/* ===========================================================================
   TABLE MANAGEMENT (MANAGE GENRE / AUTHOR)
   =========================================================================== */

// Store selected items (ID -> {name, books}) to persist across searches
const selectionState = new Map();

function initializeTableManagement() {
    const searchInput = document.getElementById("nameSearchInput");
    const searchClear = document.getElementById("searchClearButton");
    const tableContainer = document.getElementById("tableContainer");
    const deselectButton = document.getElementById("deselectAllButton");

    // Determine if we are using AJAX search (Genre/Author) or Client-side (Book)
    const isAjax = tableContainer !== null;

    if (searchInput) {
        let debounceTimer;

        searchInput.addEventListener("input", function (e) {
            const term = e.target.value;

            // Toggle Clear Button
            if (searchClear) {
                if (term.length > 0) searchClear.classList.add("active");
                else searchClear.classList.remove("active");
            }

            if (isAjax) {
                // AJAX Search
                clearTimeout(debounceTimer);
                debounceTimer = setTimeout(() => {
                    performAjaxSearch(term);
                }, 300);
            } else {
                // Client-side Search (Legacy for ManageBook)
                performClientSideSearch(term);
            }
        });

        if (searchClear) {
            searchClear.addEventListener("click", () => {
                searchInput.value = "";
                searchClear.classList.remove("active");
                searchInput.dispatchEvent(new Event("input"));
                searchInput.focus();
            });
        }
    }

    if (deselectButton) {
        deselectButton.addEventListener("click", function () {
            const rowCheckboxes = document.querySelectorAll(".row-checkbox");
            const selectAll = document.getElementById("selectAll");
            selectionState.clear();
            rowCheckboxes.forEach((cb) => (cb.checked = false));
            if (selectAll) selectAll.checked = false;
            updateBatchButtonState();
        });
    }

    // Initial binding of table content events
    bindTableContentEvents();

    // Bind static modal events (Edit/Delete/Batch)
    bindStaticModalEvents();
}

function performAjaxSearch(term) {
    const tableContainer = document.getElementById("tableContainer");
    const url = new URL(window.location.href);
    url.searchParams.set("search", term);

    fetch(url, {
        headers: { "X-Requested-With": "XMLHttpRequest" },
    })
        .then((response) => response.text())
        .then((html) => {
            tableContainer.innerHTML = html;
            bindTableContentEvents(); // Re-bind events for new content
        })
        .catch((err) => console.error("Search failed", err));
}

function performClientSideSearch(term) {
    const tableBody = document.querySelector(".table tbody");
    if (!tableBody) return;

    const rows = tableBody.querySelectorAll(".table__row");
    let hasResults = false;
    const lowerTerm = term.toLowerCase();

    rows.forEach((row) => {
        const nameCell = row.querySelector(".table__cell--main");
        if (nameCell) {
            const text = nameCell.textContent.toLowerCase();
            if (text.includes(lowerTerm)) {
                row.style.display = "";
                hasResults = true;
            } else {
                row.style.display = "none";
            }
        }
    });

    updateNoResultsRow(tableBody, hasResults, term);
    updateSelectAllState();
}

function updateNoResultsRow(tableBody, hasResults, term) {
    let noResultRow = document.getElementById("search-no-results");
    if (!hasResults && term !== "") {
        if (!noResultRow) {
            noResultRow = document.createElement("tr");
            noResultRow.id = "search-no-results";
            noResultRow.innerHTML = `
                <td colspan="7" style="text-align:center; padding: 2rem; color: var(--color-gray-500);">
                    No results found for "${term}"
                </td>`;
            tableBody.appendChild(noResultRow);
        } else {
            noResultRow.querySelector(
                "td"
            ).textContent = `No results found for "${term}"`;
            noResultRow.style.display = "";
        }
    } else {
        if (noResultRow) noResultRow.remove();
    }
}

function updateSelectAllState() {
    const selectAll = document.getElementById("selectAll");
    const rowCheckboxes = document.querySelectorAll(".row-checkbox");

    if (selectAll) {
        const visibleCheckboxes = Array.from(rowCheckboxes).filter(
            (cb) => cb.closest(".table__row").style.display !== "none"
        );

        if (visibleCheckboxes.length > 0) {
            selectAll.checked = visibleCheckboxes.every((cb) => cb.checked);
        } else {
            selectAll.checked = false;
        }
    }
}

function updateBatchButtonState() {
    const batchButton = document.getElementById("batchDeleteButton");
    const deselectButton = document.getElementById("deselectAllButton");
    const selectionCountSpan = document.getElementById("selectionCount");

    if (!batchButton) return;

    const count = selectionState.size;

    if (count > 0) {
        if (selectionCountSpan) {
            selectionCountSpan.textContent = `${count} Selected`;
            selectionCountSpan.classList.add("active");
        }
        batchButton.classList.add("active");
        if (deselectButton) deselectButton.classList.add("active");
    } else {
        if (selectionCountSpan) selectionCountSpan.classList.remove("active");
        batchButton.classList.remove("active");
        if (deselectButton) deselectButton.classList.remove("active");
    }
}

function bindTableContentEvents() {
    const selectAll = document.getElementById("selectAll");
    const rowCheckboxes = document.querySelectorAll(".row-checkbox");

    if (selectAll) {
        // Remove old listener if possible, but simpler to just re-bind on new element
        selectAll.addEventListener("click", function () {
            const visibleRows = Array.from(rowCheckboxes).filter(
                (cb) => cb.closest(".table__row").style.display !== "none"
            );
            const isChecked = this.checked;
            visibleRows.forEach((cb) => {
                cb.checked = isChecked;
                if (isChecked) {
                    selectionState.set(cb.value, {
                        name: cb.dataset.name,
                        books: cb.dataset.books,
                    });
                } else {
                    selectionState.delete(cb.value);
                }
            });
            updateBatchButtonState();
        });
    }

    rowCheckboxes.forEach((cb) => {
        // Restore state
        if (selectionState.has(cb.value)) {
            cb.checked = true;
        }

        cb.addEventListener("change", function () {
            if (this.checked) {
                selectionState.set(this.value, {
                    name: this.dataset.name,
                    books: this.dataset.books,
                });
            } else {
                selectionState.delete(this.value);
                if (selectAll) selectAll.checked = false;
            }

            // Check if all visible are checked
            const visibleCheckboxes = Array.from(rowCheckboxes).filter(
                (cb) => cb.closest(".table__row").style.display !== "none"
            );

            if (visibleCheckboxes.length > 0) {
                const allChecked = visibleCheckboxes.every((cb) => cb.checked);
                if (allChecked && selectAll) selectAll.checked = true;
            }

            updateBatchButtonState();
        });
    });

    // Bind Edit/Delete buttons inside the table
    bindDynamicModalTriggers();

    // Bind Accordion Toggles
    const toggleButtons = document.querySelectorAll(".js-toggle-detail");
    toggleButtons.forEach((btn) => {
        btn.addEventListener("click", function () {
            const masterRow = this.closest(".table__row--master");
            const detailRow = masterRow.nextElementSibling;
            const wrapper = detailRow.querySelector(".detail-wrapper");

            if (
                detailRow &&
                detailRow.classList.contains("table__row--detail")
            ) {
                this.classList.toggle("active");

                if (this.classList.contains("active")) {
                    // Open
                    detailRow.style.display = "table-row";
                    // Force reflow
                    void detailRow.offsetWidth;
                    wrapper.classList.add("open");
                } else {
                    // Close
                    wrapper.classList.remove("open");
                    wrapper.addEventListener(
                        "transitionend",
                        function () {
                            if (!wrapper.classList.contains("open")) {
                                detailRow.style.display = "none";
                            }
                        },
                        { once: true }
                    );
                }
            }
        });
    });

    // Bind Status Toggles (Featured/Archived)
    bindStatusToggles();
}

function bindStaticModalEvents() {
    const batchButton = document.getElementById("batchDeleteButton");
    const batchForm = document.getElementById("batchForm");

    if (batchButton && batchForm) {
        batchButton.addEventListener("click", function () {
            const count = selectionState.size;
            if (count == 0) return;

            const batchModal = document.getElementById("modal-batch-delete");
            const listContainer = document.getElementById("batchDeleteList");
            const confirmBtn = document.getElementById("confirmBatchDelete");
            const cancelBtn = batchModal.querySelector(".js-cancel-batch");
            const closeBtn = batchModal.querySelector(".js-close-modal");

            if (batchModal && listContainer) {
                listContainer.innerHTML = "";

                selectionState.forEach((data, id) => {
                    const name = data.name;
                    const books = parseInt(data.books || "0");
                    const isUnsafe = books > 0;

                    const item = document.createElement("div");
                    item.className = `delete-item ${
                        isUnsafe ? "delete-item--unsafe" : ""
                    }`;

                    const icon = isUnsafe
                        ? `<svg class="delete-item__icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" fill="#ef4444"><path d="M480-280q17 0 28.5-11.5T520-320q0-17-11.5-28.5T480-360q-17 0-28.5 11.5T440-320q0 17 11.5 28.5T480-280Zm-40-160h80v-240h-80v240Zm40 360q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Zm0-80q134 0 227-93t93-227q0-134-93-227t-227-93q-134 0-227 93t-93 227q0 134 93 227t227 93Zm0-320Z"/></svg>`
                        : `<svg class="delete-item__icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" fill="#22c55e"><path d="M382-240 154-468l57-57 171 171 367-367 57 57-424 424Z"/></svg>`;

                    const statusText = isUnsafe
                        ? `Cannot delete (${books} books linked)`
                        : "Ready to delete";

                    item.innerHTML = `
                        <div class="delete-item__info">
                            <span class="delete-item__name">${name}</span>
                            <span class="delete-item__status">${statusText}</span>
                        </div>
                        ${icon}
                    `;
                    listContainer.appendChild(item);
                });

                batchModal.classList.add("active");

                const closeBatchModal = () =>
                    batchModal.classList.remove("active");

                confirmBtn.onclick = () => {
                    // Ensure all selected IDs are submitted, even if not in DOM
                    // Clear existing inputs to avoid duplicates if any
                    const existingHidden =
                        batchForm.querySelectorAll('input[name="ids"]');
                    existingHidden.forEach((el) => el.remove());

                    selectionState.forEach((_, id) => {
                        const input = document.createElement("input");
                        input.type = "hidden";
                        input.name = "ids";
                        input.value = id;
                        batchForm.appendChild(input);
                    });
                    batchForm.submit();
                };

                if (cancelBtn) cancelBtn.onclick = closeBatchModal;
                if (closeBtn) closeBtn.onclick = closeBatchModal;
                batchModal.onclick = (e) => {
                    if (e.target === batchModal) closeBatchModal();
                };
            }
        });
    }

    // Toggle Confirmation Modal Events
    const toggleModal = document.getElementById("modal-toggle-confirm");
    if (toggleModal) {
        const closeBtn = toggleModal.querySelector(".js-close-modal");
        const cancelBtn = toggleModal.querySelector(".js-cancel-toggle");
        const confirmBtn = document.getElementById("confirmToggleBtn");

        const closeToggleModal = () => {
            toggleModal.classList.remove("active");
            // If we have a pending toggle that was cancelled, revert it
            if (window.pendingToggle) {
                window.pendingToggle.element.checked =
                    !window.pendingToggle.targetValue;
                window.pendingToggle = null;
            }
        };

        if (closeBtn) closeBtn.addEventListener("click", closeToggleModal);
        if (cancelBtn) cancelBtn.addEventListener("click", closeToggleModal);

        // Confirm button logic is handled dynamically, but we can bind the click here
        if (confirmBtn) {
            confirmBtn.addEventListener("click", executePendingToggle);
        }
    }

    // Edit Modal Static Events
    const editModal = document.getElementById("modal-edit");
    if (editModal) {
        const closeButton = editModal.querySelector(".js-close-modal");
        const cancelButton = editModal.querySelector(".js-cancel-modal");
        const closeEditModal = () => editModal.classList.remove("active");

        if (closeButton) closeButton.addEventListener("click", closeEditModal);
        if (cancelButton)
            cancelButton.addEventListener("click", closeEditModal);
        editModal.addEventListener("click", (e) => {
            if (e.target === editModal) closeEditModal();
        });
    }

    // Delete Modal Static Events
    const deleteModal = document.getElementById("modal-delete");
    if (deleteModal) {
        const closeDeleteButton = deleteModal.querySelector(".js-close-modal");
        const cancelDeleteButton =
            deleteModal.querySelector(".js-cancel-delete");
        const closeDeleteModal = () => deleteModal.classList.remove("active");

        if (closeDeleteButton)
            closeDeleteButton.addEventListener("click", closeDeleteModal);
        if (cancelDeleteButton)
            cancelDeleteButton.addEventListener("click", closeDeleteModal);
        deleteModal.addEventListener("click", (e) => {
            if (e.target === deleteModal) closeDeleteModal();
        });
    }
}

function bindDynamicModalTriggers() {
    const editModal = document.getElementById("modal-edit");
    if (editModal) {
        const modalTitle = editModal.querySelector(".modal__title");
        const formIdInput = editModal.querySelector("input[name='Form.Id']");
        const formNameInput = editModal.querySelector(
            "input[name='Form.Name']"
        );
        const openButtons = document.querySelectorAll(".js-open-modal");

        openButtons.forEach((button) => {
            button.addEventListener("click", () => {
                const id = button.dataset.id;
                const name = button.dataset.name;
                const type = button.dataset.type;

                if (id) {
                    modalTitle.textContent = `Edit ${type}`;
                    formIdInput.value = id;
                    formNameInput.value = name;
                } else {
                    modalTitle.textContent = `Add ${type}`;
                    formIdInput.value = "";
                    formNameInput.value = "";

                    editModal
                        .querySelectorAll(".field-validation-error")
                        .forEach((el) => (el.innerHTML = ""));
                    editModal
                        .querySelectorAll(".input-validation-error")
                        .forEach((el) =>
                            el.classList.remove("input-validation-error")
                        );
                }
                editModal.classList.add("active");
                setTimeout(() => formNameInput.focus(), 50);
            });
        });

        const shouldOpen = document.getElementById("server-modal-trigger");
        if (shouldOpen) {
            editModal.classList.add("active");
            setTimeout(() => formNameInput.focus(), 50);
        }
    }

    // Delete Triggers
    const deleteModal = document.getElementById("modal-delete");
    if (deleteModal) {
        const deleteIdInput = deleteModal.querySelector("input[name='id']");
        const deleteNameDisplay = deleteModal.querySelector(".js-delete-name");
        const deleteButtons = document.querySelectorAll(".js-delete-trigger");

        deleteButtons.forEach((button) => {
            button.addEventListener("click", () => {
                const id = button.dataset.id;
                const name = button.dataset.name;

                deleteIdInput.value = id;
                if (deleteNameDisplay) deleteNameDisplay.textContent = name;

                deleteModal.classList.add("active");
            });
        });
    }
}

function bindStatusToggles() {
    const toggles = document.querySelectorAll(".js-toggle-status");
    toggles.forEach((toggle) => {
        toggle.addEventListener("change", function (e) {
            const id = this.dataset.id;
            const field = this.dataset.field;
            const isChecked = this.checked;

            // Store pending action
            window.pendingToggle = {
                element: this,
                id: id,
                field: field,
                targetValue: isChecked,
            };

            // Check session preference
            const suppressModal =
                sessionStorage.getItem("suppressToggleConfirm") === "true";

            if (suppressModal) {
                executePendingToggle();
            } else {
                showToggleModal(field, isChecked);
            }
        });
    });
}

function showToggleModal(field, isChecked) {
    const modal = document.getElementById("modal-toggle-confirm");
    const textEl = document.getElementById("toggleConfirmText");
    const fieldName = field === "IsFeatured" ? "Featured" : "Archived";
    const action = isChecked ? "enable" : "disable";

    if (textEl) {
        textEl.innerHTML = `Are you sure you want to <b>${action}</b> the <b>${fieldName}</b> status for this book?`;
    }

    if (modal) modal.classList.add("active");
}

function executePendingToggle() {
    if (!window.pendingToggle) return;

    const { element, id, field, targetValue } = window.pendingToggle;
    const modal = document.getElementById("modal-toggle-confirm");
    const dontAskCheckbox = document.getElementById("dontAskAgain");

    // Save session preference if checked
    if (
        modal &&
        modal.classList.contains("active") &&
        dontAskCheckbox &&
        dontAskCheckbox.checked
    ) {
        sessionStorage.setItem("suppressToggleConfirm", "true");
    }

    if (modal) modal.classList.remove("active");

    // Perform AJAX
    const formData = new FormData();
    formData.append("id", id);
    formData.append("field", field);
    formData.append("value", targetValue);

    fetch("/Book/ToggleBookStatus", {
        method: "POST",
        body: formData,
    })
        .then((res) => {
            if (!res.ok) {
                // Revert on failure
                element.checked = !targetValue;
                if (window.showToast)
                    window.showToast("Failed to update status.", "error");
            } else {
                if (window.showToast)
                    window.showToast("Status updated successfully.", "success");
            }
            window.pendingToggle = null;
        })
        .catch((err) => {
            console.error(err);
            element.checked = !targetValue;
            if (window.showToast)
                window.showToast("An error occurred.", "error");
            window.pendingToggle = null;
        });
}

/* ===========================================================================
   INITIALIZATION
   =========================================================================== */

document.documentElement.classList.add("js-enabled");

document.addEventListener("DOMContentLoaded", () => {
    // Clear stored image on successful form submission
    if (sessionStorage.getItem("submissionSuccess") === "true") {
        sessionStorage.removeItem("temp_image_data");
        sessionStorage.removeItem("submissionSuccess");
    }

    // Initialize all multi-select components on the page
    const multiSelects = document.querySelectorAll(".field__multi-select");
    multiSelects.forEach(loadMultiSelect);

    // Initialize all image upload components on the page
    const uploadWrappers = document.querySelectorAll(".upload__wrapper");
    uploadWrappers.forEach(setupImageUpload);

    // Initialize features for management tables (Genre/Author)
    initializeTableManagement();
});
