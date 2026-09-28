/* ============================================
   RezerveApp — Main JavaScript
   Navigation, scroll effects, animations
   ============================================ */

document.addEventListener('DOMContentLoaded', function () {

    // === Navigation Scroll Effect ===
    const nav = document.getElementById('mainNav');

    if (nav) {
        let lastScroll = 0;

        window.addEventListener('scroll', function () {
            const currentScroll = window.pageYOffset;

            if (currentScroll > 50) {
                nav.classList.add('nav--scrolled');
            } else {
                nav.classList.remove('nav--scrolled');
            }

            lastScroll = currentScroll;
        }, { passive: true });
    }


    // === Mobile Menu ===
    const hamburger = document.getElementById('navHamburger');
    const mobileMenu = document.getElementById('navMobile');
    const overlay = document.getElementById('navOverlay');

    function toggleMobile() {
        const isOpen = mobileMenu.classList.contains('open');

        if (isOpen) {
            closeMobile();
        } else {
            mobileMenu.classList.add('open');
            overlay.style.display = 'block';
            hamburger.classList.add('active');
            document.body.style.overflow = 'hidden';
            // Trigger overlay fade-in
            requestAnimationFrame(function () {
                overlay.classList.add('visible');
            });
        }
    }

    function closeMobile() {
        mobileMenu.classList.remove('open');
        overlay.classList.remove('visible');
        hamburger.classList.remove('active');
        document.body.style.overflow = '';
        setTimeout(function () {
            overlay.style.display = 'none';
        }, 250);
    }

    if (hamburger) {
        hamburger.addEventListener('click', toggleMobile);
    }

    if (overlay) {
        overlay.addEventListener('click', closeMobile);
    }

    // Close mobile menu on link click
    if (mobileMenu) {
        const mobileLinks = mobileMenu.querySelectorAll('.nav__link');
        mobileLinks.forEach(function (link) {
            link.addEventListener('click', closeMobile);
        });
    }


    // === Scroll Reveal Animations ===
    const revealElements = document.querySelectorAll('.reveal');

    if (revealElements.length > 0) {
        const revealObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('revealed');
                    revealObserver.unobserve(entry.target);
                }
            });
        }, {
            threshold: 0.1,
            rootMargin: '0px 0px -40px 0px'
        });

        revealElements.forEach(function (el) {
            revealObserver.observe(el);
        });
    }


    // === Smooth Scroll for anchor links ===
    document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
        anchor.addEventListener('click', function (e) {
            const targetId = this.getAttribute('href');
            if (targetId === '#') return;

            const target = document.querySelector(targetId);
            if (target) {
                e.preventDefault();
                const offset = nav ? nav.offsetHeight + 16 : 80;
                const top = target.getBoundingClientRect().top + window.pageYOffset - offset;

                window.scrollTo({
                    top: top,
                    behavior: 'smooth'
                });
            }
        });
    });


    // === Appointment Mockup Interactivity ===
    // Service selection
    document.querySelectorAll('.mockup-service').forEach(function (service) {
        service.addEventListener('click', function () {
            document.querySelectorAll('.mockup-service').forEach(function (s) {
                s.classList.remove('selected');
            });
            this.classList.add('selected');
        });
    });

    // Barber selection
    document.querySelectorAll('.mockup-barber').forEach(function (barber) {
        barber.addEventListener('click', function () {
            document.querySelectorAll('.mockup-barber').forEach(function (b) {
                b.classList.remove('selected');
            });
            this.classList.add('selected');
        });
    });

    // Time slot selection
    document.querySelectorAll('.mockup-time').forEach(function (time) {
        time.addEventListener('click', function () {
            document.querySelectorAll('.mockup-time').forEach(function (t) {
                t.classList.remove('selected');
            });
            this.classList.add('selected');
        });
    });

});

