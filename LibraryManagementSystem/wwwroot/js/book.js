﻿/* ===========================================================================
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
   CATALOGUE PAGE LOGIC
   =========================================================================== */

function initializeCatalogue() {
    const filterMenu = document.getElementById("catalogueFilterMenu");
    const filterTrigger = document.getElementById("catalogueFilterTrigger");
    const clearBtn = document.getElementById("catalogueClearFilters");
    const searchInput = document.getElementById("catalogueSearchInput");
    const gridWrapper = document.getElementById("catalogueGrid");
    const filterGenre = document.getElementById("filterGenre");
    const filterAuthor = document.getElementById("filterAuthor");
    const filterRating = document.getElementById("filterRating");
    const filterYear = document.getElementById("filterYear");
    const filterEdition = document.getElementById("filterEdition");
    const searchClear = document.getElementById("catalogueSearchClear");
    // NEW: Sort Menu
    const sortMenu = document.getElementById("catalogueSortMenu");
    const sortTrigger = document.getElementById("catalogueSortTrigger");
    const sortDropdown = document.getElementById("sortDropdownMenu");
    const resetSortBtn = document.getElementById("catalogueResetSort");

    if (filterMenu && filterTrigger) {
        filterTrigger.addEventListener("click", (e) => {
            e.stopPropagation();
            if (sortMenu) sortMenu.classList.remove("active");
            filterMenu.classList.toggle("active");
        });
    }

    // NEW: Sort Menu Logic
    if (sortMenu && sortTrigger) {
        sortTrigger.addEventListener("click", (e) => {
            e.stopPropagation();
            if (filterMenu) filterMenu.classList.remove("active");
            sortMenu.classList.toggle("active");
        });
    }

    // NEW: Combined click-outside-to-close handler
    document.addEventListener("click", (e) => {
        if (filterMenu && !filterMenu.contains(e.target)) {
            filterMenu.classList.remove("active");
        }
        if (sortMenu && !sortMenu.contains(e.target)) {
            sortMenu.classList.remove("active");
        }
    });

    if (sortDropdown) {
        sortDropdown.addEventListener("change", (e) => {
            if (e.target.name === "sort") {
                debouncedSearch();
                sortMenu.classList.remove("active");
            }
        });
    }

    if (resetSortBtn) {
        resetSortBtn.addEventListener("click", (e) => {
            e.stopPropagation();
            const checkedRadio = document.querySelector(
                'input[name="sort"]:checked'
            );
            if (checkedRadio) {
                checkedRadio.checked = false;
            }
            debouncedSearch();
            if (sortMenu) {
                sortMenu.classList.remove("active");
            }
        });
    }

    let debounceTimer;
    const performSearch = () => {
        const term = searchInput ? searchInput.value : "";
        const genreId = filterGenre ? filterGenre.value : "";
        const authorId = filterAuthor ? filterAuthor.value : "";
        const rating = filterRating ? filterRating.value : "";
        const year = filterYear ? filterYear.value : "";
        const edition = filterEdition ? filterEdition.value : "";

        const params = new URLSearchParams();
        if (term) params.set("search", term);
        if (genreId) params.set("genreId", genreId);
        if (authorId) params.set("authorId", authorId);
        if (rating) params.set("rating", rating);
        if (year) params.set("year", year);
        if (edition) params.set("edition", edition);

        // Get sort values
        const sortRadio = document.querySelector('input[name="sort"]:checked');
        if (sortRadio) {
            const [sort, order] = sortRadio.value.split("-");
            params.set("sort", sort);
            params.set("order", order);
        }

        fetch(`/Book/Catalogue?${params.toString()}`, {
            headers: { "X-Requested-With": "XMLHttpRequest" },
        })
            .then((response) => response.text())
            .then((html) => {
                if (gridWrapper) gridWrapper.innerHTML = html;
            })
            .catch((err) => console.error("Catalogue search failed", err));
    };

    const debouncedSearch = () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(performSearch, 300);
    };

    if (searchInput) {
        searchInput.addEventListener("input", (e) => {
            debouncedSearch();
            if (searchClear) {
                if (e.target.value.length > 0)
                    searchClear.classList.add("active");
                else searchClear.classList.remove("active");
            }
        });
    }

    if (searchClear) {
        searchClear.addEventListener("click", () => {
            searchInput.value = "";
            searchClear.classList.remove("active");
            // Trigger input event to update search results
            searchInput.dispatchEvent(new Event("input"));
            searchInput.focus();
        });
    }

    if (filterGenre) {
        filterGenre.addEventListener("change", debouncedSearch);
    }

    if (filterAuthor) {
        filterAuthor.addEventListener("change", debouncedSearch);
    }

    if (filterRating) {
        filterRating.addEventListener("change", debouncedSearch);
    }

    if (filterYear) {
        filterYear.addEventListener("change", debouncedSearch);
    }

    if (filterEdition) {
        filterEdition.addEventListener("change", debouncedSearch);
    }

    if (clearBtn) {
        clearBtn.addEventListener("click", (e) => {
            e.stopPropagation();
            // Clear custom dropdowns
            const dropdowns = filterMenu.querySelectorAll(".filter-dropdown");
            dropdowns.forEach((d) => {
                const clear = d.querySelector(".filter-clear");
                if (clear) clear.click();
            });
            filterMenu.classList.remove("active");
        });
    }
}

