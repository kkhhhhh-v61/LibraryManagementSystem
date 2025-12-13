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
