$(document).ready(function () {
    const $input = $("#book-cover-input");
    const $zone = $("#book-cover-zone");
    const $image = $("#book-cover-preview");

    $input.on("change", function (e) {
        const file = e.target.files[0];

        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                $image.attr("src", e.target.result);
                $zone.addClass("has-image");
            };
            reader.readAsDataURL(file);
        } else {
            $image.attr("src", "#");
            $zone.removeClass("has-image");
        }
    });
});
