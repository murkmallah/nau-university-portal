/* ===== SEARCH ===== */
let searchDebounceTimer = null;

function executeSearchQuery() {
    const box = document.getElementById("searchBox");
    if (!box) return;
    const query = box.value.trim();
    if (query.length > 0) {
        window.location.href = "/Search?q=" + encodeURIComponent(query);
    }
}

function toggleSearch(event) {
    if (event && typeof event.stopPropagation === 'function') event.stopPropagation();
    let box = document.getElementById("searchBox");
    const icon = document.querySelector(".search-icon");

    if (!box) {
        box = document.createElement("input");
        box.id = "searchBox";
        box.className = "search-box";
        box.placeholder = "Search...";
        const wrap = document.querySelector(".search-wrap") || document.querySelector(".nav-search-wrap") || (icon ? icon.parentElement : null);
        if (wrap) {
            wrap.appendChild(box);
        } else {
            document.body.appendChild(box);
        }
    }

    initSearchBoxEvents(box);

    if (box.classList.contains("open") && box.value.trim().length > 0) {
        executeSearchQuery();
        return;
    }

    box.classList.toggle("open");
    if (box.classList.contains("open")) {
        box.focus();
    } else {
        closeSearchDropdown();
    }
}

function closeSearch() {
    setTimeout(() => {
        closeSearchDropdown();
        const box = document.getElementById("searchBox");
        if (box && document.activeElement !== box && (!box.value || box.value.trim().length === 0)) {
            box.classList.remove("open");
        }
    }, 250);
}

function closeSearchDropdown() {
    const dropdown = document.getElementById("nauSearchLiveDropdown");
    if (dropdown) dropdown.style.display = "none";
}

function initSearchBoxEvents(box) {
    if (!box || box.dataset.searchInitialized) return;
    box.dataset.searchInitialized = "true";

    box.addEventListener("keydown", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
            executeSearchQuery();
        } else if (e.key === "Escape") {
            box.classList.remove("open");
            closeSearchDropdown();
        }
    });

    box.addEventListener("input", function () {
        const query = box.value.trim();
        clearTimeout(searchDebounceTimer);
        if (query.length < 2) {
            closeSearchDropdown();
            return;
        }
        searchDebounceTimer = setTimeout(() => {
            fetchLiveSearchResults(query);
        }, 250);
    });
}

function fetchLiveSearchResults(query) {
    fetch("/Search/Api?q=" + encodeURIComponent(query))
        .then(res => res.json())
        .then(data => renderLiveSearchDropdown(data, query))
        .catch(err => console.error("Search API error:", err));
}

function renderLiveSearchDropdown(results, query) {
    let dropdown = document.getElementById("nauSearchLiveDropdown");
    const box = document.getElementById("searchBox");
    if (!box) return;

    if (!dropdown) {
        dropdown = document.createElement("div");
        dropdown.id = "nauSearchLiveDropdown";
        dropdown.className = "nau-search-live-dropdown";
        document.body.appendChild(dropdown);
    }

    if (!results || results.length === 0) {
        dropdown.innerHTML = `<div class="nau-search-no-items">No quick results for "${escapeHtml(query)}". Press Enter for full search.</div>`;
    } else {
        let html = results.map(item => `
            <a href="${item.url}" class="nau-search-item">
                <i class="${item.icon || 'fas fa-search'}"></i>
                <div class="nau-search-item-info">
                    <span class="nau-search-item-title">${escapeHtml(item.title)}</span>
                    <span class="nau-search-item-cat">${escapeHtml(item.category)}</span>
                </div>
            </a>
        `).join("");
        html += `<a href="/Search?q=${encodeURIComponent(query)}" class="nau-search-view-all">View all results for "${escapeHtml(query)}" &rarr;</a>`;
        dropdown.innerHTML = html;
    }

    const rect = box.getBoundingClientRect();
    dropdown.style.position = "fixed";
    dropdown.style.top = (rect.bottom + 6) + "px";
    dropdown.style.left = Math.max(10, rect.right - 300) + "px";
    dropdown.style.width = "300px";
    dropdown.style.display = "block";
    dropdown.style.zIndex = "999999";
}

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

document.addEventListener("DOMContentLoaded", function () {
    const box = document.getElementById("searchBox");
    if (box) initSearchBoxEvents(box);
});



/* ===== DYNAMIC HERO IMAGE & VIDEO SLIDESHOW ===== */
let currentSlide = 0;
let slideTimer = null;

function initSlideshow(targetIndex) {
    const hero = document.querySelector('.hero');
    const slides = document.querySelectorAll('.hero-slide');
    const dotsContainer = document.getElementById('slideDots');
    const prevBtn = document.querySelector('.slide-arrow.prev');
    const nextBtn = document.querySelector('.slide-arrow.next');

    if (!slides || slides.length === 0) {
        if (dotsContainer) {
            dotsContainer.innerHTML = '';
            dotsContainer.style.display = 'none';
        }
        if (prevBtn) prevBtn.style.display = 'none';
        if (nextBtn) nextBtn.style.display = 'none';
        clearInterval(slideTimer);
        return;
    }

    const total = slides.length;

    // Determine target slide index
    if (typeof targetIndex === 'number' && targetIndex >= 0 && targetIndex < total) {
        currentSlide = targetIndex;
    } else if (currentSlide >= total || currentSlide < 0) {
        currentSlide = 0;
    }

    // Ensure only current slide is active
    slides.forEach((s, idx) => {
        if (idx === currentSlide) {
            s.classList.add('active');
        } else {
            s.classList.remove('active');
        }
    });

    // Control navigation arrows visibility
    if (prevBtn) prevBtn.style.display = total > 1 ? 'flex' : 'none';
    if (nextBtn) nextBtn.style.display = total > 1 ? 'flex' : 'none';

    // Build dynamic slide dots
    if (dotsContainer) {
        dotsContainer.innerHTML = '';
        if (total > 1) {
            dotsContainer.style.display = 'flex';
            for (let i = 0; i < total; i++) {
                const dot = document.createElement('button');
                dot.type = 'button';
                dot.className = 'slide-dot' + (i === currentSlide ? ' active' : '');
                dot.setAttribute('aria-label', `Slide ${i + 1}`);
                dot.onclick = () => {
                    clearInterval(slideTimer);
                    goToSlide(i);
                };
                dotsContainer.appendChild(dot);
            }
        } else {
            dotsContainer.style.display = 'none';
        }
    }

    // Check video playback on active slide
    handleSlideMedia(currentSlide);

    // Setup hero hover to pause auto-slide if not already bound
    if (hero && !hero._slideHoverBound) {
        hero._slideHoverBound = true;
        hero.addEventListener('mouseenter', () => clearInterval(slideTimer));
        hero.addEventListener('mouseleave', () => startAutoSlide());
    }

    startAutoSlide();
}

function handleSlideMedia(slideIdx) {
    const slides = document.querySelectorAll('.hero-slide');
    if (!slides || !slides[slideIdx]) return;

    slides.forEach((s, idx) => {
        const vid = s.querySelector('video');
        if (vid) {
            if (idx === slideIdx) {
                vid.loop = true;
                vid.play().catch(() => {});
            } else {
                vid.pause();
            }
        }
    });
}

function goToSlide(n) {
    const slides = document.querySelectorAll('.hero-slide');
    const dots = document.querySelectorAll('.slide-dot');
    if (!slides || slides.length === 0) return;

    const total = slides.length;
    if (slides[currentSlide]) slides[currentSlide].classList.remove('active');
    if (dots && dots[currentSlide]) dots[currentSlide].classList.remove('active');

    currentSlide = ((n % total) + total) % total;

    if (slides[currentSlide]) slides[currentSlide].classList.add('active');
    if (dots && dots[currentSlide]) dots[currentSlide].classList.add('active');

    handleSlideMedia(currentSlide);
    startAutoSlide();
}

function slidePrev() {
    clearInterval(slideTimer);
    goToSlide(currentSlide - 1);
}

function slideNext() {
    clearInterval(slideTimer);
    goToSlide(currentSlide + 1);
}

function startAutoSlide() {
    clearInterval(slideTimer);
    const slides = document.querySelectorAll('.hero-slide');
    if (!slides || slides.length <= 1) return;

    const activeSlide = slides[currentSlide];
    if (activeSlide && activeSlide.querySelector('video')) {
        // Active slide contains a video: do NOT auto-advance.
        // Video will keep playing continuously until the user manually clicks next/prev arrow or a dot.
        return;
    }

    slideTimer = setInterval(() => {
        goToSlide(currentSlide + 1);
    }, 6000);
}

// Global hooks for inline onclicks and CMS editor
window.slidePrev = slidePrev;
window.slideNext = slideNext;
window.goToSlide = goToSlide;
window.initSlideshow = initSlideshow;
window.refreshHeroSlideshow = function(targetIndex) {
    initSlideshow(targetIndex);
};

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => initSlideshow());
} else {
    initSlideshow();
}

/* ===== ACCOUNT DROPDOWN (Sign Up / Log In) =====
   Progressively enhances the "Account" button (class="apply-btn") into a
   dropdown with Log In / Sign Up options. Works regardless of exactly
   where the button sits in the saved homepage content.
   Hover AND click are both handled directly in JS (not just via CSS
   :hover) so the menu reliably opens on mouseover and each link
   navigates straight to its own page. */