/* ===========================================================================
   BOOK DETAIL PAGE LOGIC
   =========================================================================== */

function initializeBookDetails() {
    const loanModal = document.getElementById("modal-loan-confirm");

    if (loanModal) {
        const loanBtn = document.querySelector(".js-loan-trigger");
        const closeBtns = loanModal.querySelectorAll(".js-close-modal");
        const bookIdInput = loanModal.querySelector(".js-loan-book-id");
        const bookTitleSpan = loanModal.querySelector(".js-loan-book-title");

        if (loanBtn) {
            loanBtn.addEventListener("click", function () {
                bookIdInput.value = this.dataset.bookId;
                bookTitleSpan.textContent = this.dataset.bookTitle;

                const options = {
                    year: "numeric",
                    month: "short",
                    day: "numeric",
                };
                const today = new Date();
                const pickup = new Date();
                pickup.setDate(today.getDate() + 3);

                loanModal.querySelector(".js-loan-date").textContent =
                    today.toLocaleDateString("en-US", options);
                loanModal.querySelector(".js-pickup-date").textContent =
                    pickup.toLocaleDateString("en-US", options);
                loanModal.querySelector(".js-loan-duration").textContent =
                    "7 Days (from pickup)";

                loanModal.classList.add("active");
            });
        }

        closeBtns.forEach((btn) => {
            btn.addEventListener("click", () => {
                loanModal.classList.remove("active");
            });
        });

        loanModal.addEventListener("click", (e) => {
            if (e.target === loanModal) loanModal.classList.remove("active");
        });
    }

    // Reserve Modal Logic
    const reserveModal = document.getElementById("modal-reserve");
    if (reserveModal) {
        const reserveBtn = document.querySelector(".js-reserve-trigger");
        const reserveCloseBtns =
            reserveModal.querySelectorAll(".js-close-modal");

        if (reserveBtn) {
            reserveBtn.addEventListener("click", function () {
                reserveModal.classList.add("active");
            });
        }

        reserveCloseBtns.forEach((btn) => {
            btn.addEventListener("click", () => {
                reserveModal.classList.remove("active");
            });
        });

        reserveModal.addEventListener("click", (e) => {
            if (e.target === reserveModal)
                reserveModal.classList.remove("active");
        });
    }
}

function initializeCancelTrigger() {
    const cancelModal = document.getElementById("modal-cancel-item");
    if (!cancelModal) return;

    const cancelForm = document.getElementById("cancelItemForm");
    const cancelTypeSpan = document.getElementById("cancelItemType");
    const cancelNameSpan = document.getElementById("cancelItemName");
    const cancelIdInput = document.getElementById("cancelItemId");

    document.body.addEventListener("click", function (e) {
        const btn = e.target.closest(".js-cancel-trigger");
        if (btn) {
            const type = btn.dataset.type; // "loan" or "reservation"
            const id = btn.dataset.id;
            const bookTitle = btn.dataset.bookTitle;

            if (type === "loan") {
                cancelForm.action = "/Loan/CancelLoan";
                cancelTypeSpan.textContent = "loan";
            } else {
                cancelForm.action = "/Reservation/Cancel";
                cancelTypeSpan.textContent = "reservation";
            }

            cancelIdInput.value = id;
            cancelNameSpan.textContent = bookTitle;

            cancelModal.classList.add("active");
        }
    });

    cancelModal.querySelectorAll(".js-close-modal").forEach((btn) => {
        btn.addEventListener("click", () =>
            cancelModal.classList.remove("active")
        );
    });

    cancelModal.addEventListener("click", (e) => {
        if (e.target === cancelModal) cancelModal.classList.remove("active");
    });
}

