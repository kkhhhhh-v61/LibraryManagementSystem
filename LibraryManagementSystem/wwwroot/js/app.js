$(document).ready(function () {
    const $body = $("body");
    const $logo = $(".logo");

    const isCollapsed = localStorage.getItem("sidebar-collapsed") === "true";
    if (isCollapsed) {
        $body.addClass("layout--collapsed");
    }

    $logo.on("click", function (e) {
        e.preventDefault();
        $body.toggleClass("layout--collapsed");
        const hasClass = $body.hasClass("layout--collapsed");
        localStorage.setItem("sidebar-collapsed", hasClass);
    });
});