(function setupAccountDropdown() {
    // Tracked in memory only (never written to the DOM) so it can never get
    // captured and saved into the CMS content -- that was the bug: a
    // data-attribute marker was getting baked into the saved homepage HTML,
    // so on the next load the script saw "already enhanced" and skipped
    // creating the menu and listeners entirely.
    var enhancedButtons = new WeakSet();

    function getAuth() {
        return window.NAU_AUTH || { isAuthenticated: false, isAdmin: false, userName: '' };
    }

    function enhance(btn) {
        if (enhancedButtons.has(btn)) return; // already enhanced this page load
        var auth = getAuth();

        // Normally we only recognize the pristine "Account" button by its
        // label. But if a previous save ever baked a logged-in admin's name
        // into the saved HTML (an old bug), the button won't say "Account"
        // anymore -- so we also recognize it by the .account-dropdown
        // wrapper, which only this script ever creates around this exact
        // button. This lets already-corrupted saved content self-heal on
        // the next page load instead of needing a manual database fix.
        var alreadyWrapped = !!btn.closest('.account-dropdown');
        var isPristineAccountBtn = btn.textContent.trim().toLowerCase() === 'account';
        var looksAlreadyEnhanced = btn.innerHTML.indexOf('fa-circle-user') !== -1;
        if (!alreadyWrapped && !isPristineAccountBtn && !looksAlreadyEnhanced) return;

        enhancedButtons.add(btn);

        // Clean up any stale marker left over in previously-saved CMS content.
        btn.removeAttribute('data-acct-enhanced');

        var wrap = btn.closest('.account-dropdown');
        if (!wrap) {
            wrap = document.createElement('div');
            wrap.className = 'account-dropdown';
            btn.parentNode.insertBefore(wrap, btn);
            wrap.appendChild(btn);
        }

        // Stop the button from navigating anywhere itself -- it only opens the menu.
        btn.removeAttribute('href');
        btn.setAttribute('role', 'button');

        // Logged in: show the user's name on the button itself, so it's
        // obvious at a glance that they're signed in -- and the menu only
        // offers Log Out. Logged out: keep the original Log In / Sign Up menu.
        if (auth.isAuthenticated) {
            btn.innerHTML = '<i class="fas fa-circle-user" style="margin-right:6px;"></i>' + (auth.userName || 'My Account');
        } else {
            // Always reset to plain "Account" when logged out -- otherwise a
            // stale name that was ever baked into the saved page content
            // (an old bug) would keep showing forever for every visitor.
            btn.innerHTML = 'Account';
        }

        // The menu is appended directly to <body> (not inside the announcement
        // bar / navbar) and positioned with `fixed` coordinates computed from
        // the button's on-screen location. This guarantees it can never get
        // clipped or rendered behind the white navbar by any ancestor's
        // overflow/stacking context -- which is what was happening before.
        var menu = document.createElement('div');
        menu.className = 'account-dropdown-menu';
        if (auth.isAuthenticated) {
            menu.innerHTML =
                '<a href="#" id="navLogoutLink"><i class="fas fa-right-from-bracket" style="margin-right:8px;"></i>Log Out</a>';
        } else {
            menu.innerHTML =
                '<a href="/Account/Login"><i class="fas fa-right-to-bracket" style="margin-right:8px;"></i>Log In</a>';
        }
        menu.style.position = 'fixed';
        menu.style.display = 'none';
        menu.style.zIndex = '2147483647';
        menu.style.background = '#fff';
        menu.style.minWidth = '150px';
        menu.style.borderRadius = '8px';
        menu.style.boxShadow = '0 10px 28px rgba(0,0,0,0.2)';
        menu.style.overflow = 'hidden';
        document.body.appendChild(menu);

        // Log Out has to be a POST (that's how the server action is set up),
        // so build a tiny form and submit it instead of just following a link.
        if (auth.isAuthenticated) {
            var logoutLink = menu.querySelector('#navLogoutLink');
            logoutLink.addEventListener('click', function (e) {
                e.preventDefault();
                var form = document.createElement('form');
                form.method = 'post';
                form.action = '/Account/Logout';
                document.body.appendChild(form);
                form.submit();
            });
        }

        function positionMenu() {
            var r = btn.getBoundingClientRect();
            menu.style.top = (r.bottom + 8) + 'px';
            menu.style.left = 'auto';
            menu.style.right = (window.innerWidth - r.right) + 'px';
        }

        var closeTimer;
        function openMenu() {
            clearTimeout(closeTimer);
            positionMenu();
            menu.style.display = 'block';
            wrap.classList.add('open');
        }
        function closeMenu() {
            closeTimer = setTimeout(function () {
                menu.style.display = 'none';
                wrap.classList.remove('open');
            }, 150);
        }

        // Hover for mouse users.
        wrap.addEventListener('mouseenter', openMenu);
        wrap.addEventListener('mouseleave', closeMenu);
        menu.addEventListener('mouseenter', function () { clearTimeout(closeTimer); });
        menu.addEventListener('mouseleave', closeMenu);

        // Click/tap for touch users (toggles so it also works on mobile).
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            if (menu.style.display === 'block') {
                menu.style.display = 'none';
                wrap.classList.remove('open');
            } else {
                openMenu();
            }
        });

        document.addEventListener('click', function (e) {
            if (!wrap.contains(e.target) && !menu.contains(e.target)) {
                menu.style.display = 'none';
                wrap.classList.remove('open');
            }
        });

        // Keep the menu aligned under the button if the page scrolls/resizes
        // while it's open.
        window.addEventListener('scroll', function () {
            if (menu.style.display === 'block') positionMenu();
        }, true);
        window.addEventListener('resize', function () {
            if (menu.style.display === 'block') positionMenu();
        });
    }

    function run() {
        document.querySelectorAll('.apply-btn').forEach(enhance);
        if (auth.isAuthenticated) {
            document.documentElement.classList.add('nau-logged-in');
            if (document.body) document.body.classList.add('nau-logged-in');
            document.querySelectorAll('a[href*="/Account/Register"], a[href="/Account/Register"]').forEach(function (el) {
                if (el.classList.contains('nav-link')) {
                    el.setAttribute('href', '/CourseCatalog');
                    return;
                }
                if (el.closest('.footer-col') && el.textContent.trim().toLowerCase() === 'admissions') {
                    el.setAttribute('href', '/CourseCatalog');
                    return;
                }
                el.style.display = 'none';
            });
            document.querySelectorAll('a, button, span.feat-btn, div.util-info-row').forEach(function (el) {
                var text = el.textContent.trim().toLowerCase();
                if (text === 'apply online' || text === 'apply now' || text === 'apply online now →' || text === 'start application' || text === 'apply online now') {
                    var target = el.closest('.dd-featured') || el.closest('.util-info-row') || el;
                    target.style.display = 'none';
                }
            });
        }
    }

    run();

    // The homepage body comes from CMS-saved HTML; re-run if it changes
    // after this script's first pass (e.g. late-inserted content).
    var observer = new MutationObserver(run);
    observer.observe(document.body, { childList: true, subtree: true });
})();

/* ===== Wire up the "View All Faculties" button (Academics dropdown) =====
   The homepage body is saved HTML from the database, so instead of editing
   that content directly, we find the button by its text and point it at
   the new public Faculties page here. Same idea as the Account dropdown
   above -- runs once on load and again if the page content changes late. */
(function wireViewAllFacultiesButton() {
    var targetHref = '/Faculties/Public';
    var wired = new WeakSet();

    function looksLikeViewAllFaculties(el) {
        var text = (el.textContent || '').trim().toLowerCase();
        return text === 'view all faculties' || text === 'view all faculty' || text === 'all faculties' || text === 'view all programs';
    }

    function run() {
        document.querySelectorAll('a, button').forEach(function (el) {
            if (wired.has(el)) return;
            if (!looksLikeViewAllFaculties(el)) return;
            wired.add(el);

            if (el.tagName === 'A') {
                el.setAttribute('href', targetHref);
            } else {
                el.addEventListener('click', function () {
                    window.location.href = targetHref;
                });
            }
        });
    }

    run();

    var observer = new MutationObserver(run);
    observer.observe(document.body, { childList: true, subtree: true });
})();


const newsData = [
    { title: "14TH ESP (EXPERIENCE SHARING PLATFORM)", date: "April 17, 2025", desc: "14TH ESP (EXPERIENCE SHARING PLATFORM)", icon: "fa-handshake" },
    { title: "12th CPD (Continuing Professional Development)", date: "February 20, 2025", desc: "12TH CPD (Continuing Professional Development) HUMAN FACTORS IN AVIATION SAFETY ENGINEERING", icon: "fa-shield" },
    { title: "11th CPD: CONFLICT RESOLUTION AND NEGOTIATION", date: "November 14, 2024", desc: "11th CPD (Continuing Professional Development) CONFLICT RESOLUTION AND NEGOTIATION", icon: "fa-tools" }
];
const eventsData = [
    { day: "08", month: "JUN", title: "Aviation Safety Seminar", location: "AU Auditorium", time: "10:00 AM – 01:00 PM" },
    { day: "15", month: "JUN", title: "Aircraft Maintenance Workshop", location: "Hangar Training Facility", time: "09:00 AM – 03:00 PM" },
    { day: "22", month: "JUN", title: "Career Day & Industry Interaction", location: "AU Campus", time: "11:00 AM – 02:00 PM" }
];
const newsEl = document.getElementById("news");
if (newsEl) {
    newsEl.innerHTML = newsData.map(n => `
  <div class="news-item">
    <div class="news-img-placeholder"><i class="fas ${n.icon}"></i></div>
    <div class="news-body">
      <h4>${n.title}</h4>
      <div class="news-meta"><i class="far fa-calendar"></i> ${n.date}</div>
      <p>${n.desc}</p>
    </div>
  </div>`).join("");
}
const eventsEl = document.getElementById("events");
if (eventsEl) {
    eventsEl.innerHTML = eventsData.map(e => `
  <div class="event-item">
    <div class="event-date"><div class="month">${e.month}</div><div class="day">${e.day}</div></div>
    <div class="event-body">
      <h4>${e.title}</h4>
      <div class="event-detail"><i class="fas fa-map-marker-alt"></i> ${e.location}</div>
      <div class="event-detail"><i class="far fa-clock"></i> ${e.time}</div>
    </div>
  </div>`).join("");
}

/* ===== COUNTER ANIMATION ===== */
function animateCounters() {
    document.querySelectorAll('.fact-number').forEach(el => {
        const target = parseInt(el.textContent);
        if (isNaN(target)) return;
        const suffix = el.textContent.replace(/[0-9]/g, '');
        let count = 0;
        const step = Math.ceil(target / 50);
        const timer = setInterval(() => { count = Math.min(count + step, target); el.textContent = count + suffix; if (count >= target) clearInterval(timer); }, 30);
    });
}
const factsObserver = new IntersectionObserver((entries) => { entries.forEach(e => { if (e.isIntersecting) { animateCounters(); factsObserver.disconnect(); } }); }, { threshold: 0.3 });
const factsSection = document.querySelector('.facts2');
if (factsSection) factsObserver.observe(factsSection);

/* ===== ACADEMIC CALENDAR ===== */

