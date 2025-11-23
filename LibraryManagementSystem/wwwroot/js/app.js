document.addEventListener("DOMContentLoaded", function () {
    const banner = document.getElementById("banner-showcase");
    const bannerNav = document.getElementById("banner-nav");

    if (!banner || !bannerNav) {
        return; // Exit if elements are not found
    }

    const images = banner.querySelectorAll("img");
    let currentIndex = 0;

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

    // Create navigation bars
    images.forEach((_, index) => {
        const navItem = document.createElement("div");
        navItem.classList.add("banner-nav-item");
        navItem.addEventListener("click", () => {
            scrollToImage(index);
        });
        bannerNav.appendChild(navItem);
    });

    // Initial state
    scrollToImage(0); // Center the first image on load
});
