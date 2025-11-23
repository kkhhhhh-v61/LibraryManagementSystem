document.addEventListener("DOMContentLoaded", function () {
    /* ==========================================================================
        1. BANNER SHOWCASE
        ========================================================================== */
    const bannerShowcase = document.getElementById("banner-showcase");
    const bannerNav = document.querySelector(".banner__nav");
    const bannerWrapper = document.querySelector(".banner");

    if (!bannerShowcase || !bannerNav || !bannerWrapper) {
        return;
    }

    const images = bannerShowcase.querySelectorAll(".banner__image");
    let currentIndex = 0;
    let autoScrollInterval;
    let manualNavTimeout;
    const scrollInterval = 5000;

    function updateNav(index) {
        const navItems = bannerNav.querySelectorAll(".banner__nav-item");
        navItems.forEach((item, i) => {
            if (i === index) {
                item.classList.add("active");
            } else {
                item.classList.remove("active");
            }
        });
    }

    function scrollToImage(index) {
        const image = images[index];
        if (image) {
            const scrollLeft = image.offsetLeft;

            bannerShowcase.scrollLeft = scrollLeft;
            currentIndex = index;
            updateNav(index);
        }
    }

    function startAutoScroll() {
        stopAutoScroll(); // Ensure no multiple intervals are running
        autoScrollInterval = setInterval(() => {
            const nextIndex = (currentIndex + 1) % images.length;
            scrollToImage(nextIndex);
        }, scrollInterval);
    }

    function stopAutoScroll() {
        clearInterval(autoScrollInterval);
    }

    images.forEach((_, index) => {
        const navItem = document.createElement("div");
        navItem.classList.add("banner__nav-item");
        navItem.addEventListener("click", () => {
            scrollToImage(index);
            stopAutoScroll();
            clearTimeout(manualNavTimeout);
            manualNavTimeout = setTimeout(startAutoScroll, scrollInterval); // Restart after a delay
        });
        bannerNav.appendChild(navItem);
    });

    bannerWrapper.addEventListener("mouseenter", stopAutoScroll);
    bannerWrapper.addEventListener("mouseleave", startAutoScroll);

    scrollToImage(0);
    startAutoScroll();
});
