/* ===== NAU About page CMS enhancement (admin only) =====
   Adds "Add ..." / per-item "remove" buttons on top of the generic
   cms-editor.js text/image editing, so an Admin can grow or shrink
   repeatable groups without hand-writing HTML:
     - History of CAA: "Key Milestones" timeline + info cards
     - History of CATI: any timeline/info-card layout it shares with CAA
     - Management: "Core Functions", "Governance Bodies" and
       "Post-2016 Restructuring"
   Everything added here is stripped back out of the saved HTML by
   cms-editor.js's cleanedHtml(), so only the real content (title/text,
   year/description, etc.) gets persisted.

   initAboutCmsEnhancer(ids) wires this up for one editable region; call it
   once per region so every database-backed About section gets the same
   add/remove item behaviour.
*/
function initAboutCmsEnhancer(ids) {
    "use strict";
    ids = ids || {};

    var region = document.getElementById(ids.region || "cms-editable-region");
    var editBtn = document.getElementById(ids.editBtn || "cmsEditBtn");
    if (!region || !editBtn) return;

    // Only meaningful on sections with a layout this enhancer knows how to
    // grow/shrink -- if this region has none of those, do nothing.
    var hasTimeline   = !!region.querySelector(".timeline-list");
    var hasInfoCard   = !!region.querySelector(".info-card");
    var hasCoreFn     = !!region.querySelector(".core-functions");
    var hasMgmtGrid   = !!region.querySelector(".mgmt-grid");
    if (!hasTimeline && !hasInfoCard && !hasCoreFn && !hasMgmtGrid) return;

    // Avoid wiring the same region up twice if this ever gets called more
    // than once for it.
    if (region.dataset.aboutCmsEnhanced === "1") return;
    region.dataset.aboutCmsEnhanced = "1";

    // One-time styles for the editing chrome (add buttons + remove badges) --
    // shared across every region, only injected once per page.
    if (!document.getElementById("aboutCmsEnhancerStyles")) {
        var style = document.createElement("style");
        style.id = "aboutCmsEnhancerStyles";
        style.textContent =
            ".cms-item-pos { position: relative; }" +
            ".cms-remove-btn { position:absolute; top:6px; right:6px; width:22px; height:22px; border-radius:50%; " +
            "border:2px solid #fff; background:#e53935; color:#fff; font-size:15px; font-weight:700; line-height:1; " +
            "font-family:Arial,Helvetica,sans-serif; padding:0; cursor:pointer; " +
            "display:flex; align-items:center; justify-content:center; z-index:5; box-shadow:0 1px 4px rgba(0,0,0,.25); }" +
            ".cms-remove-btn:hover { background:#c62828; }" +
            ".cms-add-btn { display:flex; align-items:center; justify-content:center; gap:6px; width:100%; " +
            "padding:10px; margin-top:6px; border:1.5px dashed #94a3b8; background:#f8fafc; color:#0b2c4d; " +
            "font-family:inherit; font-weight:700; font-size:12px; border-radius:10px; cursor:pointer; min-height:44px; }" +
            ".cms-add-btn:hover { background:#eef2f7; border-color:#0b2c4d; }" +
            ".core-functions .cms-add-btn, .mgmt-grid .cms-add-btn { min-height: 120px; }";
        document.head.appendChild(style);
    }

    function addRemoveBtn(container, title) {
        container.classList.add("cms-item-pos");
        var btn = document.createElement("button");
        btn.type = "button";
        btn.className = "cms-remove-btn";
        btn.title = title || "Remove";
        btn.setAttribute("aria-label", title || "Remove");
        btn.textContent = "\u00D7"; // plain "×" character -- avoids any icon-font glitches
        btn.addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            if (confirm("Remove this item? This can't be undone after you save.")) {
                container.remove();
            }
        });
        container.appendChild(btn);
    }

    // Finds the repeatable-group container (matching containerSelector)
    // that immediately follows the <h3> whose text starts with headingText.
    // Used for Management, where "Core Functions" and "Post-2016
    // Restructuring" both use the same .core-functions/.core-fn markup, so
    // they have to be told apart by which heading they sit under rather
    // than by class name alone.
    function findContainerAfterHeading(headingText, containerSelector) {
        var headings = region.querySelectorAll("h3");
        for (var i = 0; i < headings.length; i++) {
            if (headings[i].textContent.trim().indexOf(headingText) === 0) {
                var el = headings[i].nextElementSibling;
                while (el) {
                    if (el.matches(containerSelector)) return el;
                    el = el.nextElementSibling;
                }
            }
        }
        return null;
    }

    function wrapExistingItems(container, itemSelector, removeTitle) {
        if (!container) return;
        container.querySelectorAll(itemSelector).forEach(function (item) {
            if (item.querySelector(":scope > .cms-remove-btn")) return;
            addRemoveBtn(item, removeTitle);
        });
    }

    function addGroupAddButton(container, btnClass, label, buildItem, removeTitle) {
        if (!container || container.querySelector(":scope > ." + btnClass)) return;
        var btn = document.createElement("button");
        btn.type = "button";
        btn.className = "cms-add-btn " + btnClass;
        btn.innerHTML = '<i class="fas fa-plus"></i> ' + label;
        btn.addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            var item = buildItem();
            container.insertBefore(item, btn);
            addRemoveBtn(item, removeTitle);
        });
        container.appendChild(btn);
    }

    // ----- History of CAA / CATI: timeline milestones -----
    function enhanceTimeline() {
        var list = region.querySelector(".timeline-list");
        if (!list) return;
        wrapExistingItems(list, ".timeline-item", "Remove milestone");
        addGroupAddButton(list, "cms-add-milestone-btn", "Add Milestone", function () {
            var item = document.createElement("div");
            item.className = "timeline-item";
            item.innerHTML =
                '<div class="timeline-year">Year</div>' +
                '<div class="timeline-desc">New milestone — click to edit this text</div>';
            return item;
        }, "Remove milestone");
    }

    // ----- History of CAA / CATI: info cards (sidebar) -----
    function enhanceInfoCards() {
        var sidebar = region.querySelector(".about-sidebar");
        if (!sidebar) return;
        wrapExistingItems(sidebar, ".info-card", "Remove card");
        if (sidebar.querySelector(".cms-add-infocard-btn")) return;
        var btn = document.createElement("button");
        btn.type = "button";
        btn.className = "cms-add-btn cms-add-infocard-btn";
        btn.innerHTML = '<i class="fas fa-plus"></i> Add Info Card';
        btn.addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            var card = document.createElement("div");
            card.className = "info-card";
            card.innerHTML =
                '<div class="info-card-icon"><i class="fas fa-star"></i></div>' +
                '<h4>New Card Title</h4>' +
                '<p>Click to edit this description text.</p>' +
                '<span class="info-badge">Label</span>';
            sidebar.insertBefore(card, btn);
            addRemoveBtn(card, "Remove card");
        });
        sidebar.appendChild(btn);
    }

    // ----- Management: Core Functions -----
    function enhanceCoreFunctions() {
        var container = findContainerAfterHeading("Core Functions", ".core-functions");
        if (!container) return;
        wrapExistingItems(container, ".core-fn", "Remove function");
        addGroupAddButton(container, "cms-add-corefn-btn", "Add Function", function () {
            var item = document.createElement("div");
            item.className = "core-fn";
            item.innerHTML =
                '<i class="fas fa-star"></i>' +
                '<h4>New Function</h4>' +
                '<p>Click to edit this description.</p>';
            return item;
        }, "Remove function");
    }

    // ----- Management: Governance Bodies -----
    function enhanceGovernanceBodies() {
        var container = findContainerAfterHeading("Governance Bodies", ".mgmt-grid");
        if (!container) return;
        wrapExistingItems(container, ".mgmt-card", "Remove body");
        addGroupAddButton(container, "cms-add-mgmtcard-btn", "Add Governance Body", function () {
            var item = document.createElement("div");
            item.className = "mgmt-card";
            item.innerHTML =
                '<div class="mgmt-card-icon" style="background:rgba(11,44,77,0.08);color:#0b2c4d;"><i class="fas fa-building"></i></div>' +
                '<h4>New Body</h4>' +
                '<p>Click to edit this description.</p>';
            return item;
        }, "Remove body");
    }

    // ----- Management: Post-2016 Restructuring -----
    // Shares the exact same .core-functions/.core-fn markup as "Core
    // Functions" above -- told apart only by which heading it follows.
    function enhanceRestructuring() {
        var container = findContainerAfterHeading("Post-2016 Restructuring", ".core-functions");
        if (!container) return;
        wrapExistingItems(container, ".core-fn", "Remove entity");
        addGroupAddButton(container, "cms-add-restructure-btn", "Add Entity", function () {
            var item = document.createElement("div");
            item.className = "core-fn";
            item.style.background = "linear-gradient(135deg,#334155,#475569)";
            item.innerHTML =
                '<i class="fas fa-circle-info"></i>' +
                '<h4>New Entity</h4>' +
                '<p>Click to edit this description.</p>';
            return item;
        }, "Remove entity");
    }

    function enhance() {
        if (hasTimeline) enhanceTimeline();
        if (hasInfoCard) enhanceInfoCards();
        if (hasCoreFn) {
            enhanceCoreFunctions();
            enhanceRestructuring();
        }
        if (hasMgmtGrid) enhanceGovernanceBodies();
    }

    editBtn.addEventListener("click", enhance);
}
