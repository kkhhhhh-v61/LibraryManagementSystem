document.addEventListener("DOMContentLoaded", function () {
    /* ==========================================================================
       HOME: Banner Showcase
       ========================================================================== */
    const bannerShowcase = document.getElementById("banner-showcase");
    const bannerNav = document.querySelector(".banner__nav");
    const bannerWrapper = document.querySelector(".banner");

    if (!bannerShowcase || !bannerNav || !bannerWrapper) {
        return;
    }

    const slides = bannerShowcase.querySelectorAll(".banner__slide");
    const prevButton = document.querySelector(".banner__arrow--prev");
    const nextButton = document.querySelector(".banner__arrow--next");
    let currentIndex = 0;
    let autoScrollInterval;
    const scrollInterval = 5500;

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

    function showSlide(index) {
        slides.forEach((slide, i) => {
            slide.classList.remove("active");
        });
        if (slides[index]) {
            slides[index].classList.add("active");
            updateNav(index);
        }
    }

    function startAutoScroll() {
        stopAutoScroll(); // Ensure no multiple intervals are running
        autoScrollInterval = setInterval(() => {
            currentIndex = (currentIndex + 1) % slides.length;
            showSlide(currentIndex);
        }, scrollInterval);
    }

    function stopAutoScroll() {
        clearInterval(autoScrollInterval);
    }

    slides.forEach((_, index) => {
        const navItem = document.createElement("div");
        navItem.classList.add("banner__nav-item");
        navItem.addEventListener("click", () => {
            currentIndex = index;
            showSlide(index);
            stopAutoScroll();
            startAutoScroll();
        });
        bannerNav.appendChild(navItem);
    });

    prevButton.addEventListener("click", () => {
        currentIndex = (currentIndex - 1 + slides.length) % slides.length;
        showSlide(currentIndex);
        stopAutoScroll();
        startAutoScroll();
    });

    nextButton.addEventListener("click", () => {
        currentIndex = (currentIndex + 1) % slides.length;
        showSlide(currentIndex);
        stopAutoScroll();
        startAutoScroll();
    });

    showSlide(0);
    startAutoScroll();
});