/* ===========================================================================
   BOOK COVER CROPPER
   =========================================================================== */

function initializeBookCropper() {
    const input = document.getElementById("Image"); // asp-for="Image"
    if (!input) return;

    const modal = document.getElementById("modal-book-crop");
    const image = document.getElementById("book-crop-image");
    const confirmBtn = document.getElementById("btn-confirm-crop");
    const resetBtn = document.getElementById("btn-reset-crop");
    const closeBtns = modal.querySelectorAll(".js-close-modal");
    let cropper;

    input.addEventListener("change", function (e) {
        if (e.target.files && e.target.files.length > 0) {
            const file = e.target.files[0];
            const reader = new FileReader();
            reader.onload = function (evt) {
                // Ensure image is loaded before initializing cropper
                image.onload = function () {
                    if (cropper) cropper.destroy();
                    cropper = new Cropper(image, {
                        aspectRatio: 2 / 3,
                        viewMode: 1,
                        autoCropArea: 1,
                        background: false,
                    });
                };
                image.src = evt.target.result;
                modal.classList.add("active");
            };
            reader.readAsDataURL(file);
        }
    });

    confirmBtn.addEventListener("click", function () {
        if (!cropper) return;
        const data = cropper.getData(true); // true for rounded integers

        document.getElementById("CropX").value = data.x;
        document.getElementById("CropY").value = data.y;
        document.getElementById("CropWidth").value = data.width;
        document.getElementById("CropHeight").value = data.height;

        // Update preview with cropped version
        const canvas = cropper.getCroppedCanvas();
        const preview = document.querySelector(".upload__preview");
        const wrapper = document.querySelector(".upload__wrapper");

        if (preview && wrapper) {
            preview.src = canvas.toDataURL();
            wrapper.classList.add("has-image");
        }

        modal.classList.remove("active");
    });

    if (resetBtn) {
        resetBtn.addEventListener("click", () => {
            if (cropper) cropper.reset();
        });
    }

    closeBtns.forEach((btn) =>
        btn.addEventListener("click", () => {
            modal.classList.remove("active");
            input.value = ""; // Clear input if cancelled
        })
    );
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

    // Initialize Catalogue Page
    initializeCatalogue();

    // Initialize Book Details Page
    initializeBookDetails();

    // Initialize Cancel Trigger for Loan/Reservation
    initializeCancelTrigger();

    // Initialize Book Cropper
    initializeBookCropper();

    // Initialize Review Filter
    initializeReviewFilter();
});

/* ===========================================================================
   REVIEW FILTERING & PAGINATION
   =========================================================================== */

function initializeReviewFilter() {
    const dropdown = document.getElementById("reviewRatingDropdown");
    if (!dropdown) return;

    const trigger = dropdown.querySelector(".filter-dropdown__trigger");
    const items = dropdown.querySelectorAll(".filter-dropdown__item");
    const triggerText = dropdown.querySelector(".trigger__text");
    const bookId = dropdown.dataset.bookId;

    trigger.addEventListener("click", (e) => {
        e.stopPropagation();
        dropdown.classList.toggle("active");
    });

    document.addEventListener("click", (e) => {
        if (!dropdown.contains(e.target)) {
            dropdown.classList.remove("active");
        }
    });

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

            // Trigger Load
            loadReviews(bookId, value, 1);
        });
    });
}

window.loadReviews = function (bookId, rating, page) {
    const container = document.getElementById("reviewsContainer");
    if (!container) return;

    // Show loading state
    container.style.opacity = "0.5";

    let url = `/Book/BookDetails?id=${bookId}&page=${page}`;
    if (rating) {
        url += `&rating=${rating}`;
    }

    fetch(url, {
        headers: { "X-Requested-With": "XMLHttpRequest" },
    })
        .then((res) => res.text())
        .then((html) => {
            container.innerHTML = html;
            container.style.opacity = "1";
        })
        .catch((err) => {
            console.error(err);
            container.style.opacity = "1";
        });
};
