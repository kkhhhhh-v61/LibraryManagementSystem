document.addEventListener("DOMContentLoaded", function () {
    const banner = document.getElementById("banner-showcase");
    const bannerNav = document.getElementById("banner-nav");
    const bannerWrapper = document.getElementById("banner-wrapper");

    if (!banner || !bannerNav || !bannerWrapper) {
        return;
    }

    const images = banner.querySelectorAll("img");
    let currentIndex = 0;
    let autoScrollInterval;
    let manualNavTimeout;
    const scrollInterval = 5000;

    function updateNav(index) {
        const navItems = bannerNav.querySelectorAll(".banner-nav-item");
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

            banner.scrollLeft = scrollLeft;
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
        navItem.classList.add("banner-nav-item");
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
