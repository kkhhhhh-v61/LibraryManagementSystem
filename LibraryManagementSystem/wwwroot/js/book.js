$(document).ready(function () {
    const $input = $("#book-cover-input");
    const $zone = $("#book-cover-zone");
    const $image = $("#book-cover-preview");
    const $removeBtn = $("#book-cover-remove");
    const $wrapper = $input.closest(".form__upload-wrapper");

    // File Selection Preview
    $input.on("change", function (e) {
        const file = e.target.files[0];

        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                $image.attr("src", e.target.result);
                $zone.addClass("has-image");
                $wrapper.addClass("has-file");
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
    });
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
            const value = option.dataset.value; // Get string ID
            const text = option.innerText;

            addChip(value, text);
            updateHiddenSelect(value, true);

            option.classList.add("selected");

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
        chip.innerHTML = `${text} <span class="multi-select__chip-remove">&times;</span>`;

        chip.querySelector(".multi-select__chip-remove").addEventListener(
            "click",
            (e) => {
                e.stopPropagation();
                removeChip(value);
            }
        );

        container.insertBefore(chip, searchInput);
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
    }

    function updateHiddenSelect(value, isSelected) {
        const option = hiddenSelect.querySelector(`option[value="${value}"]`);
        if (option) {
            option.selected = isSelected;
        }
    }
}
