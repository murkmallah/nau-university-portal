/* ===== NAU GLOBAL NAVBAR SEARCH ===== */
(function () {
    let searchDebounceTimer = null;

    function executeSearchQuery() {
        const box = document.getElementById("searchBox");
        if (!box) return;
        const query = box.value.trim();
        if (query.length > 0) {
            window.location.href = "/Search?q=" + encodeURIComponent(query);
        }
    }

    window.toggleSearch = function (event) {
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
            } else if (icon) {
                icon.parentNode.insertBefore(box, icon.nextSibling);
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
    };

    window.closeSearch = function () {
        setTimeout(function () {
            closeSearchDropdown();
            const box = document.getElementById("searchBox");
            if (box && document.activeElement !== box && (!box.value || box.value.trim().length === 0)) {
                box.classList.remove("open");
            }
        }, 250);
    };

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
            searchDebounceTimer = setTimeout(function () {
                fetchLiveSearchResults(query);
            }, 250);
        });
    }

    function fetchLiveSearchResults(query) {
        fetch("/Search/Api?q=" + encodeURIComponent(query))
            .then(function (res) { return res.json(); })
            .then(function (data) { renderLiveSearchDropdown(data, query); })
            .catch(function (err) { console.error("Search API error:", err); });
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
            dropdown.innerHTML = '<div class="nau-search-no-items">No quick results for "' + escapeHtml(query) + '". Press Enter for full search.</div>';
        } else {
            let html = results.map(function (item) {
                return '<a href="' + item.url + '" class="nau-search-item">' +
                    '<i class="' + (item.icon || 'fas fa-search') + '"></i>' +
                    '<div class="nau-search-item-info">' +
                    '<span class="nau-search-item-title">' + escapeHtml(item.title) + '</span>' +
                    '<span class="nau-search-item-cat">' + escapeHtml(item.category) + '</span>' +
                    '</div>' +
                    '</a>';
            }).join("");
            html += '<a href="/Search?q=' + encodeURIComponent(query) + '" class="nau-search-view-all">View all results for "' + escapeHtml(query) + '" &rarr;</a>';
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

        const icon = document.querySelector(".search-icon");
        if (icon && !icon.getAttribute("onclick")) {
            icon.addEventListener("click", window.toggleSearch);
        }
    });
})();