const calendarData = {
    resultDates: [
        { label: "ATC Refresher (Officers) No.29 — Result Announced", date: "12.01.2026", type: "result" },
        { label: "ATC Refresher (Officers) No.30 — Result Announced", date: "06.04.2026", type: "result" },
        { label: "Audit Technique Course No.04 — Result Announced", date: "13.01.2026", type: "result" },
        { label: "Aeronautical Study Safety Assessment No.03 (Jan) — Result Announced", date: "20.01.2026", type: "result" },
        { label: "Aviation Economics Course No.03 — Result Announced", date: "27.01.2026", type: "result" },
        { label: "INDRA MSSR O&M No.05 — Result Announced", date: "03.02.2026", type: "result" },
        { label: "Supply & Demand in Air Transport Industry No.02 — Result Announced", date: "17.02.2026", type: "result" },
        { label: "Basic ATSEP Refresher Officers No.06 — Result Announced", date: "03.03.2026", type: "result" },
        { label: "Approach Control Surveillance No.06 — Result Announced", date: "20.03.2026", type: "result" },
        { label: "Air Passenger Rights No.01 — Result Announced", date: "09.04.2026", type: "result" },
        { label: "ATC Refresher (Officers) No.31 — Result Announced", date: "04.06.2026", type: "result" },
        { label: "Area/Approach Control Surveillance No.60 — Mid-Term Result", date: "01.06.2026", type: "midterm" },
        { label: "Aerodrome Certification Course No.02 — Result Announced", date: "16.06.2026", type: "result" },
        { label: "Flight Procedure Design (Conventional) No.03 — Result Announced", date: "07.07.2026", type: "result" },
        { label: "Area/Approach Control Surveillance No.60 — Final Result Announced", date: "10.07.2026", type: "result" },
        { label: "ATC Refresher (Officers) No.32 — Result Announced", date: "14.07.2026", type: "result" },
        { label: "Airport Economics Course No.01 — Result Announced", date: "21.07.2026", type: "result" },
        { label: "ANS Inspector Course No.01 — Result Announced", date: "04.08.2026", type: "result" },
        { label: "ELP Course No.05 — Mid-Term Result", date: "24.08.2026", type: "midterm" },
        { label: "ATC Refresher (Officers) No.33 — Result Announced", date: "11.08.2026", type: "result" },
        { label: "Aeronautical Mobile Service (Staff) No.05 — Result Announced", date: "08.09.2026", type: "result" },
        { label: "ATC Refresher (Officers) No.34 — Result Announced", date: "08.09.2026", type: "result" },
        { label: "Aircraft Accident & Investigation Awareness No.01 — Result Announced", date: "08.09.2026", type: "result" },
        { label: "ATSEP Radar Technicians No.03 — Mid-Term Result", date: "14.09.2026", type: "midterm" },
        { label: "Facilitation (ICAO Annex-9) No.01 — Result Announced", date: "22.09.2026", type: "result" },
        { label: "Flight Procedure Design (PBN) No.01 — Result Announced", date: "03.11.2026", type: "result" },
        { label: "ATSEP Radar Technicians No.03 — Final Result Announced", date: "06.10.2026", type: "result" },
        { label: "Aviation Climate Change Course No.01 — Result Announced", date: "13.10.2026", type: "result" },
        { label: "Disabled Aircraft Recovery Course No.01 — Result Announced", date: "27.10.2026", type: "result" },
        { label: "ATC Refresher (Officers) No.35 — Result Announced", date: "22.09.2026", type: "result" },
        { label: "Area/Approach Control Surveillance No.61 — Final Result Announced", date: "20.10.2026", type: "result" },
        { label: "ATC Refresher (Officers) No.36 — Result Announced", date: "13.10.2026", type: "result" },
        { label: "Annex 14 Application and Requirements No.03 — Result Announced", date: "24.11.2026", type: "result" },
        { label: "ATC Refresher (Officers) No.37 — Result Announced", date: "24.11.2026", type: "result" },
        { label: "SAR Coordinator Course No.02 — Result Announced", date: "15.12.2026", type: "result" },
        { label: "ATC Refresher (Officers) No.38 — Result Announced", date: "08.12.2026", type: "result" },
        { label: "ATC Refresher (Officers) No.39 — Result Announced", date: "22.12.2026", type: "result" },
        { label: "Understanding ICAO Annex-19 Safety Mgmt No.03 — Result Announced", date: "15.12.2026", type: "result" },
        { label: "Basic ATC Course No.46 — Mid-Term Result (Estimated)", date: "08.03.2027", type: "midterm" }
    ],
    schools: [
        {
            name: "School of Air Navigation Services (ANS)",
            icon: "fa-tower-cell",
            color: "#0b2c4d",
            courses: [
                // ATS Block
                { title: "Basic ATC Course No. 46", duration: "40 Weeks", start: "07.12.2026", end: "03.09.2027" },
                { title: "ATC Refresher (Abridged) For Officers — No.29", duration: "01 Week", start: "05.01.2026", end: "09.01.2026" },
                { title: "ATC Refresher (Abridged) For Officers — No.30", duration: "01 Week", start: "30.03.2026", end: "03.04.2026" },
                { title: "ATC Refresher (Abridged) For Officers — No.31", duration: "01 Week", start: "25.05.2026", end: "29.05.2026" },
                { title: "ATC Refresher (Abridged) For Officers — No.32", duration: "01 Week", start: "06.07.2026", end: "10.07.2026" },
                { title: "ATC Refresher (Abridged) For Officers — No.33", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                { title: "ATC Refresher (Abridged) For Officers — No.34", duration: "01 Week", start: "31.08.2026", end: "04.09.2026" },
                { title: "ATC Refresher (Abridged) For Officers — No.35", duration: "01 Week", start: "14.09.2026", end: "19.09.2026" },
                { title: "ATC Refresher (Abridged) For Officers — No.36", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                { title: "ATC Refresher (Abridged) For Officers — No.37", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                { title: "ATC Refresher (Abridged) For Officers — No.38", duration: "01 Week", start: "30.11.2026", end: "04.12.2026" },
                { title: "ATC Refresher (Abridged) For Officers — No.39", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                { title: "ATC Refresher (Abridged) For Officers — No.40", duration: "01 Week", start: "28.12.2026", end: "01.01.2027" },
                { title: "ATC Assistant Course No. 03", duration: "08 Weeks", start: "07.09.2026", end: "30.10.2026" },
                { title: "ATC Refresher (Abridged) For Staff — No.09", duration: "01 Week", start: "26.01.2026", end: "30.01.2026" },
                { title: "ATC Refresher (Abridged) For Staff — No.10", duration: "01 Week", start: "18.05.2026", end: "22.05.2026" },
                { title: "ATC Refresher (Abridged) For Staff — No.11", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                { title: "ATC Refresher (Abridged) For Staff — No.12", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" },
                { title: "Approach Control Surveillance Course No. 06", duration: "05 Weeks", start: "09.02.2026", end: "13.03.2026" },
                { title: "Area / Approach Control Surveillance Course No. 60", duration: "12 Weeks", start: "13.04.2026", end: "03.07.2026" },
                { title: "Area / Approach Control Surveillance Course No. 61", duration: "12 Weeks", start: "27.07.2026", end: "16.10.2026" },
                { title: "Flight Procedure Design (Conventional) Course No. 03", duration: "04 Weeks", start: "08.06.2026", end: "03.07.2026" },
                { title: "Flight Procedure Design (PBN) Course No. 01", duration: "03 Weeks", start: "12.10.2026", end: "30.10.2026" },
                { title: "ELP (English Language Proficiency) Advance Course No. 02", duration: "02 Weeks", start: "06.04.2026", end: "17.04.2026" },
                { title: "ELP (English Language Proficiency) Course No. 05", duration: "03 Weeks", start: "10.08.2026", end: "04.09.2026" },
                { title: "Basic SAR Course No. 22", duration: "02 Weeks", start: "02.03.2026", end: "13.03.2026" },
                { title: "Basic SAR Course No. 23", duration: "02 Weeks", start: "31.08.2026", end: "11.09.2026" },
                { title: "Basic SAR Course No. 24", duration: "02 Weeks", start: "16.11.2026", end: "27.11.2026" },
                { title: "SAR Coordinator Course No. 02", duration: "02 Weeks", start: "30.11.2026", end: "11.12.2026" },
                { title: "On Job Training (OJT) Course No. 01", duration: "01 Week", start: "12.01.2026", end: "16.01.2026" },
                { title: "On Job Training (OJT) Course No. 02", duration: "01 Week", start: "11.05.2026", end: "15.05.2026" },
                { title: "On Job Training (OJT) Course No. 03", duration: "01 Week", start: "30.11.2026", end: "04.12.2026" },
                { title: "Managing Aeronautical Information Course No. 01", duration: "01 Week", start: "24.08.2026", end: "28.08.2026" },
                { title: "Managing Aeronautical Information Course No. 02", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                // ATS & Comm. Ops Block
                { title: "CAVRN (For Officers) Course No. 03", duration: "01 Week", start: "19.01.2026", end: "23.01.2026" },
                { title: "CAVRN (For Officers) Course No. 04", duration: "01 Week", start: "01.06.2026", end: "05.06.2026" },
                { title: "SWIM Course No. 02", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                { title: "SWIM Course No. 03", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                { title: "Aeronautical Radio Freq. Spectrum Mgmt (Officers) No. 04", duration: "01 Week", start: "09.02.2026", end: "13.02.2026" },
                { title: "Aeronautical Radio Freq. Spectrum Mgmt (Officers) No. 05", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                { title: "COO Course No. 09", duration: "14 Weeks", start: "TBD", end: "TBD" },
                { title: "Communication Operation Assistant Course No. 21", duration: "10 Weeks", start: "TBD", end: "TBD" },
                { title: "IDS AMHS Advance (For Officers) Course No. 04", duration: "02 Weeks", start: "27.07.2026", end: "07.08.2026" },
                { title: "IDS AirNav AMHS (CRONOS) DB (Staff) Course No. 08", duration: "02 Weeks", start: "05.10.2026", end: "16.10.2026" },
                { title: "IDS AirNav AMHS (CRONOS) DB (Staff) Course No. 09", duration: "02 Weeks", start: "07.12.2026", end: "18.12.2026" },
                { title: "SDCPS Software (IOU) (Staff) Course No. 10", duration: "01 Week", start: "06.07.2026", end: "10.07.2026" },
                { title: "SDCPS Software (IOU) (Staff) Course No. 11", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                { title: "Aeronautical Mobile Service (Officers) Course No. 04", duration: "04 Weeks", start: "27.04.2026", end: "22.05.2026" },
                { title: "Aeronautical Mobile Service (Staff) Course No. 05", duration: "03 Weeks", start: "17.08.2026", end: "04.09.2026" },
                // CNS Block
                { title: "INDRA MSSR Operation & Maintenance Course No. 05", duration: "04 Weeks", start: "05.01.2026", end: "30.01.2026" },
                { title: "ADSB Operation & Maintenance Course No. 04", duration: "01 Week", start: "02.03.2026", end: "06.03.2026" },
                { title: "Selex (ILS) Operation & Maintenance Course No. 03", duration: "04 Weeks", start: "30.03.2026", end: "24.04.2026" },
                { title: "INDRA PSR Operation & Maintenance Course No. 03", duration: "04 Weeks", start: "06.04.2026", end: "30.04.2026" },
                { title: "ATM (Air Traffic Management) System Course No. 03", duration: "02 Weeks", start: "04.05.2026", end: "15.05.2026" },
                { title: "ELDS MSSR Operation & Maintenance Course No. 04", duration: "04 Weeks", start: "20.07.2026", end: "14.08.2026" },
                { title: "Selex DVOR / DME Operation & Maintenance Course No. 04", duration: "04 Weeks", start: "23.11.2026", end: "18.12.2026" },
                { title: "Basic ATSEP Refresher (For Officers) Course No. 06", duration: "01 Week", start: "23.02.2026", end: "27.02.2026" },
                { title: "ATSEP Data Processing & Networking (Technicians) No. 03", duration: "08 Weeks", start: "08.06.2026", end: "31.07.2026" },
                { title: "ATSEP (Radar) For Technicians Course No. 03", duration: "07 Weeks", start: "17.08.2026", end: "02.10.2026" },
                { title: "ATSEP (NAVAIDS) For Technicians Course No. 02", duration: "06 Weeks", start: "14.09.2026", end: "23.10.2026" },
                { title: "ATSEP (Radio Communication) For Technicians Course No. 03", duration: "06 Weeks", start: "05.10.2026", end: "13.11.2026" },
                { title: "INDRA VCCS SDC-2000 O&M Course No. 01", duration: "01 Week", start: "TBD", end: "TBD" },
                { title: "HARRIS LIBERTY STAR VCCS O&M Course No. 01", duration: "01 Week", start: "TBD", end: "TBD" }
            ]
        },
        {
            name: "School of Regulatory Services",
            icon: "fa-scale-balanced",
            color: "#1e5f74",
            courses: [
                { title: "International Air Law Course No. 01", duration: "01 Week", start: "04.05.2026", end: "08.05.2026" },
                { title: "Managing Compliance with ICAO SARPs Course No. 02", duration: "01 Week", start: "18.05.2026", end: "22.05.2026" },
                { title: "Air Cargo Regulatory Framework & Compliance Course No. 02", duration: "01 Week", start: "01.06.2026", end: "05.06.2026" },
                { title: "Annex 14 Application and Requirements Course No. 03", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                { title: "Aerodrome Certification Course No. 02", duration: "02 Weeks", start: "01.06.2026", end: "12.06.2026" },
                { title: "Aerodrome Manual Course No. 02", duration: "01 Week", start: "29.06.2026", end: "03.07.2026" },
                { title: "Aviation Economics Course No. 03", duration: "01 Week", start: "19.01.2026", end: "23.01.2026" },
                { title: "Supply & Demand in Air Transport Industry Course No. 02", duration: "01 Week", start: "08.02.2026", end: "13.02.2026" },
                { title: "Airport Economics Course No. 01", duration: "02 Weeks", start: "06.07.2026", end: "17.07.2026" },
                { title: "Airport Marketing Course No. 03", duration: "01 Week", start: "17.08.2026", end: "21.08.2026" },
                { title: "Audit Technique Course No. 04", duration: "01 Week", start: "05.01.2026", end: "09.01.2026" },
                { title: "Audit Technique Course No. 05", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" },
                { title: "Aeronautical Study Safety Assessment Course No. 03 (Jan)", duration: "01 Week", start: "12.01.2026", end: "16.01.2026" },
                { title: "Aeronautical Study Safety Assessment Course No. 03 (Sep)", duration: "01 Week", start: "07.09.2026", end: "11.09.2026" },
                { title: "Understanding ICAO Annex-19 Safety Management Course No. 02", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                { title: "Understanding ICAO Annex-19 Safety Management Course No. 03", duration: "01 Week", start: "07.12.2026", end: "11.12.2026" },
                { title: "Fundamentals of Air Cargo Operations & Safety (Staff) No. 01", duration: "01 Week", start: "13.07.2026", end: "17.07.2026" },
                { title: "ANS Inspector Course No. 01", duration: "02 Weeks", start: "20.07.2026", end: "31.07.2026" },
                { title: "Rules of the Air for Aviation Safety Professionals (ICAO Annex 02) No. 01", duration: "02 Weeks", start: "03.08.2026", end: "07.08.2026" },
                { title: "Aviation Climate Change Course No. 01", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                { title: "Air Passenger Rights Course No. 01", duration: "01 Week", start: "30.03.2026", end: "04.04.2026" },
                { title: "Aircraft Accident and Investigation Awareness (ICAO Annex-13) No. 01", duration: "01 Week", start: "31.08.2026", end: "04.09.2026" },
                { title: "Facilitation (ICAO Annex-9) Course No. 01", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                { title: "Disabled Aircraft Recovery Course No. 01", duration: "01 Week", start: "19.10.2026", end: "23.10.2026" }
            ]
        },
        {
            name: "School of Airport Services",
            icon: "fa-fire-extinguisher",
            color: "#c0392b",
            courses: [
                { title: "Senior Aerodrome Fire Fighter Course No. 57", duration: "05 Weeks", start: "05.01.2026", end: "06.02.2026" },
                { title: "Senior Aerodrome Fire Fighter Course No. 58", duration: "05 Weeks", start: "17.08.2026", end: "18.09.2026" },
                { title: "Senior Aerodrome Fire Fighter Course No. 59", duration: "05 Weeks", start: "26.10.2026", end: "27.11.2026" },
                { title: "Aerodrome Fire Fighter Refresher Course No. 12", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                { title: "Aerodrome Fire Fighter Refresher Course No. 13", duration: "01 Week", start: "21.09.2026", end: "25.09.2026" },
                { title: "Aerodrome Fire Fighter Refresher Course No. 14", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                { title: "SVD (Refresher) Course No. 10", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                { title: "SVD (Refresher) Course No. 11", duration: "01 Week", start: "21.09.2026", end: "25.09.2026" },
                { title: "SVD (Refresher) Course No. 12", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                { title: "Fire Fighting (Corporate Sector) Course No. 01", duration: "01 Week", start: "26.01.2026", end: "30.01.2026" },
                { title: "Fire Fighting (Corporate Sector) Course No. 02", duration: "01 Week", start: "15.06.2026", end: "19.06.2026" },
                { title: "Fire Fighting (Corporate Sector) Course No. 03", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                { title: "Fire Fighting (Corporate Sector) Course No. 04", duration: "01 Week", start: "31.08.2026", end: "04.09.2026" },
                { title: "Fire Fighting (Corporate Sector) Course No. 05", duration: "01 Week", start: "07.09.2026", end: "11.09.2026" },
                { title: "Fire Fighting (Corporate Sector) Course No. 06", duration: "01 Week", start: "19.10.2026", end: "23.10.2026" },
                { title: "Fire Prevention Course No. 69", duration: "03 Weeks", start: "23.02.2026", end: "13.03.2026" },
                { title: "Fire Prevention Course No. 70", duration: "03 Weeks", start: "01.06.2026", end: "19.06.2026" },
                { title: "Fire Prevention Course No. 71", duration: "03 Weeks", start: "29.06.2026", end: "17.07.2026" },
                { title: "Breathing Apparatus Course No. 24", duration: "03 Weeks", start: "30.03.2026", end: "17.04.2026" },
                { title: "Breathing Apparatus Course No. 25", duration: "03 Weeks", start: "27.04.2026", end: "15.05.2026" },
                { title: "Breathing Apparatus Course No. 26", duration: "03 Weeks", start: "20.07.2026", end: "07.08.2026" },
                { title: "Junior Fire Officer Course No. 15", duration: "05 Weeks", start: "28.09.2026", end: "30.10.2026" },
                { title: "Fire Investigation (For Officers) Course No. 01", duration: "02 Weeks", start: "30.11.2026", end: "11.12.2026" },
                { title: "Aircraft Familiarization (For Officers) Course No. 01", duration: "01 Week", start: "28.12.2026", end: "01.01.2027" },
                { title: "Fire Officer (Refresher) Course No. 14", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                { title: "Aerodrome Operations (For Officers) Course No. 06", duration: "04 Weeks", start: "19.01.2026", end: "13.02.2026" },
                { title: "Facilitation & Hospitality Management (Officers) Course No. 03", duration: "02 Weeks", start: "14.09.2026", end: "25.09.2026" },
                { title: "Facilitation & Hospitality Management (Staff) Course No. 07", duration: "02 Weeks", start: "10.03.2026", end: "30.03.2026" },
                { title: "Facilitation & Hospitality Management (Officers) Course No. 08", duration: "02 Weeks", start: "02.11.2026", end: "13.11.2026" },
                { title: "Airside Safety Awareness Course No. 07 (JIAP)", duration: "01 Week", start: "05.01.2026", end: "09.01.2026" },
                { title: "Airside Safety Awareness Course No. 08 (IIAP)", duration: "01 Week", start: "19.01.2026", end: "23.01.2026" },
                { title: "Airside Safety Awareness Course No. 09 (QIAP)", duration: "01 Week", start: "06.07.2026", end: "10.07.2026" },
                { title: "Airside Safety Awareness Course No. 10 (BKIAP)", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                { title: "Airside Safety Awareness Course No. 11 (FIAP)", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                { title: "Airside Safety Awareness Course No. 12 (AIAP)", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                { title: "Airside Safety Awareness Course No. 13 (MIAP)", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                { title: "Marshaller Refresher Course No. 02", duration: "01 Week", start: "16.02.2026", end: "20.02.2026" },
                { title: "Marshaller Refresher Course No. 03", duration: "01 Week", start: "20.07.2026", end: "27.07.2026" },
                { title: "Standard & Recommended Practices Aerodromes (Annex-14) (Engineers) No. 03", duration: "02 Weeks", start: "05.01.2026", end: "16.01.2026" },
                { title: "Airfield Pavement Maintenance & Surface Evaluation (Engineers) No. 02", duration: "01 Week", start: "06.04.2026", end: "10.04.2026" },
                { title: "PBB Maintenance (STP) Course No. 08", duration: "02 Weeks", start: "02.02.2026", end: "13.02.2026" },
                { title: "PBB Maintenance (STP) Course No. 09", duration: "02 Weeks", start: "02.11.2026", end: "13.11.2026" },
                { title: "Underground Cable Fault Repairs (Technicians) Course No. 03", duration: "02 Weeks", start: "04.05.2026", end: "15.05.2026" },
                { title: "Engineering Estimation (Staff) Course No. 02", duration: "02 Weeks", start: "08.06.2026", end: "19.06.2026" },
                { title: "FIDIC Contract (Officers) Course No. 01", duration: "01 Week", start: "17.08.2026", end: "21.08.2026" }
            ]
        },
        {
            name: "School of Aviation Management",
            icon: "fa-chart-line",
            color: "#1e8e3e",
            courses: [
                { title: "Aviation Management Course No. 09", duration: "05 Weeks", start: "12.01.2026", end: "13.02.2026" },
                { title: "Aviation Management Course No. 10", duration: "05 Weeks", start: "19.10.2026", end: "20.11.2026" },
                { title: "Personal Effectiveness (Staff) Course No. 06", duration: "01 Week", start: "04.05.2026", end: "08.05.2026" },
                { title: "Personal Effectiveness (Staff) Course No. 07", duration: "01 Week", start: "07.09.2026", end: "11.09.2026" },
                { title: "Personal Effectiveness (Staff) Course No. 08", duration: "01 Week", start: "07.12.2026", end: "11.12.2026" },
                { title: "Basic Logistics (Staff) Course No. 05", duration: "03 Weeks", start: "19.01.2026", end: "06.02.2026" },
                { title: "Basic Logistics (Staff) Course No. 06", duration: "03 Weeks", start: "20.04.2026", end: "08.05.2026" },
                { title: "Basic Logistics (Staff) Course No. 07", duration: "03 Weeks", start: "13.07.2026", end: "31.07.2026" },
                { title: "Basic Logistics (Staff) Course No. 08", duration: "03 Weeks", start: "09.11.2026", end: "27.11.2026" },
                { title: "Logistics & Supply Chain Operations (Officers) No. 01", duration: "03 Weeks", start: "18.05.2026", end: "05.06.2026" },
                { title: "Logistics & Supply Chain Operations (Officers) No. 02", duration: "03 Weeks", start: "07.09.2026", end: "25.09.2026" },
                { title: "Human Factor in Aviation (Officers) Course No. 03", duration: "01 Week", start: "04.05.2026", end: "08.05.2026" },
                { title: "Human Factor in Aviation (Officers) Course No. 04", duration: "01 Week", start: "26.10.2026", end: "30.10.2026" },
                { title: "Introduction to Aviation IT System (Staff) Course No. 05", duration: "01 Week", start: "22.06.2026", end: "26.06.2026" },
                { title: "Introduction to Aviation IT System (Staff) Course No. 06", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" },
                { title: "IT/Cyber Security, Audit & Compliance Mgmt (Officers) No. 01", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                { title: "IT/Cyber Security, Audit & Compliance Mgmt (Officers) No. 02", duration: "01 Week", start: "07.12.2026", end: "11.12.2026" },
                { title: "MS Excel Basic (Staff) Course No. 01", duration: "01 Week", start: "13.04.2026", end: "17.04.2026" },
                { title: "MS Excel Basic (Staff) Course No. 02", duration: "01 Week", start: "19.10.2026", end: "23.10.2026" },
                { title: "Introduction to ERP (Staff) Course No. 01", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                { title: "Introduction to ERP (Staff) Course No. 02", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                { title: "E-Procurement through E-PADS (Officers) Course No. 03", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                { title: "E-Procurement through E-PADS (Officers) Course No. 04", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                { title: "Basic Office Organization (Asst/Supervisor) Course No. 21", duration: "02 Weeks", start: "05.01.2026", end: "16.01.2026" },
                { title: "Basic Office Organization (Asst/Supervisor) Course No. 22", duration: "02 Weeks", start: "31.08.2026", end: "11.09.2026" },
                { title: "Legal Proficiency (Officers) Course No. 01", duration: "03 Weeks", start: "26.01.2026", end: "13.02.2026" },
                { title: "Legal Proficiency (Staff) Course No. 02", duration: "02 Weeks", start: "17.08.2026", end: "28.08.2026" },
                { title: "Legal Proficiency (Staff) Course No. 03", duration: "02 Weeks", start: "12.10.2026", end: "23.10.2026" },
                { title: "ITC Refresher Course No. 02", duration: "03 Days", start: "04.02.2026", end: "06.02.2026" },
                { title: "ITC Course No. 33", duration: "02 Weeks", start: "06.04.2026", end: "17.04.2026" },
                { title: "ITC Course No. 34", duration: "02 Weeks", start: "12.10.2026", end: "23.10.2026" },
                { title: "ETC (Examination Technique Course) No. 14", duration: "01 Week", start: "08.06.2026", end: "12.06.2026" },
                { title: "ETC (Examination Technique Course) No. 15", duration: "01 Week", start: "17.08.2026", end: "21.08.2026" },
                { title: "Basic Mgmt System Standards (ISO/SMS) Officers Course No. 02 (SQMS)", duration: "01 Week", start: "06.04.2026", end: "10.04.2026" },
                { title: "Basic Mgmt System Standards (ISO/SMS) Officers Course No. 03 (SQMS)", duration: "01 Week", start: "30.11.2026", end: "04.12.2026" },
                { title: "Basic Mgmt System Standards (ISO/SMS) Staff Course No. 03 (SQMS)", duration: "01 Week", start: "13.04.2026", end: "17.04.2026" },
                { title: "Basic Mgmt System Standards (ISO/SMS) Staff Course No. 04 (SQMS)", duration: "01 Week", start: "26.10.2026", end: "30.10.2026" },
                { title: "Finance For Non-Financial Managers (Officers) Course No. 02", duration: "01 Week", start: "21.09.2026", end: "25.09.2026" },
                { title: "Finance For Non-Financial Managers (Officers) Course No. 03", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                { title: "PPRA Rules (Officers) Course No. 02", duration: "01 Week", start: "27.07.2026", end: "31.07.2026" },
                { title: "PPRA Rules (Officers) Course No. 03", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                { title: "Superintendent Development Course No. 05", duration: "01 Week", start: "09.02.2026", end: "13.02.2026" },
                { title: "Superintendent Development Course No. 06", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" }
            ]
        }
    ]
};

function openCalendar(e, view) {
    e.preventDefault();
    calActiveView = view || 'courses';
    if (!window._calMidnightRefreshScheduled) {
        window._calMidnightRefreshScheduled = true;
        scheduleMidnightRefresh(function () { renderCalendar(); renderResultDates(); });
    }
    if (calActiveView === 'results') {
        // Switch to result dates tab
        setTimeout(() => {
            const tabs = document.querySelectorAll('.cal-view-tab');
            tabs.forEach(t => t.classList.remove('active'));
            const resultTab = document.getElementById('calTabResults');
            if (resultTab) resultTab.classList.add('active');
            const coursesTab = document.getElementById('calTabCourses');
            if (coursesTab) coursesTab.classList.remove('active');
            const searchWrap = document.querySelector('.cal-search-wrap');
            const legendStrip = document.getElementById('calLegendStrip');
            if (searchWrap) searchWrap.style.display = 'none';
            if (legendStrip) legendStrip.style.display = 'none';
            const titleEl = document.getElementById('calMainSchoolTitle');
            if (titleEl) titleEl.textContent = 'Result Announcement Dates';
            renderResultDates();
        }, 50);
    } else {
        renderCalendar();
    }
    document.getElementById('calendarModal').classList.add('open');
    document.body.style.overflow = 'hidden';
}
function closeCalendar() {
    document.getElementById('calendarModal').classList.remove('open');
    document.body.style.overflow = '';
}

let activeSchool = 0;
let calSearchTerm = '';
let calStatusFilter = 'all';



function renderCalendar() {
    const school = calendarData.schools[activeSchool];
    const list = document.getElementById('calCourseList');
    const titleEl = document.getElementById('calMainSchoolTitle');
    const schoolsEl = document.getElementById('calSidebarSchools');

    // Render sidebar schools
    if (schoolsEl) {
        const allSchoolsLabel = `<div class="cal-sf-label-schools" style="margin-top:4px">Schools</div>`;
        schoolsEl.innerHTML = allSchoolsLabel + calendarData.schools.map((s, i) => `
      <button class="cal-sidebar-school-btn${i === activeSchool ? ' active' : ''}" onclick="switchCalSchool(${i})" style="">
        <span class="cal-sidebar-school-dot" style="background:${s.color}"></span>
        <span>${s.name.replace('School of ', '')}</span>
      </button>`).join('');
    }

    if (titleEl) titleEl.textContent = school.name.replace('School of ', '');

    // Parse course dates into events
    function parseDate(str) {
        if (!str || str === 'TBD') return null;
        const p = str.split('.');
        if (p.length !== 3) return null;
        return new Date(parseInt(p[2]), parseInt(p[1]) - 1, parseInt(p[0]));
    }

    // Filter courses by status filter
    const filteredCourses = school.courses.filter(c => {
        const matchSearch = !calSearchTerm || c.title.toLowerCase().includes(calSearchTerm.toLowerCase());
        const isTBD = c.start === 'TBD';
        const upcoming = !isTBD && isUpcoming(c);
        const completed = !isTBD && !upcoming;
        const matchFilter = calStatusFilter === 'all'
            || (calStatusFilter === 'upcoming' && upcoming)
            || (calStatusFilter === 'completed' && completed)
            || (calStatusFilter === 'tbd' && isTBD);
        return matchSearch && matchFilter;
    });

    // Build month calendars for all months that have courses (Jan–Dec 2026)
    const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    const DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    const year = 2026;

    // Map date string -> array of courses starting that day
    const startMap = {};
    const rangeMap = {}; // date -> array of active courses

    filteredCourses.forEach((c, ci) => {
        const sd = parseDate(c.start);
        const ed = parseDate(c.end);
        if (!sd) return;
        const key = `${sd.getFullYear()}-${sd.getMonth()}-${sd.getDate()}`;
        if (!startMap[key]) startMap[key] = [];
        startMap[key].push({ c, ci, color: school.color });
        // Mark range days
        if (ed && ed > sd) {
            const d = new Date(sd);
            d.setDate(d.getDate() + 1);
            while (d <= ed) {
                const rk = `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`;
                if (!rangeMap[rk]) rangeMap[rk] = [];
                rangeMap[rk].push({ c, ci, color: school.color });
                d.setDate(d.getDate() + 1);
            }
        }
    });

    // Find which months actually have courses
    const activeMonths = new Set();
    filteredCourses.forEach(c => {
        const sd = parseDate(c.start);
        if (sd && sd.getFullYear() === year) activeMonths.add(sd.getMonth());
        const ed = parseDate(c.end);
        if (ed && ed.getFullYear() === year) activeMonths.add(ed.getMonth());
    });

    if (filteredCourses.length === 0) {
        list.innerHTML = `<div class="cal-empty"><i class="fas fa-search"></i><p>No courses found${calSearchTerm ? ' matching "' + calSearchTerm + '"' : ''}</p></div>`;
        return;
    }

    const today = new Date();

    let html = '<div class="cal-grid-wrap">';

    for (let m = 0; m < 12; m++) {
        if (!activeMonths.has(m)) continue;
        const firstDay = new Date(year, m, 1);
        const lastDay = new Date(year, m + 1, 0);
        const startDow = firstDay.getDay();
        const totalDays = lastDay.getDate();

        // Count courses starting this month
        let monthCount = 0;
        for (let d = 1; d <= totalDays; d++) {
            const k = `${year}-${m}-${d}`;
            if (startMap[k]) monthCount += startMap[k].length;
        }

        html += `<div class="cal-month-block">
      <div class="cal-month-header">
        <div class="cal-month-name">${MONTHS[m]} <span>${year}</span></div>
        <div class="cal-month-count">${monthCount} course${monthCount !== 1 ? 's' : ''} start</div>
      </div>
      <div class="cal-week-days">${DAYS.map(d => `<div class="cal-wd">${d}</div>`).join('')}</div>
      <div class="cal-days-grid">`;

        // Empty cells before first day
        for (let i = 0; i < startDow; i++) html += `<div class="cal-day empty"></div>`;

        for (let d = 1; d <= totalDays; d++) {
            const k = `${year}-${m}-${d}`;
            const starts = startMap[k] || [];
            const inRange = rangeMap[k] || [];
            const isToday = today.getFullYear() === year && today.getMonth() === m && today.getDate() === d;
            const hasCourse = starts.length > 0;
            const inRangeOnly = !hasCourse && inRange.length > 0;

            let cls = 'cal-day';
            if (isToday) cls += ' today';
            if (hasCourse) cls += ' has-course';
            if (inRangeOnly) cls += ' in-range';

            let dotsHtml = '';
            if (starts.length > 0) {
                dotsHtml = `<div class="cal-day-dots">${starts.slice(0, 3).map(ev => `<span class="cal-day-dot" style="background:${school.color}"></span>`).join('')}${starts.length > 3 ? `<span class="cal-day-dot-more">+${starts.length - 3}</span>` : ''}</div>`;
            } else if (inRange.length > 0) {
                dotsHtml = `<div class="cal-day-dots"><span class="cal-day-dot range-dot" style="background:${school.color}44"></span></div>`;
            }

            // Tooltip data
            const tooltipCourses = starts.length > 0 ? starts : inRange;
            const tooltipAttr = tooltipCourses.length > 0
                ? `data-tooltip="${tooltipCourses.slice(0, 3).map(ev => ev.c.title.substring(0, 45) + '...').join('|')}"`
                : '';

            html += `<div class="${cls}" ${tooltipAttr} onclick="showDayCourses(${year},${m},${d},'${k}')" title="${starts.map(ev => ev.c.title).join('\n')}">
        <span class="cal-day-num">${d}</span>
        ${dotsHtml}
      </div>`;
        }

        // Fill remaining cells
        const totalCells = startDow + totalDays;
        const remainder = totalCells % 7;
        if (remainder > 0) {
            for (let i = remainder; i < 7; i++) html += `<div class="cal-day empty"></div>`;
        }

        html += `</div></div>`;
    }

    // Floating course list (shows on day click)
    html += `<div id="calDayPopup" class="cal-day-popup" style="display:none">
    <div class="cal-day-popup-header">
      <span id="calDayPopupTitle"></span>
      <button onclick="document.getElementById('calDayPopup').style.display='none'"><i class="fas fa-times"></i></button>
    </div>
    <div id="calDayPopupList"></div>
  </div>`;

    html += '</div>';
    list.innerHTML = html;

    // Store filtered courses for day lookup
    window._calFilteredCourses = filteredCourses;
    window._calSchoolColor = school.color;
    window._calStartMap = startMap;
    window._calRangeMap = rangeMap;
}

function setCalFilter(filter, btn) {
    calStatusFilter = filter;
    document.querySelectorAll('.cal-sf-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    renderCalendar();
}

function parseDMY(str) {
    if (!str || str === 'TBD') return null;
    const parts = str.split('.');
    if (parts.length !== 3) return null;
    const d = new Date(parts[2], parts[1] - 1, parts[0]);
    return isNaN(d) ? null : d;
}
function isUpcoming(course) {
    // Accept either a course object {start,end} or a raw date string (back-compat).
    const c = (typeof course === 'string') ? { start: course, end: null } : course;
    if (!c || !c.start || c.start === 'TBD') return false;
    // "Upcoming" = not finished yet. Uses the END date (falling back to start)
    // so a course currently in progress isn't mistakenly shown as Completed.
    const endDate = parseDMY(c.end) || parseDMY(c.start);
    if (!endDate) return false;
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return endDate >= today;
}
// Re-render the given function every day at 00:00:05, so status badges
// (Upcoming/Completed) stay correct even if a page/modal is left open
// across a day change, without needing a manual refresh.
function scheduleMidnightRefresh(renderFn) {
    const now = new Date();
    const nextMidnight = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1, 0, 0, 5);
    setTimeout(function tick() {
        try { renderFn(); } catch (e) { }
        setInterval(function () { try { renderFn(); } catch (e) { } }, 24 * 60 * 60 * 1000);
    }, nextMidnight - now);
}

let calActiveView = 'courses'; // 'courses' or 'results'

function switchCalView(view, btn) {
    calActiveView = view;
    document.querySelectorAll('.cal-view-tab').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    const searchWrap = document.querySelector('.cal-search-wrap');
    const legendStrip = document.getElementById('calLegendStrip');
    const sidebarSchools = document.getElementById('calSidebarSchools');
    const titleEl = document.getElementById('calMainSchoolTitle');
    const searchInput = document.getElementById('calSearchInput');
    if (view === 'results') {
        if (searchWrap) searchWrap.style.display = 'none';
        if (legendStrip) legendStrip.style.display = 'none';
        if (sidebarSchools) sidebarSchools.style.opacity = '0.4';
        if (titleEl) titleEl.textContent = 'Result Announcement Dates';
        if (searchInput) searchInput.value = '';
        renderResultDates();
    } else {
        if (searchWrap) searchWrap.style.display = '';
        if (legendStrip) legendStrip.style.display = '';
        if (sidebarSchools) sidebarSchools.style.opacity = '1';
        calSearchTerm = '';
        if (searchInput) searchInput.value = '';
        renderCalendar();
    }
}

function renderResultDates() {
    const list = document.getElementById('calCourseList');
    const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    const SHORT_MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

    function parseDate(str) {
        if (!str || str === 'TBD') return null;
        const p = str.split('.');
        if (p.length !== 3) return null;
        return new Date(parseInt(p[2]), parseInt(p[1]) - 1, parseInt(p[0]));
    }

    // Group result dates by month
    const byMonth = {};
    calendarData.resultDates.forEach(r => {
        const d = parseDate(r.date);
        if (!d) return;
        const m = d.getMonth();
        if (!byMonth[m]) byMonth[m] = [];
        byMonth[m].push({ ...r, dateObj: d });
    });

    const today = new Date();
    let html = '<div class="cal-results-wrap">';
    html += `<div style="background:#f0f7ff;border:1.5px solid #bfdbfe;border-radius:12px;padding:14px 18px;margin-bottom:20px;display:flex;align-items:center;gap:12px;font-size:13px;color:#1e40af;">
    <i class="fas fa-info-circle" style="font-size:16px;flex-shrink:0;"></i>
    <span>Results are officially published via the <strong>Student Online Portal</strong>. Dates below are estimated announcement dates based on course end dates (typically 1–2 weeks after course completion). Contact <strong>AdID.Academics@paa.gov.pk</strong> for confirmed dates.</span>
  </div>`;

    const sortedMonths = Object.keys(byMonth).map(Number).sort((a, b) => a - b);
    sortedMonths.forEach(m => {
        const items = byMonth[m].sort((a, b) => a.dateObj - b.dateObj);
        html += `<div class="cal-results-month">
      <div class="cal-results-month-title"><i class="fas fa-calendar-alt" style="color:#1e8e3e;font-size:12px;"></i>${MONTHS[m]} 2026 <span style="font-size:11px;color:#9aa5b4;font-weight:600;margin-left:4px;">${items.length} announcement${items.length !== 1 ? 's' : ''}</span></div>`;
        items.forEach(r => {
            const d = r.dateObj;
            const isPast = d < today;
            const typeIcon = r.type === 'midterm' ? 'fa-clipboard-check' : 'fa-poll';
            html += `<div class="cal-result-item" style="${isPast ? 'opacity:0.7;' : ''}">
        <div class="cal-result-date-badge">
          <div class="rdb-day">${d.getDate()}</div>
          <div>${SHORT_MONTHS[d.getMonth()]}</div>
        </div>
        <div class="cal-result-icon ${r.type}"><i class="fas ${typeIcon}"></i></div>
        <div class="cal-result-label">${r.label}</div>
        <span class="cal-result-type-tag ${r.type}">${r.type === 'midterm' ? 'Mid-Term' : 'Final'}</span>
        ${isPast ? '<span style="font-size:10px;color:#9aa5b4;font-weight:600;">Past</span>' : '<span style="font-size:10px;color:#1e8e3e;font-weight:600;">Upcoming</span>'}
      </div>`;
        });
        html += `</div>`;
    });
    html += '</div>';
    list.innerHTML = html;
}

function switchCalSchool(i) {
    activeSchool = i;
    renderCalendar();
}

function calSearch(val) {
    calSearchTerm = val;
    renderCalendar();
}

function showDayCourses(year, month, day, key) {
    const starts = (window._calStartMap || {})[key] || [];
    const ranges = (window._calRangeMap || {})[key] || [];
    const all = [...starts, ...ranges.filter(r => !starts.find(s => s.c.title === r.c.title))];
    if (!all.length) return;
    const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    document.getElementById('calDayPopupTitle').textContent = `${day} ${MONTHS[month]} ${year}`;
    const color = window._calSchoolColor || '#1e8e3e';
    document.getElementById('calDayPopupList').innerHTML = all.map(ev => {
        const isTBD = ev.c.start === 'TBD';
        const isStart = starts.find(s => s.c.title === ev.c.title);
        const st = isTBD ? 'tbd' : (isUpcoming(ev.c) ? 'upcoming' : 'past');
        const stLabel = isTBD ? 'TBD' : (isUpcoming(ev.c) ? 'Upcoming' : 'Completed');
        return `<div class="cal-popup-item">
      <div class="cal-popup-dot" style="background:${color}"></div>
      <div class="cal-popup-info">
        <div class="cal-popup-title">${ev.c.title}</div>
        <div class="cal-popup-meta">
          <span><i class="fas fa-hourglass-half"></i> ${ev.c.duration}</span>
          <span class="cc-card-badge ${st}" style="font-size:9px;padding:2px 7px">${stLabel}</span>
          ${isStart ? '<span style="color:#4ecc78;font-size:10px;font-weight:700">▶ Starts today</span>' : '<span style="color:rgba(255,255,255,0.3);font-size:10px">In progress</span>'}
        </div>
      </div>
    </div>`;
    }).join('');
    document.getElementById('calDayPopup').style.display = 'block';
}



/* ===== COURSE CATALOG MODAL ===== */
const catalogData = {
    schools: [
        {
            name: "Air Navigation Services", icon: "fa-tower-cell", color: "#0b2c4d",
            courses: [
                { title: "Basic ATC Course No. 46", duration: "40 Weeks", start: "07.12.2026", end: "03.09.2027" },
                { title: "ATC Refresher (Abridged) For Officers Course No.29", duration: "01 Week", start: "05.01.2026", end: "09.01.2026" },
                { title: "ATC Refresher (Abridged) For Officers Course No.30", duration: "01 Week", start: "30.03.2026", end: "03.04.2026" },
                { title: "ATC Refresher (Abridged) For Officers Course No.31", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                { title: "ATC Refresher (Abridged) For Officers Course No.32", duration: "01 Week", start: "04.05.2026", end: "08.05.2026" },
                { title: "ATC Refresher (Abridged) For Officers Course No.33", duration: "01 Week", start: "08.06.2026", end: "12.06.2026" },
                { title: "ATC Refresher (Abridged) For Officers Course No.34", duration: "01 Week", start: "22.06.2026", end: "26.06.2026" },
                { title: "ATC Refresher (Abridged) For Officers Course No.35", duration: "01 Week", start: "06.07.2026", end: "10.07.2026" },
                { title: "ATC Refresher (Abridged) For Officers Course No.36", duration: "01 Week", start: "13.07.2026", end: "17.07.2026" },
                { title: "ATC Refresher (Abridged) For Officers Course No.37", duration: "01 Week", start: "27.07.2026", end: "31.07.2026" },
                { title: "ATC Refresher (Abridged) For Officers Course No.38", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                { title: "ATC Refresher (Abridged) For Officers Course No.39", duration: "01 Week", start: "24.08.2026", end: "28.08.2026" },
                { title: "ATC Refresher (Abridged) For Officers Course No.40", duration: "01 Week", start: "01.01.2027", end: "01.01.2027" },
                { title: "ATC Assistant Course No. 02", duration: "08 Weeks", start: "TBD", end: "TBD" },
                { title: "ATC Assistant Course No. 03", duration: "08 Weeks", start: "07.09.2026", end: "30.10.2026" },
                { title: "ATC Refresher (Abridged) For Staff Course No.09", duration: "01 Week", start: "26.01.2026", end: "30.01.2026" },
                { title: "ATC Refresher (Abridged) For Staff Course No.10", duration: "01 Week", start: "27.04.2026", end: "01.05.2026" },
                { title: "ATC Refresher (Abridged) For Staff Course No.11", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" },
                { title: "ATC Refresher (Abridged) For Staff Course No.12", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                { title: "Approach Control Surveillance Course No. 06", duration: "05 Weeks", start: "09.02.2026", end: "13.03.2026" },
                { title: "Area / Approach Control Surveillance Course No.60", duration: "12 Weeks", start: "13.04.2026", end: "03.07.2026" },
                { title: "Area / Approach Control Surveillance Course No.61", duration: "12 Weeks", start: "19.10.2026", end: "16.01.2027" },
                { title: "Flight Procedure Design (Conventional) Course No.03", duration: "04 Weeks", start: "08.06.2026", end: "03.07.2026" },
                { title: "Flight Procedure Design (PBN) Course No.01", duration: "03 Weeks", start: "12.10.2026", end: "30.10.2026" },
                { title: "ELP Advanced Course No. 02", duration: "02 Weeks", start: "06.04.2026", end: "17.04.2026" },
                { title: "ELP Course No. 05", duration: "03 Weeks", start: "10.08.2026", end: "04.09.2026" },
                { title: "Basic SAR Course No. 22", duration: "02 Weeks", start: "02.03.2026", end: "13.03.2026" },
                { title: "Basic SAR Course No. 23", duration: "02 Weeks", start: "31.08.2026", end: "11.09.2026" },
                { title: "Basic SAR Course No. 24", duration: "02 Weeks", start: "16.11.2026", end: "27.11.2026" },
                { title: "SAR Coordinator Course No. 02", duration: "02 Weeks", start: "30.11.2026", end: "11.12.2026" },
                { title: "On Job Training Course No. 01", duration: "01 Week", start: "12.01.2026", end: "16.01.2026" },
                { title: "On Job Training Course No. 02", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                { title: "On Job Training Course No. 03", duration: "01 Week", start: "30.11.2026", end: "04.12.2026" },
                { title: "Managing Aeronautical Information Course No. 01", duration: "01 Week", start: "24.08.2026", end: "28.08.2026" },
                { title: "Managing Aeronautical Information Course No. 02", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                { title: "CAVRN (For Officer) Course No. 03", duration: "01 Week", start: "19.01.2026", end: "23.01.2026" },
                { title: "CAVRN (For Officer) Course No. 04", duration: "01 Week", start: "01.06.2026", end: "05.06.2026" },
                { title: "SWIM Course No. 02", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                { title: "SWIM Course No. 03", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                { title: "Aeronautical Radio Frequency Spectrum Management Course No.04", duration: "01 Week", start: "09.02.2026", end: "13.02.2026" },
                { title: "Aeronautical Radio Frequency Spectrum Management Course No.05", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                { title: "COO Course No. 09", duration: "14 Weeks", start: "TBD", end: "TBD" },
                { title: "Communication Operation Assistant Course No. 21", duration: "10 Weeks", start: "TBD", end: "TBD" },
                { title: "IDS AirNav AMHS CRONOS DB Course No. 08", duration: "02 Weeks", start: "05.10.2026", end: "16.10.2026" },
                { title: "IDS AirNav AMHS CRONOS DB Course No. 09", duration: "02 Weeks", start: "07.12.2026", end: "18.12.2026" },
                { title: "SDCPS Software (IOU) Course No. 10", duration: "01 Week", start: "06.07.2026", end: "10.07.2026" },
                { title: "SDCPS Software (IOU) Course No. 11", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                { title: "Aeronautical Mobile Service Officers Course No. 04", duration: "04 Weeks", start: "27.04.2026", end: "22.05.2026" },
                { title: "Aeronautical Mobile Service Staff Course No. 05", duration: "03 Weeks", start: "17.08.2026", end: "04.09.2026" },
                { title: "INDRA MSSR Operation & Maintenance Course No.05", duration: "04 Weeks", start: "05.01.2026", end: "30.01.2026" },
                { title: "ADSB Operation & Maintenance Course No.04", duration: "01 Week", start: "02.03.2026", end: "06.03.2026" },
                { title: "Selex (ILS) Operation & Maintenance Course No.03", duration: "04 Weeks", start: "30.03.2026", end: "24.04.2026" },
                { title: "INDRA PSR Operation & Maintenance Course No.03", duration: "04 Weeks", start: "06.04.2026", end: "30.04.2026" },
                { title: "ATM System Course No.03", duration: "02 Weeks", start: "04.05.2026", end: "15.05.2026" },
                { title: "ELDS MSSR Operation & Maintenance Course No.04", duration: "04 Weeks", start: "20.07.2026", end: "14.08.2026" },
                { title: "Selex DVOR / DME Operation & Maintenance Course No.04", duration: "04 Weeks", start: "23.11.2026", end: "18.12.2026" },
                { title: "Indra Normarc ILS Operation and Maintenance Course No.01", duration: "04 Weeks", start: "TBD", end: "TBD" },
                { title: "Basic ATSEP Refresher (For Officers) Course No.06", duration: "01 Week", start: "23.02.2026", end: "27.02.2026" },
                { title: "ATSEP Data Processing & Networking For Technicians Course No.03", duration: "08 Weeks", start: "08.06.2026", end: "31.07.2026" },
                { title: "ATSEP Radar For Technicians Course No.03", duration: "07 Weeks", start: "17.08.2026", end: "02.10.2026" },
                { title: "ATSEP NAVAIDS For Technicians Course No.02", duration: "06 Weeks", start: "14.09.2026", end: "23.10.2026" },
                { title: "ATSEP Radio Communication For Technicians Course No.03", duration: "06 Weeks", start: "05.10.2026", end: "13.11.2026" },
                { title: "ATSEP Qualification Communication Module Course No.01", duration: "02 Weeks", start: "TBD", end: "TBD" },
                { title: "ATSEP Qualification Navigation Module Course No.01", duration: "02 Weeks", start: "TBD", end: "TBD" },
                { title: "ATSEP Qualification Surveillance Module Course No.01", duration: "02 Weeks", start: "TBD", end: "TBD" },
                { title: "ATSEP Qualification Data Processing Course No.01", duration: "02 Weeks", start: "TBD", end: "TBD" },
                { title: "Basic ATSEP For CNS Technicians/Staff Course No.03", duration: "12 Weeks", start: "TBD", end: "TBD" },
                { title: "Basic ATSEP For CNS Officers Course No.02", duration: "08 Weeks", start: "TBD", end: "TBD" },
                { title: "INDRA VCCS SDC-2000 Operations & Maintenance Course No.01", duration: "01 Week", start: "TBD", end: "TBD" },
                { title: "HARRIS LIBERTY STAR VCCS Operations & Maintenance Course No.01", duration: "01 Week", start: "TBD", end: "TBD" }
            ]
        },
        {
            name: "Regulatory Services", icon: "fa-scale-balanced", color: "#1e5f74",
            courses: [
                { title: "International Air Law Course No.01", duration: "01 Week", start: "04.05.2026", end: "08.05.2026" },
                { title: "Managing Compliance with ICAO SARPs Course No.02", duration: "01 Week", start: "18.05.2026", end: "22.05.2026" },
                { title: "Air Cargo Regulatory Framework & Compliance Course No.02", duration: "01 Week", start: "01.06.2026", end: "05.06.2026" },
                { title: "Annex 14 Application and Requirements Course No.03", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                { title: "Aerodrome Certification Course No.02", duration: "02 Weeks", start: "01.06.2026", end: "12.06.2026" },
                { title: "Aerodrome Manual Course No.02", duration: "01 Week", start: "29.06.2026", end: "03.07.2026" },
                { title: "Aviation Economics Course No.03", duration: "01 Week", start: "19.01.2026", end: "23.01.2026" },
                { title: "Supply and Demand in Air Transport Industry Course No.02", duration: "01 Week", start: "08.02.2026", end: "13.02.2026" },
                { title: "Airport Economics Course No.01", duration: "02 Weeks", start: "06.07.2026", end: "17.07.2026" },
                { title: "Airport Marketing Course No.03", duration: "01 Week", start: "17.08.2026", end: "21.08.2026" },
                { title: "Audit Technique Course No.04", duration: "01 Week", start: "05.01.2026", end: "09.01.2026" },
                { title: "Aeronautical Study Safety Assessment Course No.03 (Jan)", duration: "01 Week", start: "12.01.2026", end: "16.01.2026" },
                { title: "Understanding ICAO Annex-19 Safety Management Course No.02", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                { title: "Fundamental of Air Cargo Operations & Safety (For Staff) Course No.01", duration: "01 Week", start: "13.07.2026", end: "17.07.2026" },
                { title: "ANS Inspector Course No.01", duration: "02 Weeks", start: "20.07.2026", end: "31.07.2026" },
                { title: "Rules of the Air for Aviation Safety Professionals (ICAO Annex 02) Course No.01", duration: "02 Weeks", start: "03.08.2026", end: "07.08.2026" },
                { title: "Aeronautical Study Safety Assessment Course No.03 (Sep)", duration: "01 Week", start: "07.09.2026", end: "11.09.2026" },
                { title: "Audit Technique Course No.05", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" },
                { title: "Understanding ICAO Annex-19 Safety Management Course No.03", duration: "01 Week", start: "07.12.2026", end: "11.12.2026" },
                { title: "Aviation Climate Change Course No.01", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                { title: "Air Passenger Rights Course No.01", duration: "01 Week", start: "30.03.2026", end: "04.04.2026" },
                { title: "Aircraft Accident and Investigation Awareness (ICAO Annex-13) Course No.01", duration: "01 Week", start: "31.08.2026", end: "04.09.2026" },
                { title: "Facilitation (ICAO Annex-9) Course No.01", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                { title: "Disabled Aircraft Recovery Course No.01", duration: "01 Week", start: "19.10.2026", end: "23.10.2026" }
            ]
        },
        {
            name: "Airport Services", icon: "fa-fire-extinguisher", color: "#c0392b",
            courses: [
                { title: "Basic Aerodrome Fire Fighter Course No.03", duration: "12 Weeks", start: "TBD", end: "TBD" },
                { title: "Aerodrome Fire Fighter Refresher Course No.12", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                { title: "Aerodrome Fire Fighter Refresher Course No.13", duration: "01 Week", start: "21.09.2026", end: "25.09.2026" },
                { title: "Aerodrome Fire Fighter Refresher Course No.14", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                { title: "Basic Aerodrome Fire Fighter Course No.61", duration: "12 Weeks", start: "TBD", end: "TBD" },
                { title: "SVD (Refresher) Course No.10", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                { title: "SVD (Refresher) Course No.11", duration: "01 Week", start: "21.09.2026", end: "25.09.2026" },
                { title: "SVD (Refresher) Course No.12", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                { title: "Basic SVD Course No.27", duration: "03 Weeks", start: "TBD", end: "TBD" },
                { title: "Fire Fighting (For Corporate Sector) Course No.01", duration: "01 Week", start: "26.01.2026", end: "30.01.2026" },
                { title: "Fire Fighting (For Corporate Sector) Course No.02", duration: "01 Week", start: "15.06.2026", end: "19.06.2026" },
                { title: "Fire Fighting (For Corporate Sector) Course No.03", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                { title: "Fire Fighting (For Corporate Sector) Course No.04", duration: "01 Week", start: "31.08.2026", end: "04.09.2026" },
                { title: "Fire Fighting (For Corporate Sector) Course No.05", duration: "01 Week", start: "07.09.2026", end: "11.09.2026" },
                { title: "Fire Fighting (For Corporate Sector) Course No.06", duration: "01 Week", start: "19.10.2026", end: "23.10.2026" },
                { title: "Fire Prevention Course No.69", duration: "03 Weeks", start: "23.02.2026", end: "13.03.2026" },
                { title: "Fire Prevention Course No.70", duration: "03 Weeks", start: "01.06.2026", end: "19.06.2026" },
                { title: "Fire Prevention Course No.71", duration: "03 Weeks", start: "29.06.2026", end: "17.07.2026" },
                { title: "Senior Aerodrome Fire Fighter Course No.57", duration: "05 Weeks", start: "05.01.2026", end: "06.02.2026" },
                { title: "Senior Aerodrome Fire Fighter Course No.58", duration: "05 Weeks", start: "17.08.2026", end: "18.09.2026" },
                { title: "Senior Aerodrome Fire Fighter Course No.59", duration: "05 Weeks", start: "26.10.2026", end: "27.11.2026" },
                { title: "Breathing Apparatus Course No.24", duration: "03 Weeks", start: "30.03.2026", end: "17.04.2026" },
                { title: "Breathing Apparatus Course No.25", duration: "03 Weeks", start: "27.04.2026", end: "18.09.2026" },
                { title: "Breathing Apparatus Course No.26", duration: "03 Weeks", start: "20.07.2026", end: "07.08.2026" },
                { title: "Junior Fire Officer Course No.15", duration: "05 Weeks", start: "28.09.2026", end: "30.10.2026" },
                { title: "Fire Investigation (For Officers) Course No.01", duration: "02 Weeks", start: "30.11.2026", end: "11.12.2026" },
                { title: "Aircraft Familiarization (For Officers) Course No.01", duration: "01 Week", start: "28.12.2026", end: "01.01.2027" },
                { title: "Fire Officer (Refresher) Course No.14", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                { title: "Aerodrome Operations (For Officers) Course No.06", duration: "04 Weeks", start: "19.01.2026", end: "13.02.2026" },
                { title: "Facilitation & Hospitality Management (For Officer) Course No.03", duration: "02 Weeks", start: "14.09.2026", end: "25.09.2026" },
                { title: "Facilitation & Hospitality Management (For Staff) Course No.07", duration: "02 Weeks", start: "10.03.2026", end: "30.03.2026" },
                { title: "Facilitation & Hospitality Management (For Officer) Course No.08", duration: "02 Weeks", start: "02.11.2026", end: "13.11.2026" },
                { title: "Airside Safety Awareness Course No.07 (JIAP)", duration: "01 Week", start: "05.01.2026", end: "09.01.2026" },
                { title: "Airside Safety Awareness Course No.08 (IIAP)", duration: "01 Week", start: "19.01.2026", end: "23.01.2026" },
                { title: "Airside Safety Awareness Course No.09 (QIAP)", duration: "01 Week", start: "06.07.2026", end: "10.07.2026" },
                { title: "Airside Safety Awareness Course No.10 (BKIAP)", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                { title: "Airside Safety Awareness Course No.11 (FIAP)", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                { title: "Airside Safety Awareness Course No.12 (AIAP)", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                { title: "Airside Safety Awareness Course No.13 (MIAP)", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                { title: "Basic Bird Shooter Course No.03", duration: "02 Weeks", start: "TBD", end: "TBD" },
                { title: "Marshaller Refresher Course No.02", duration: "01 Week", start: "16.02.2026", end: "20.02.2026" },
                { title: "Marshaller Refresher Course No.03", duration: "01 Week", start: "20.07.2026", end: "27.07.2026" },
                { title: "Aircraft Marshaller Course No.53", duration: "08 Weeks", start: "TBD", end: "TBD" },
                { title: "Wildlife Hazard Management Course No.01", duration: "TBD", start: "TBD", end: "TBD" },
                { title: "Airport Slot Management Course No.01", duration: "TBD", start: "TBD", end: "TBD" },
                { title: "Standard & Recommended Practices For Aerodromes (Annex-14) For Engineers Course No.03", duration: "02 Weeks", start: "05.01.2026", end: "16.01.2026" },
                { title: "Airfield Pavement Maintenance & Surface Evaluation (For Engineers) Course No.02", duration: "01 Week", start: "06.04.2026", end: "10.04.2026" },
                { title: "PBB Maintenance (STP) Course No.08", duration: "02 Weeks", start: "02.02.2026", end: "13.02.2026" },
                { title: "PBB Maintenance (STP) Course No.09", duration: "02 Weeks", start: "02.11.2026", end: "13.11.2026" },
                { title: "Underground Cable Fault Repairs (For Technicians) Course No.03", duration: "02 Weeks", start: "04.05.2026", end: "15.05.2026" },
                { title: "Engineering Estimation (For Staff) Course No.02", duration: "02 Weeks", start: "08.06.2026", end: "19.06.2026" },
                { title: "FIDIC Contract (For Officers) Course No.01", duration: "01 Week", start: "17.08.2026", end: "21.08.2026" }
            ]
        },
        {
            name: "Aviation Management", icon: "fa-chart-line", color: "#1e8e3e",
            courses: [
                { title: "Personal Effectiveness (For Staff) Course No.06", duration: "01 Week", start: "04.05.2026", end: "08.05.2026" },
                { title: "Personal Effectiveness (For Staff) Course No.07", duration: "01 Week", start: "07.09.2026", end: "11.09.2026" },
                { title: "Personal Effectiveness (For Staff) Course No.08", duration: "01 Week", start: "07.12.2026", end: "11.12.2026" },
                { title: "Basic Logistics (For Staff) Course No.05", duration: "03 Weeks", start: "19.01.2026", end: "06.02.2026" },
                { title: "Basic Logistics (For Staff) Course No.06", duration: "03 Weeks", start: "20.04.2026", end: "08.05.2026" },
                { title: "Basic Logistics (For Staff) Course No.07", duration: "03 Weeks", start: "13.07.2026", end: "31.07.2026" },
                { title: "Basic Logistics (For Staff) Course No.08", duration: "03 Weeks", start: "09.11.2026", end: "27.11.2026" },
                { title: "Logistics and Supply Chain Operations (For Officers) Course No.01", duration: "03 Weeks", start: "18.05.2026", end: "05.06.2026" },
                { title: "Logistics and Supply Chain Operations (For Officers) Course No.02", duration: "03 Weeks", start: "07.09.2026", end: "25.09.2026" },
                { title: "Aviation Management Course No.09", duration: "05 Weeks", start: "12.01.2026", end: "13.02.2026" },
                { title: "Aviation Management Course No.10", duration: "05 Weeks", start: "19.10.2026", end: "20.11.2026" },
                { title: "Human Factor in Aviation (For Officers) Course No.03", duration: "01 Week", start: "04.05.2026", end: "08.05.2026" },
                { title: "Human Factor in Aviation (For Officers) Course No.04", duration: "01 Week", start: "26.10.2026", end: "30.10.2026" },
                { title: "Introduction to Aviation IT System (For Staff) Course No.05", duration: "01 Week", start: "22.06.2026", end: "26.06.2026" },
                { title: "Introduction to Aviation IT System (For Staff) Course No.06", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" },
                { title: "IT / Cyber Security, Audit and Compliance Management (For Officers) Course No.01", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                { title: "IT / Cyber Security, Audit and Compliance Management (For Officers) Course No.02", duration: "01 Week", start: "07.12.2026", end: "11.12.2026" },
                { title: "MS Excel - Basic (For Staff) Course No.01", duration: "01 Week", start: "13.04.2026", end: "17.04.2026" },
                { title: "MS Excel - Basic (For Staff) Course No.02", duration: "01 Week", start: "19.10.2026", end: "23.10.2026" },
                { title: "Introduction to ERP (For Staff) Course No.01", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                { title: "Introduction to ERP (For Staff) Course No.02", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                { title: "E-Procurement through E-PADS (For Officers) Course No.03", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                { title: "E-Procurement through E-PADS (For Officers) Course No.04", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                { title: "Basic Office Organization For Asst/Supervisor Course No.21", duration: "02 Weeks", start: "05.01.2026", end: "16.01.2026" },
                { title: "Basic Office Organization For Asst/Supervisor Course No.22", duration: "02 Weeks", start: "31.08.2026", end: "11.09.2026" },
                { title: "Legal Proficiency (For Officers) Course No.01", duration: "03 Weeks", start: "26.01.2026", end: "31.01.2026" },
                { title: "Legal Proficiency (For Staff) Course No.02", duration: "02 Weeks", start: "17.08.2026", end: "28.08.2026" },
                { title: "Legal Proficiency (For Staff) Course No.03", duration: "02 Weeks", start: "12.10.2026", end: "23.10.2026" },
                { title: "ITC Refresher Course No.02", duration: "03 Days", start: "04.02.2026", end: "06.02.2026" },
                { title: "ITC Course No.33", duration: "02 Weeks", start: "06.04.2026", end: "17.04.2026" },
                { title: "ITC Course No.34", duration: "02 Weeks", start: "12.10.2026", end: "23.10.2026" },
                { title: "ETC (Examination Technique Course) Course No.14", duration: "01 Week", start: "08.06.2026", end: "12.06.2026" },
                { title: "ETC (Examination Technique Course) Course No.15", duration: "01 Week", start: "17.08.2026", end: "21.08.2026" },
                { title: "Basic Management System Standards (ISO/SMS) For PAA Officers Course No.02", duration: "01 Week", start: "06.04.2026", end: "10.04.2026" },
                { title: "Basic Management System Standards (ISO/SMS) For PAA Officers Course No.03", duration: "01 Week", start: "30.11.2026", end: "04.12.2026" },
                { title: "Basic Management System Standards (ISO/SMS) For PAA Staff Course No.03", duration: "01 Week", start: "13.04.2026", end: "17.04.2026" },
                { title: "Basic Management System Standards (ISO/SMS) For PAA Staff Course No.04", duration: "01 Week", start: "26.10.2026", end: "30.10.2026" },
                { title: "Finance For Non-Financial Managers (For Officers) Course No.02", duration: "01 Week", start: "21.09.2026", end: "25.09.2026" },
                { title: "Finance For Non-Financial Managers (For Officers) Course No.03", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                { title: "PPRA Rules (For Officers) Course No.02", duration: "01 Week", start: "27.07.2026", end: "31.07.2026" },
                { title: "PPRA Rules (For Officers) Course No.03", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                { title: "Superintendent Development Course No.05", duration: "01 Week", start: "09.02.2026", end: "13.02.2026" },
                { title: "Superintendent Development Course No.06", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" },
                { title: "Emotional Intelligence (For Officers) Course No.03", duration: "01 Week", start: "26.01.2026", end: "30.01.2026" },
                { title: "Emotional Intelligence (For Officers) Course No.04", duration: "01 Week", start: "09.02.2026", end: "13.02.2026" },
                { title: "Emotional Intelligence (For Staff) Course No.01", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                { title: "Emotional Intelligence (For Staff) Course No.02", duration: "01 Week", start: "04.05.2026", end: "08.05.2026" },
                { title: "Leadership Development (For Officers) Course No.02", duration: "01 Week", start: "08.06.2026", end: "12.06.2026" },
                { title: "Leadership Development (For Officers) Course No.03", duration: "01 Week", start: "20.07.2026", end: "24.07.2026" },
                { title: "Foundations of Ethics in Aviation (For Officers) Course No.01", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                { title: "Foundations of Ethics in Aviation (For Officers) Course No.02", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                { title: "Foundations of Ethics in Aviation (For Officers) Course No.03", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                { title: "Foundations of Ethics in Aviation (For Officers) Course No.04", duration: "01 Week", start: "30.11.2026", end: "04.12.2026" }
            ]
        }
    ]
};

let ccSchool = 0, ccStatus = 'all', ccCurrentSchool = null, ccCurrentCourse = null;

function openCourseCatalog(e) {
    e.preventDefault();
    document.getElementById('courseCatalogModal').classList.add('open');
    document.body.style.overflow = 'hidden';
    renderCatalog();
    if (!window._ccMidnightRefreshScheduled) {
        window._ccMidnightRefreshScheduled = true;
        scheduleMidnightRefresh(renderCatalog);
    }
}
function closeCourseCatalog() {
    document.getElementById('courseCatalogModal').classList.remove('open');
    document.body.style.overflow = '';
}
function openCCDetail(si, ci) {
    const school = catalogData.schools[si];
    const c = school.courses[ci];
    const st = ccGetStatus(c);
    const label = st === 'upcoming' ? 'Upcoming' : st === 'tbd' ? 'TBD' : 'Completed';
    document.getElementById('ccDetailContent').innerHTML = `
    <div class="cc-detail-school-tag"><i class="fas ${school.icon}"></i> ${school.name}</div>
    <div class="cc-detail-title">${c.title}</div>
    <div class="cc-detail-grid">
      <div class="cc-detail-item"><label>Duration</label><span>${c.duration}</span></div>
      <div class="cc-detail-item"><label>Status</label><span><span class="cc-card-badge ${st}">${label}</span></span></div>
      <div class="cc-detail-item"><label>Start Date</label><span>${c.start}</span></div>
      <div class="cc-detail-item"><label>End Date</label><span>${c.end}</span></div>
    </div>
    <div class="cc-detail-note"><i class="fas fa-info-circle"></i> Part of CATI Hyderabad – ATP 2026 Training Program</div>`;
    document.getElementById('ccDetailModal').classList.add('open');
}
function closeCCDetail() {
    document.getElementById('ccDetailModal').classList.remove('open');
}
function setCCStatus(s, btn) {
    ccStatus = s;
    document.querySelectorAll('.cc-filter-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    renderCatalog();
}
function ccIsUpcoming(d) {
    return isUpcoming({ start: d, end: null });
}
function ccGetStatus(c) {
    if (c.start === 'TBD') return 'tbd';
    const endDate = parseDMY(c.end) || parseDMY(c.start);
    if (!endDate) return 'completed';
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return endDate >= today ? 'upcoming' : 'completed';
}
function renderCatalog() {
    const school = catalogData.schools[ccSchool];
    const search = (document.getElementById('ccSearchInput') || { value: '' }).value.toLowerCase();
    // Build tabs
    const tabsEl = document.getElementById('ccSchoolTabs');
    if (tabsEl && tabsEl.children.length === 0) {
        tabsEl.innerHTML = catalogData.schools.map((s, i) =>
            `<button class="cc-school-tab${i === ccSchool ? ' active' : ''}" style="--tab-color:${s.color}" onclick="ccSwitchSchool(${i},this)">
        <i class="fas ${s.icon}"></i> ${s.name}
      </button>`).join('');
    }
    // Filter courses
    const filtered = school.courses.filter(c => {
        const matchSearch = !search || c.title.toLowerCase().includes(search);
        const st = ccGetStatus(c);
        const matchStatus = ccStatus === 'all' || ccStatus === st;
        return matchSearch && matchStatus;
    });
    // Stats
    const up = school.courses.filter(c => ccGetStatus(c) === 'upcoming').length;
    const done = school.courses.filter(c => ccGetStatus(c) === 'completed').length;
    const tbd = school.courses.filter(c => ccGetStatus(c) === 'tbd').length;
    document.getElementById('ccStatsRow').innerHTML = `
    <div class="cc-stat-chip"><span class="cc-stat-dot all" style="background:#888"></span><strong>${school.courses.length}</strong> Total</div>
    <div class="cc-stat-chip"><span class="cc-stat-dot up"></span><strong>${up}</strong> Upcoming</div>
    <div class="cc-stat-chip"><span class="cc-stat-dot done"></span><strong>${done}</strong> Completed</div>
    <div class="cc-stat-chip"><span class="cc-stat-dot tbd"></span><strong>${tbd}</strong> TBD</div>`;
    // Grid
    document.getElementById('ccCourseGrid').innerHTML = filtered.length ? filtered.map((c, i) => {
        const st = ccGetStatus(c);
        const label = st === 'upcoming' ? 'Upcoming' : st === 'tbd' ? 'TBD' : 'Completed';
        const si = catalogData.schools.indexOf(school);
        const ci = school.courses.indexOf(c);
        return `<div class="cc-card" style="animation-delay:${Math.min(i, 20) * 0.03}s" onclick="openCCDetail(${si},${ci})">
      <div class="cc-card-top">
        <div class="cc-card-title">${c.title}</div>
        <span class="cc-card-badge ${st}">${label}</span>
      </div>
      <div class="cc-card-meta">
        <span><i class="fas fa-hourglass-half"></i>${c.duration}</span>
        <span><i class="fas fa-calendar-check"></i>${c.start}</span>
      </div>
    </div>`;
    }).join('') : `<div class="cc-empty"><i class="fas fa-search"></i><p>No courses found</p></div>`;
}
function ccSwitchSchool(i, btn) {
    ccSchool = i;
    document.querySelectorAll('.cc-school-tab').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    renderCatalog();
}
document.addEventListener('keydown', e => { if (e.key === 'Escape') { closeCCDetail(); closeCourseCatalog(); closeCalendar(); } });


/* ===== ELIGIBILITY MODAL ===== */
function openEligibility(e) { e.preventDefault(); document.getElementById('eligibilityModal').classList.add('open'); document.body.style.overflow = 'hidden'; }
function switchEligTab(btn, tab) {
    document.querySelectorAll('.elig-tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.elig-tab-content').forEach(t => t.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById('eligTab-' + tab).classList.add('active');
}

/* ===== REQUIRED DOCS MODAL ===== */
function openRequiredDocs(e, tab) {
    if (e && typeof e.preventDefault === 'function') e.preventDefault();
    const el = document.getElementById('reqDocsModal');
    if (el) el.classList.add('open');
    document.body.style.overflow = 'hidden';
    if (tab) {
        document.querySelectorAll('.rdoc-tab').forEach(t => t.classList.remove('active'));
        document.querySelectorAll('.rdoc-tab-content').forEach(t => t.classList.remove('active'));
        const tabContent = document.getElementById('rdocTab-' + tab);
        if (tabContent) tabContent.classList.add('active');
    }
}
function switchRdocTab(btn, tab) {
    document.querySelectorAll('.rdoc-tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.rdoc-tab-content').forEach(t => t.classList.remove('active'));
    if (btn) btn.classList.add('active');
    const content = document.getElementById('rdocTab-' + tab);
    if (content) content.classList.add('active');
}

/* ===== PROFESSIONAL COURSES MODAL ===== */
function openProfCourses(e, tab) {
    e.preventDefault();
    document.getElementById('profCoursesModal').classList.add('open');
    document.body.style.overflow = 'hidden';
    // Switch to correct tab
    document.querySelectorAll('.pc-tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.pc-tab-content').forEach(t => t.classList.remove('active'));
    document.getElementById('pcTab-' + tab + '-btn').classList.add('active');
    document.getElementById('pcTab-' + tab).classList.add('active');
}
function switchPcTab(btn, tab) {
    document.querySelectorAll('.pc-tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.pc-tab-content').forEach(t => t.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById('pcTab-' + tab).classList.add('active');
}
// Shared close (defined early so all opener functions in this block can use it at call-time)
function closeUtilModal(id) { const el = document.getElementById(id); if (el) { el.classList.remove('open'); document.body.style.overflow = ''; } }
document.addEventListener('keydown', e => { if (e.key === 'Escape') { ['eligibilityModal', 'reqDocsModal', 'profCoursesModal', 'eventsModal'].forEach(id => { const el = document.getElementById(id); if (el && el.classList.contains('open')) closeUtilModal(id); }); } });


function openEvents(e) { e.preventDefault(); document.getElementById('eventsModal').classList.add('open'); document.body.style.overflow = 'hidden'; }
function switchEvTab(btn, tab) {
    document.querySelectorAll('.ev-tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.ev-tab-content').forEach(t => t.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById('evTab-' + tab).classList.add('active');
}


function openResults(e) { e.preventDefault(); document.getElementById('resultsModal').classList.add('open'); document.body.style.overflow = 'hidden'; }
function openExamSchedule(e) { e.preventDefault(); document.getElementById('examModal').classList.add('open'); document.body.style.overflow = 'hidden'; }
function switchExamTab(btn, tab) {
    document.querySelectorAll('.exam-tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.exam-tab-content').forEach(t => t.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById('examTab-' + tab).classList.add('active');
}
function openDownloads(e) { e.preventDefault(); document.getElementById('downloadsModal').classList.add('open'); document.body.style.overflow = 'hidden'; }
function filterDownloads(query) {
    const q = query.toLowerCase();
    document.querySelectorAll('.dl-item').forEach(item => {
        const tags = (item.dataset.tags || '') + ' ' + (item.querySelector('.dl-item-name')?.textContent || '');
        item.classList.toggle('hidden', q && !tags.toLowerCase().includes(q));
    });
    document.querySelectorAll('.dl-category').forEach(cat => {
        const visible = cat.querySelectorAll('.dl-item:not(.hidden)').length;
        cat.style.display = visible ? '' : 'none';
    });
}
function closeUtilModal(id) { document.getElementById(id).classList.remove('open'); document.body.style.overflow = ''; }
document.addEventListener('keydown', e => { if (e.key === 'Escape') { ['resultsModal', 'examModal', 'downloadsModal'].forEach(id => { const el = document.getElementById(id); if (el && el.classList.contains('open')) closeUtilModal(id); }); } });

/* ===== CLOSE ANY LEFTOVER MODAL ON BACK/FORWARD NAVIGATION =====
   When a modal (Academic Calendar, Course Catalog, Eligibility, etc.) is
   open and the user navigates to another page, the browser can restore
   this page from its back-forward cache (bfcache) exactly as it was --
   including the modal still marked "open". That's why it appeared to
   pop up on its own when coming back to the homepage. Force every modal
   closed whenever the page is (re)shown. */
window.addEventListener('pageshow', function () {
    ['calendarModal', 'courseCatalogModal', 'ccDetailModal', 'eligibilityModal',
     'reqDocsModal', 'profCoursesModal', 'eventsModal', 'resultsModal',
     'examModal', 'downloadsModal'].forEach(function (id) {
        var el = document.getElementById(id);
        if (el) el.classList.remove('open');
    });
    document.body.style.overflow = '';
});