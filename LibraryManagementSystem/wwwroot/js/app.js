$("[type=reset]").on("click", (e) => {
    e.preventDefault();
    location = location;
});

$("[data-trim]").on("change", (e) => {
    e.target.value = e.target.value.trim();
});

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

/**
 * Displays a global toast notification.
 * @param {string} message - The message to display.
 * @param {string} type - 'success', 'error', 'warning', or 'info' (default).
 * @param {number} duration - Duration in ms (default 3000).
 */
window.showToast = (message, type = "info", duration = 3000) => {
    const $container = $("#toast-container");
    const $toast = $("<div>").addClass(`toast toast--${type}`).text(message);

    $container.append($toast);

    void $toast[0].offsetWidth;

    $toast.addClass("toast--visible");

    setTimeout(() => {
        $toast.removeClass("toast--visible");
        setTimeout(() => $toast.remove(), 300);
    }, duration);
};
