$(document).ready(function () {
    const $input = $("#book-cover-input");
    const $zone = $("#book-cover-zone");
    const $image = $("#book-cover-preview");
    const $removeBtn = $("#book-cover-remove");
    const $wrapper = $input.closest(".form__upload-wrapper");
    const $base64Input = $("#book-cover-temp");
    const $resetBtn = $(".button--reset");

    // File Selection Preview
    $input.on("change", function (e) {
        const file = e.target.files[0];

        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                $image.attr("src", e.target.result);
                $zone.addClass("has-image");
                $wrapper.addClass("has-file");
                sessionStorage.setItem("tempBookCover", e.target.result);
                $base64Input.val(e.target.result);
            };
            reader.readAsDataURL(file);
        }
    });

    $removeBtn.on("click", function (e) {
        e.preventDefault();
        e.stopPropagation();
        $input.val("");
        $image.attr("src", "");
        $zone.removeClass("has-image");
        $wrapper.removeClass("has-file");
        sessionStorage.removeItem("tempBookCover");
        $base64Input.val("");
    });

    $resetBtn.on("click", function () {
        // Manually trigger the remove logic for the image preview
        $removeBtn.click();
    });

    // On page load, check for a stored image in sessionStorage
    const storedImage = sessionStorage.getItem("tempBookCover");
    if (storedImage) {
        $image.attr("src", storedImage);
        $zone.addClass("has-image");
        $wrapper.addClass("has-file");
        $base64Input.val(storedImage);
    }
});

document.addEventListener("DOMContentLoaded", () => {
    if (sessionStorage.getItem("submissionSuccess") === "true") {
        sessionStorage.removeItem("tempBookCover");
        sessionStorage.removeItem("submissionSuccess");
    }
});

document.addEventListener("DOMContentLoaded", () => {
    const multiSelects = document.querySelectorAll(".form__multi-select");
    multiSelects.forEach(setupMultiSelect);
});

function setupMultiSelect(wrapper) {
    const container = wrapper.querySelector(".multi-select__container");
    const searchInput = wrapper.querySelector(".multi-select__search");
    const dropdown = wrapper.querySelector(".multi-select__dropdown");
    const options = dropdown.querySelectorAll(".multi-select__option");
    const hiddenSelect = wrapper.querySelector(".multi-select__hidden-input");
    const parentRowItem = wrapper.closest(".form__row-item");
    const label = parentRowItem.querySelector(".form__label");
    const errorSpan = parentRowItem.querySelector(".form__error");

    // Check for initial validation error on page load
    if (hiddenSelect.classList.contains("input-validation-error")) {
        // If there are no selected items, apply the error style
        if (hiddenSelect.selectedOptions.length === 0) {
            container.classList.add("has-error");
            label.classList.add("has-error");
            if (errorSpan) errorSpan.style.display = "block";
        }
    }

    // Check for pre-selected options
    Array.from(hiddenSelect.options).forEach((option) => {
        if (option.selected) {
            const value = option.value;
            const text = option.text;
            addChip(value, text);
            dropdown
                .querySelector(`.multi-select__option[data-value="${value}"]`)
                .classList.add("selected");
        }
    });

    // Toggle Dropdown on Focus
    searchInput.addEventListener("focus", () => {
        dropdown.classList.add("active");
    });

    // Close dropdown when clicking outside
    document.addEventListener("click", (e) => {
        if (!wrapper.contains(e.target)) {
            dropdown.classList.remove("active");
        }
    });

    // Filter Options (Search)
    searchInput.addEventListener("input", (e) => {
        const term = e.target.value.toLowerCase();
        let hasVisibleOptions = false;

        options.forEach((option) => {
            const text = option.innerText.toLowerCase();
            const isVisible = text.includes(term);

            option.style.display = isVisible ? "block" : "none";
            if (isVisible) {
                hasVisibleOptions = true;
            }
        });

        // Only show the dropdown if there are matching options
        dropdown.classList.toggle("active", hasVisibleOptions);
    });

    // Handle Option Click
    options.forEach((option) => {
        option.addEventListener("click", () => {
            const value = option.dataset.value;
            const text = option.innerText;

            if (option.classList.contains("selected")) {
                // If already selected, remove it
                removeChip(value);
            } else {
                // If not selected, select it
                addChip(value, text);
                option.classList.add("selected");
            }

            // Reset Search
            searchInput.value = "";
            options.forEach((o) => (o.style.display = "block"));
            searchInput.focus();
        });
    });

    function addChip(value, text) {
        const chip = document.createElement("div");
        chip.classList.add("multi-select__chip");
        chip.dataset.value = value;
        chip.innerHTML = `${text} 
            <span class="multi-select__chip-remove">
                <svg xmlns="http://www.w3.org/2000/svg" height="16px" viewBox="0 -960 960 960" width="24px" fill="#000000" class="multi-select__chip-remove-icon">
                    <path d="m256-200-56-56 224-224-224-224 56-56 224 224 224-224 56 56-224 224 224 224-56 56-224-224-224 224Z"/>
                </svg>
            </span>`;

        chip.querySelector(".multi-select__chip-remove").addEventListener(
            "click",
            (e) => {
                e.stopPropagation();
                removeChip(value);
            }
        );

        container.insertBefore(chip, searchInput);

        updateHiddenSelect(value, true);

        updateValidationState();
    }

    function removeChip(value) {
        // Remove chip
        const chip = container.querySelector(
            `.multi-select__chip[data-value="${value}"]`
        );
        if (chip) chip.remove();

        // Update Dropdown UI
        const option = dropdown.querySelector(
            `.multi-select__option[data-value="${value}"]`
        );
        if (option) option.classList.remove("selected");

        // Update Hidden Select
        updateHiddenSelect(value, false);

        updateValidationState();
    }

    function updateHiddenSelect(value, isSelected) {
        const option = hiddenSelect.querySelector(`option[value="${value}"]`);
        if (option) {
            option.selected = isSelected;
        }
    }

    function updateValidationState() {
        // Only run this logic if the field was initially invalid
        if (!hiddenSelect.classList.contains("input-validation-error")) return;

        const selectedCount = hiddenSelect.selectedOptions.length;

        // Add or remove the 'has-error' class based on the selection count
        container.classList.toggle("has-error", selectedCount === 0);
        label.classList.toggle("has-error", selectedCount === 0);
        if (errorSpan) {
            errorSpan.style.display = selectedCount === 0 ? "block" : "none";
        }
    }
}
