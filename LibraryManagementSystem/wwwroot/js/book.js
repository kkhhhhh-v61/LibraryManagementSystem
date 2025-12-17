document.documentElement.classList.add("js-enabled");

document.addEventListener("DOMContentLoaded", () => {
    if (sessionStorage.getItem("submissionSuccess") === "true") {
        sessionStorage.removeItem("temp_image_data");
        sessionStorage.removeItem("submissionSuccess");
    }
});

document.addEventListener("DOMContentLoaded", () => {
    const multiSelects = document.querySelectorAll(".field__multi-select");
    multiSelects.forEach(loadMultiSelect);
});

function loadMultiSelect(wrapper) {
    const searchContainer = wrapper.querySelector(".multi-select__search");
    const searchInput = wrapper.querySelector(".search__input");
    const dropdown = wrapper.querySelector(".multi-select__dropdown");
    const select = wrapper.querySelector(".multi-select__select");

    // Render chips and mark selected options
    Array.from(select.options).forEach((option) => {
        if (option.selected) {
            createChip(option.value, option.text);
            const dropdownOption = dropdown.querySelector(
                `.dropdown__option[data-value="${option.value}"]`
            );
            if (dropdownOption) dropdownOption.classList.add("selected");
        }
    });

    // Open dropdown when search input is focused
    searchInput.addEventListener("focus", () => {
        dropdown.classList.add("active");
    });

    // Close dropdown when clicking outside
    document.addEventListener("click", (e) => {
        if (!wrapper.contains(e.target)) {
            dropdown.classList.remove("active");
        }
    });

    // Filter dropdown options based on user input
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

        // Open dropdown if there are visible options
        dropdown.classList.toggle("active", hasVisibleOptions);
    });

    // Select option when clicked
    dropdown.addEventListener("click", (e) => {
        const option = e.target.closest(".dropdown__option");
        if (!option) return; // Not an option

        const value = option.dataset.value;
        const text = option.innerText;

        toggleOption(value, text, option);

        searchInput.value = "";
        dropdown
            .querySelectorAll(".dropdown__option")
            .forEach((o) => (o.style.display = "block"));
        searchInput.focus();
    });

    // Remove chips when 'X' button is clicked
    searchContainer.addEventListener("click", (e) => {
        const removeButton = e.target.closest(".chip__remove");
        if (removeButton) {
            e.stopPropagation();
            const chip = removeButton.closest(".multi-select__chip");
            const value = chip.dataset.value;

            // Find the corresponding option and un-highlight it
            const dropdownOption = dropdown.querySelector(
                `.dropdown__option[data-value="${value}"]`
            );
            toggleOption(value, null, dropdownOption);
        }
    });

    function toggleOption(value, text, option) {
        // Find the actual option
        const selectOption = select.querySelector(`option[value="${value}"]`);
        if (!selectOption) return;

        // Toggle selected state for actual option
        const isNowSelected = !selectOption.selected;
        selectOption.selected = isNowSelected;

        if (isNowSelected) {
            // Add chip and highlight option
            createChip(value, text);
            option.classList.add("selected");
        } else {
            // Remove chip and un-highlight option
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

document.addEventListener("DOMContentLoaded", () => {
    const uploadWrappers = document.querySelectorAll(".upload__wrapper");
    uploadWrappers.forEach(setupImageUpload);
});

function setupImageUpload(wrapper) {
    const input = wrapper.querySelector(".upload__input");
    const preview = wrapper.querySelector(".upload__preview");
    const removeButton = wrapper.querySelector(".upload__remove");
    const form = wrapper.closest("form");
    const STORAGE_KEY = "temp_image_data";

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
    }

    const storedBase64 = sessionStorage.getItem(STORAGE_KEY);
    if (storedBase64) {
        restoreFileState(storedBase64);
    }

    function handleFileUpdate(file) {
        if (!file) return;

        // Read file and show preview
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

document.addEventListener("DOMContentLoaded", () => {
    /*  ===========================================================================
        EDIT MODAL
        =========================================================================== */

    const editModal = document.getElementById("modal-edit");

    if (!editModal) return;

    const modalTitle = editModal.querySelector(".modal__title");
    const formIdInput = editModal.querySelector("input[name='Form.Id']");
    const formNameInput = editModal.querySelector("input[name='Form.Name']");
    const openButtons = document.querySelectorAll(".js-open-modal");
    const closeButton = editModal.querySelector(".js-close-modal");
    const cancelButton = editModal.querySelector(".js-cancel-modal");

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
        });
    });

    const closeEditModal = () => editModal.classList.remove("active");
    if (closeButton) closeButton.addEventListener("click", closeEditModal);
    if (cancelButton) cancelButton.addEventListener("click", closeEditModal);

    editModal.addEventListener("click", (e) => {
        if (e.target === editModal) closeEditModal();
    });

    const shouldOpen = document.getElementById("server-modal-trigger");
    if (shouldOpen) editModal.classList.add("active");

    /*  ===========================================================================
        DELETE MODAL
        =========================================================================== */

    const deleteModal = document.getElementById("modal-delete");

    if (!deleteModal) return;

    const deleteIdInput = deleteModal.querySelector("input[name='id']");
    const deleteNameDisplay = deleteModal.querySelector(".js-delete-name");
    const deleteButtons = document.querySelectorAll(".js-delete-trigger");
    const closeDeleteButton = deleteModal.querySelector(".js-close-modal");
    const cancelDeleteButton = deleteModal.querySelector(".js-cancel-delete");

    deleteButtons.forEach((button) => {
        button.addEventListener("click", () => {
            const id = button.dataset.id;
            const name = button.dataset.name;

            deleteIdInput.value = id;
            if (deleteNameDisplay) deleteNameDisplay.textContent = name;

            deleteModal.classList.add("active");
        });
    });

    const closeDeleteModal = () => deleteModal.classList.remove("active");

    if (closeDeleteButton)
        closeDeleteButton.addEventListener("click", closeDeleteModal);
    if (cancelDeleteButton)
        cancelDeleteButton.addEventListener("click", closeDeleteModal);

    deleteModal.addEventListener("click", (e) => {
        if (e.target === deleteModal) closeDeleteModal();
    });
});
