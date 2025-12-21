// Store selected items (ID -> {name, books}) to persist across searches
const selectionState = new Map();

function initializeTableManagement() {
    const searchInput = document.getElementById("nameSearchInput");
    const searchClear = document.getElementById("searchClearButton");
    const tableContainer = document.getElementById("tableContainer");
    const deselectButton = document.getElementById("deselectAllButton");

    // Filter Menu Elements
    const filterMenu = document.getElementById("filterMenu");
    const filterTrigger = document.getElementById("filterMenuTrigger");
    const clearFiltersBtn = document.getElementById("clearFilters");
    const filterFeatured = document.getElementById("filterFeatured");
    const filterArchived = document.getElementById("filterArchived");
    const filterDateFrom = document.getElementById("filterDateFrom");
    const filterDateTo = document.getElementById("filterDateTo");

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
                    // Get current sort
                    const activeHeader = tableContainer.querySelector(
                        ".sortable-header.active"
                    );
                    const sort = activeHeader
                        ? activeHeader.dataset.sort
                        : null;
                    const order = activeHeader
                        ? activeHeader.dataset.order
                        : null;

                    performAjaxSearch(term, 1, sort, order);
                }, 300);
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

    // Filter Menu Logic
    if (filterMenu && filterTrigger) {
        filterTrigger.addEventListener("click", (e) => {
            e.stopPropagation();
            filterMenu.classList.toggle("active");
        });

        document.addEventListener("click", (e) => {
            if (!filterMenu.contains(e.target)) {
                filterMenu.classList.remove("active");
            }
        });

        if (filterFeatured) {
            filterFeatured.addEventListener("change", () =>
                triggerSearch(searchInput)
            );
        }

        if (filterArchived) {
            filterArchived.addEventListener("change", () =>
                triggerSearch(searchInput)
            );
        }

        if (filterDateFrom) {
            filterDateFrom.addEventListener("change", () =>
                triggerSearch(searchInput)
            );
        }

        if (filterDateTo) {
            filterDateTo.addEventListener("change", () =>
                triggerSearch(searchInput)
            );
        }

        if (clearFiltersBtn) {
            clearFiltersBtn.addEventListener("click", () => {
                const filterAuthor = document.getElementById("filterAuthor");
                if (filterAuthor) filterAuthor.value = "";
                const filterGenre = document.getElementById("filterGenre");
                if (filterGenre) filterGenre.value = "";
                const filterStatus = document.getElementById("filterStatus");
                if (filterStatus) filterStatus.value = "";
                const filterFeatured =
                    document.getElementById("filterFeatured");
                if (filterFeatured) filterFeatured.checked = false;
                const filterArchived =
                    document.getElementById("filterArchived");
                if (filterArchived) filterArchived.checked = false;
                if (filterDateFrom) filterDateFrom.value = "";
                if (filterDateTo) filterDateTo.value = "";
                filterMenu.classList.remove("active");
                triggerSearch(searchInput);
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

    // Initialize Custom Filters (Dropdowns)
    initializeCustomFilters();

    // Bind static modal events (Edit/Delete/Batch)
    bindStaticModalEvents();

    // Bind static Add Copy modal events
    bindAddCopyStaticEvents();

    // Bind Pagination Events (Delegation)
    if (tableContainer) {
        tableContainer.addEventListener("click", function (e) {
            if (e.target.classList.contains("pagination__link")) {
                e.preventDefault();
                const page = e.target.dataset.page;
                const term = searchInput ? searchInput.value : "";

                // Get current sort
                const activeHeader = tableContainer.querySelector(
                    ".sortable-header.active"
                );
                const sort = activeHeader ? activeHeader.dataset.sort : null;
                const order = activeHeader ? activeHeader.dataset.order : null;

                performAjaxSearch(term, page, sort, order);
            }

            // Sorting
            const header = e.target.closest(".sortable-header");
            if (header) {
                e.preventDefault();
                const sort = header.dataset.sort;
                let order = "asc";

                if (header.classList.contains("active")) {
                    order = header.dataset.order === "asc" ? "desc" : "asc";
                }

                const term = searchInput ? searchInput.value : "";
                performAjaxSearch(term, 1, sort, order);
            }
        });
    }
}

function initializeCustomFilters() {
    const dropdowns = document.querySelectorAll(".filter-dropdown");
    const searchInput = document.getElementById("nameSearchInput");

    dropdowns.forEach((dropdown) => {
        const trigger = dropdown.querySelector(".filter-dropdown__trigger");
        const input = dropdown.querySelector('input[type="hidden"]');
        const triggerText = dropdown.querySelector(".trigger__text");
        const items = dropdown.querySelectorAll(".filter-dropdown__item");
        const clearBtn = dropdown.querySelector(".filter-clear");

        // Initial State
        if (input.value) {
            dropdown.classList.add("has-value");
        }

        // Toggle
        trigger.addEventListener("click", (e) => {
            e.stopPropagation();
            // Close others
            dropdowns.forEach((d) => {
                if (d !== dropdown) d.classList.remove("active");
            });
            dropdown.classList.toggle("active");
        });

        // Selection
        items.forEach((item) => {
            item.addEventListener("click", (e) => {
                e.stopPropagation();
                const value = item.dataset.value;
                const text = item.textContent;

                // Update UI
                triggerText.textContent = text;
                items.forEach((i) => i.classList.remove("selected"));
                item.classList.add("selected");
                dropdown.classList.remove("active");

                if (value) {
                    dropdown.classList.add("has-value");
                } else {
                    dropdown.classList.remove("has-value");
                }

                // Update Value and Trigger Search
                if (input.value !== value) {
                    input.value = value;
                    input.dispatchEvent(new Event("change"));
                    if (searchInput) triggerSearch(searchInput);
                }
            });
        });

        // Clear Button
        if (clearBtn) {
            clearBtn.addEventListener("click", (e) => {
                e.stopPropagation();

                // Reset to default (empty)
                input.value = "";
                const defaultItem = dropdown.querySelector(
                    '.filter-dropdown__item[data-value=""]'
                );
                if (defaultItem) {
                    triggerText.textContent = defaultItem.textContent;
                    items.forEach((i) => i.classList.remove("selected"));
                    defaultItem.classList.add("selected");
                }

                dropdown.classList.remove("has-value");
                dropdown.classList.remove("active");
                input.dispatchEvent(new Event("change"));
                if (searchInput) triggerSearch(searchInput);
            });
        }
    });

    // Close on outside click
    document.addEventListener("click", (e) => {
        dropdowns.forEach((d) => {
            if (!d.contains(e.target)) {
                d.classList.remove("active");
            }
        });
    });
}

function triggerSearch(searchInput) {
    const tableContainer = document.getElementById("tableContainer");
    const term = searchInput ? searchInput.value : "";

    // Get current sort
    const activeHeader = tableContainer.querySelector(
        ".sortable-header.active"
    );
    const sort = activeHeader ? activeHeader.dataset.sort : null;
    const order = activeHeader ? activeHeader.dataset.order : null;

    performAjaxSearch(term, 1, sort, order);
}

function performAjaxSearch(term, page, sort, order) {
    const tableContainer = document.getElementById("tableContainer");
    const filterAuthor = document.getElementById("filterAuthor");
    const filterGenre = document.getElementById("filterGenre");
    const filterFeatured = document.getElementById("filterFeatured");
    const filterArchived = document.getElementById("filterArchived");
    const filterStatus = document.getElementById("filterStatus");
    const filterDateFrom = document.getElementById("filterDateFrom");
    const filterDateTo = document.getElementById("filterDateTo");

    const url = new URL(window.location.href);

    if (term) url.searchParams.set("search", term);
    else url.searchParams.delete("search");

    url.searchParams.set("page", page);

    if (sort) url.searchParams.set("sort", sort);
    if (order) url.searchParams.set("order", order);

    if (filterAuthor && filterAuthor.value)
        url.searchParams.set("authorId", filterAuthor.value);
    else url.searchParams.delete("authorId");

    if (filterGenre && filterGenre.value)
        url.searchParams.set("genreId", filterGenre.value);
    else url.searchParams.delete("genreId");

    if (filterStatus && filterStatus.value)
        url.searchParams.set("status", filterStatus.value);
    else url.searchParams.delete("status");

    if (filterFeatured && filterFeatured.checked)
        url.searchParams.set("featured", "true");
    else url.searchParams.delete("featured");

    if (filterArchived && filterArchived.checked)
        url.searchParams.set("archived", "true");
    else url.searchParams.delete("archived");

    if (filterDateFrom && filterDateFrom.value)
        url.searchParams.set("dateFrom", filterDateFrom.value);
    else url.searchParams.delete("dateFrom");

    if (filterDateTo && filterDateTo.value)
        url.searchParams.set("dateTo", filterDateTo.value);
    else url.searchParams.delete("dateTo");

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

function updateBatchButtonState() {
    const batchButton = document.getElementById("batchDeleteButton");
    const deselectButton = document.getElementById("deselectAllButton");
    const selectionCountSpan = document.getElementById("selectionCount");
    const mergeButton = document.getElementById("mergeSlipsButton");

    if (!batchButton) return;

    const count = selectionState.size;

    if (count > 0) {
        if (selectionCountSpan) {
            selectionCountSpan.textContent = `${count} Selected`;
            selectionCountSpan.classList.add("active");
        }
        batchButton.classList.add("active");
        if (deselectButton) deselectButton.classList.add("active");

        if (mergeButton) {
            if (count > 1) {
                mergeButton.style.display = "inline-flex";
            } else {
                mergeButton.style.display = "none";
            }
        }
    } else {
        if (selectionCountSpan) selectionCountSpan.classList.remove("active");
        batchButton.classList.remove("active");
        if (deselectButton) deselectButton.classList.remove("active");
        if (mergeButton) {
            mergeButton.style.display = "none";
        }
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
                        status: cb.dataset.status,
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
                    status: this.dataset.status,
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

    // Bind Status Toggles (Featured/Archived)
    bindStatusToggles();

    // Bind Add Copy Triggers
    bindAddCopyTriggers();
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
                    const status = data.status;
                    let isUnsafe = books > 0;
                    let statusText = isUnsafe
                        ? `Cannot delete (${books} books linked)`
                        : "Ready to process";

                    const isLoanCancel =
                        batchForm.action.includes("BatchCancelLoan");

                    if (status) {
                        if (isLoanCancel) {
                            if (status === "RESERVED") {
                                isUnsafe = false;
                                statusText = "Ready to cancel";
                            } else {
                                isUnsafe = true;
                                const formattedStatus =
                                    status.charAt(0).toUpperCase() +
                                    status.slice(1).toLowerCase();
                                statusText = `Cannot cancel (${formattedStatus})`;
                            }
                        } else if (status !== "AVAILABLE") {
                            isUnsafe = true;
                            const formattedStatus =
                                status.charAt(0).toUpperCase() +
                                status.slice(1).toLowerCase();
                            statusText = `Cannot delete (${formattedStatus})`;
                        }
                    }

                    const item = document.createElement("div");
                    item.className = `delete-item ${
                        isUnsafe ? "delete-item--unsafe" : ""
                    }`;

                    const icon = isUnsafe
                        ? `<svg class="delete-item__icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" fill="#ef4444"><path d="M480-280q17 0 28.5-11.5T520-320q0-17-11.5-28.5T480-360q-17 0-28.5 11.5T440-320q0 17 11.5 28.5T480-280Zm-40-160h80v-240h-80v240Zm40 360q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Zm0-80q134 0 227-93t93-227q0-134-93-227t-227-93q-134 0-227 93t-93 227q0 134 93 227t227 93Zm0-320Z"/></svg>`
                        : `<svg class="delete-item__icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 -960 960 960" fill="#22c55e"><path d="M382-240 154-468l57-57 171 171 367-367 57 57-424 424Z"/></svg>`;

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

    const mergeButton = document.getElementById("mergeSlipsButton");
    if (mergeButton && batchForm) {
        mergeButton.addEventListener("click", function () {
            if (selectionState.size > 1) {
                const originalAction = batchForm.action;
                batchForm.action = "/Loan/MergedConfirmation";
                batchForm.method = "post";

                batchForm
                    .querySelectorAll('input[name="ids"]')
                    .forEach((el) => el.remove());
                selectionState.forEach((_, id) => {
                    const input = document.createElement("input");
                    input.type = "hidden";
                    input.name = "ids";
                    input.value = id;
                    batchForm.appendChild(input);
                });

                batchForm.submit();
                batchForm.action = originalAction;
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

function bindAddCopyStaticEvents() {
    const modal = document.getElementById("modal-add-copy");
    const summaryModal = document.getElementById("modal-copy-summary");

    if (modal) {
        const closeButtons = modal.querySelectorAll(
            ".js-close-modal, .js-cancel-modal"
        );
        closeButtons.forEach((btn) =>
            btn.addEventListener("click", () =>
                modal.classList.remove("active")
            )
        );
        modal.addEventListener("click", (e) => {
            if (e.target === modal) modal.classList.remove("active");
        });
    }

    if (summaryModal) {
        const closeButtons = summaryModal.querySelectorAll(".js-close-modal");
        closeButtons.forEach((btn) =>
            btn.addEventListener("click", () =>
                summaryModal.classList.remove("active")
            )
        );
        summaryModal.addEventListener("click", (e) => {
            if (e.target === summaryModal)
                summaryModal.classList.remove("active");
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

function bindAddCopyTriggers() {
    const modal = document.getElementById("modal-add-copy");
    if (!modal) return;

    const bookIdInput = document.getElementById("addCopyBookId");
    const bookTitleSpan = document.getElementById("addCopyBookTitle");
    const triggers = document.querySelectorAll(".js-add-copy");

    triggers.forEach((btn) =>
        btn.addEventListener("click", () => {
            bookIdInput.value = btn.dataset.id;
            bookTitleSpan.textContent = btn.dataset.title;

            // Fetch and display existing copies
            const listContainer = document.getElementById(
                "addCopyExistingList"
            );
            if (listContainer) {
                listContainer.innerHTML =
                    '<div style="padding:1rem; text-align:center; color:#666;">Loading copies...</div>';

                fetch(`/Book/GetCopiesList?bookId=${btn.dataset.id}`)
                    .then((res) => res.text())
                    .then((html) => {
                        listContainer.innerHTML = html;
                    })
                    .catch((err) => {
                        console.error(err);
                        listContainer.innerHTML =
                            '<div style="padding:1rem; text-align:center; color:var(--color-error);">Failed to load copies.</div>';
                    });
            }

            modal.classList.add("active");
        })
    );
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

document.addEventListener("DOMContentLoaded", () => {
    // Initialize features for management tables (Genre/Author/Book)
    initializeTableManagement();
});
