/**
 * NAU CMS Searchable Icon Picker Component
 * Provides visual icon search, category filtering, and direct selection
 * for FontAwesome 6 icons across all CMS modals & inline edit mode.
 */
(function (window, document) {
  "use strict";

  // Comprehensive, categorized list of FontAwesome icons for NAU CMS
  var ICON_DATABASE = [
    // 🚩 Flags & Emblems
    { class: "fas fa-flag", name: "National Flag", cat: "flags", keywords: "flag national country state pkr banner emblem sign pakistan" },
    { class: "far fa-flag", name: "Flag Outline", cat: "flags", keywords: "flag outline mark banner notice sign" },
    { class: "fas fa-flag-checkered", name: "Checkered Flag", cat: "flags", keywords: "flag race checkered competition finish winner sports achievement victory" },
    { class: "fas fa-flag-usa", name: "Flag Star / State", cat: "flags", keywords: "flag star international foreign state america" },
    { class: "fas fa-mountain-sun", name: "Peak / Milestone", cat: "flags", keywords: "flag peak mountain summit milestone achievement victory" },
    { class: "fas fa-monument", name: "Landmark / Monument", cat: "flags", keywords: "monument landmark history pillar memorial" },
    { class: "fas fa-award", name: "Award Ribbon", cat: "flags", keywords: "award ribbon flag badge merit distinction prize" },
    { class: "fas fa-shield", name: "Emblem Shield", cat: "flags", keywords: "shield emblem coat of arms insignia badge" },

    // ✈️ Aviation & Flight
    { class: "fas fa-plane", name: "Airplane", cat: "aviation", keywords: "flight aircraft jet airline fly travel aviation" },
    { class: "fas fa-plane-departure", name: "Plane Departure", cat: "aviation", keywords: "takeoff flight departure airport travel launch" },
    { class: "fas fa-plane-arrival", name: "Plane Arrival", cat: "aviation", keywords: "landing flight arrival airport touchdown" },
    { class: "fas fa-plane-up", name: "Plane Ascend", cat: "aviation", keywords: "takeoff ascend flight climb sky" },
    { class: "fas fa-plane-slash", name: "Plane Slash", cat: "aviation", keywords: "no flight cancelled grounded delay" },
    { class: "fas fa-helicopter", name: "Helicopter", cat: "aviation", keywords: "chopper rotor heli flight aviation" },
    { class: "fas fa-jet-fighter", name: "Jet Fighter", cat: "aviation", keywords: "military jet fighter air force combat supersonic" },
    { class: "fas fa-jet-fighter-up", name: "Fighter Ascend", cat: "aviation", keywords: "fighter jet supersonic climb mission" },
    { class: "fas fa-paper-plane", name: "Paper Plane", cat: "aviation", keywords: "send apply message flight glide" },
    { class: "fas fa-compass", name: "Compass", cat: "aviation", keywords: "navigation heading direction bearing orientation" },
    { class: "fas fa-route", name: "Flight Route", cat: "aviation", keywords: "waypoint path trajectory navigation corridor" },
    { class: "fas fa-location-crosshairs", name: "GPS Target", cat: "aviation", keywords: "target gps waypoint coordinates runway" },
    { class: "fas fa-globe", name: "Globe / World", cat: "aviation", keywords: "global international world universal foreign travel" },

    // 📡 Radar, ATC & Telecom
    { class: "fas fa-tower-broadcast", name: "Tower Broadcast", cat: "radar", keywords: "atc tower radio air traffic control broadcast communication" },
    { class: "fas fa-tower-observation", name: "Control Tower", cat: "radar", keywords: "tower atc control observation airport building" },
    { class: "fas fa-broadcast-tower", name: "Broadcast Tower", cat: "radar", keywords: "antenna transmission radio tower signal" },
    { class: "fas fa-satellite-dish", name: "Satellite Dish", cat: "radar", keywords: "cns atm dish communications radar telemetry" },
    { class: "fas fa-satellite", name: "Satellite", cat: "radar", keywords: "gnss gps orbit telemetry space orbit" },
    { class: "fas fa-radio", name: "Radio / VHF", cat: "radar", keywords: "frequency comms transmission voice vhf" },
    { class: "fas fa-binoculars", name: "Binoculars", cat: "radar", keywords: "visual look observation runway surveillance spotting" },
    { class: "fas fa-headset", name: "Headset / ATC", cat: "radar", keywords: "operator controller support voice comms atc" },
    { class: "fas fa-signal", name: "Signal Bars", cat: "radar", keywords: "reception connectivity wireless network bars" },
    { class: "fas fa-walkie-talkie", name: "Walkie Talkie", cat: "radar", keywords: "radio field handheld comms transceiver" },
    { class: "fas fa-bullhorn", name: "Megaphone / Notice", cat: "radar", keywords: "announcement alert broadcast notice shout speak" },

    // 🎓 Academics, Admissions & Research
    { class: "fas fa-graduation-cap", name: "Graduation Cap", cat: "academics", keywords: "degree convocation student graduate education admission" },
    { class: "fas fa-university", name: "University", cat: "academics", keywords: "institution academy college campus school faculty" },
    { class: "fas fa-school", name: "School / Department", cat: "academics", keywords: "department faculty classroom school academy" },
    { class: "fas fa-book", name: "Book", cat: "academics", keywords: "study course subject textbook reading syllabus" },
    { class: "fas fa-book-open", name: "Open Book", cat: "academics", keywords: "course catalog curriculum reading library open" },
    { class: "fas fa-book-reader", name: "Book Reader", cat: "academics", keywords: "study learning scholar library reading student" },
    { class: "fas fa-certificate", name: "Certificate", cat: "academics", keywords: "diploma certification qualification approval cpd" },
    { class: "fas fa-medal", name: "Medal", cat: "academics", keywords: "winner excellence topper recognition honor competition" },
    { class: "fas fa-trophy", name: "Trophy / Winner", cat: "academics", keywords: "champion winner achievement rank first gold" },
    { class: "fas fa-chalkboard-user", name: "Instructor / Lecture", cat: "academics", keywords: "instructor teacher professor lecture training workshop class" },
    { class: "fas fa-user-graduate", name: "Student Graduate", cat: "academics", keywords: "alumni student scholar enrollee degree convocation" },
    { class: "fas fa-file-signature", name: "Admission Apply", cat: "academics", keywords: "admission apply registration form signature contract" },
    { class: "fas fa-scroll", name: "Scroll / Charter", cat: "academics", keywords: "charter historical document decree degree transcript" },
    { class: "fas fa-microscope", name: "Microscope", cat: "academics", keywords: "research science laboratory experiment analysis" },
    { class: "fas fa-flask", name: "Flask / Science", cat: "academics", keywords: "lab chemistry research experiment test" },
    { class: "fas fa-atom", name: "Atom / Physics", cat: "academics", keywords: "science physics quantum technology innovation" },
    { class: "fas fa-dna", name: "DNA / Biotech", cat: "academics", keywords: "biology research health science genetics" },
    { class: "fas fa-file-lines", name: "Document / Syllabus", cat: "academics", keywords: "notes papers exam syllabus text report article" },
    { class: "fas fa-clipboard-list", name: "Clipboard / Checklist", cat: "academics", keywords: "checklist tasks survey evaluation exam results" },

    // 💻 IT, Cyber Security & Electronics
    { class: "fas fa-laptop-code", name: "Laptop Code", cat: "tech", keywords: "programming software development it cyber coding" },
    { class: "fas fa-shield-halved", name: "Shield Security", cat: "tech", keywords: "security protection cyber defense safety antivirus" },
    { class: "fas fa-shield", name: "Solid Shield", cat: "tech", keywords: "guard secure firewall safety defense" },
    { class: "fas fa-lock", name: "Lock / Privacy", cat: "tech", keywords: "encrypted password auth access security" },
    { class: "fas fa-key", name: "Key / Auth", cat: "tech", keywords: "credential secret login token key access" },
    { class: "fas fa-network-wired", name: "Network Wired", cat: "tech", keywords: "ethernet lan infrastructure servers connectivity" },
    { class: "fas fa-server", name: "Server", cat: "tech", keywords: "datacenter hosting backend cloud hardware" },
    { class: "fas fa-database", name: "Database", cat: "tech", keywords: "sql data storage records query database" },
    { class: "fas fa-microchip", name: "Microchip / Avionics", cat: "tech", keywords: "cpu hardware processor circuit avionics electronics" },
    { class: "fas fa-code", name: "Code Brackets", cat: "tech", keywords: "html script developer programming software" },
    { class: "fas fa-terminal", name: "Terminal / CLI", cat: "tech", keywords: "console shell bash cmd prompt coding" },
    { class: "fas fa-wifi", name: "Wi-Fi", cat: "tech", keywords: "wireless internet connection smart campus signal" },
    { class: "fas fa-fingerprint", name: "Fingerprint", cat: "tech", keywords: "biometric security identity attendance biometric" },
    { class: "fas fa-robot", name: "Robot / AI", cat: "tech", keywords: "artificial intelligence chatbot automation robot" },

    // ⚙️ Engineering, Operations & Tools
    { class: "fas fa-gears", name: "Gears / Mechanics", cat: "engineering", keywords: "gears machinery automation engineering technical" },
    { class: "fas fa-cogs", name: "Cogs / Engineering", cat: "engineering", keywords: "cogs process operation infrastructure mechanics" },
    { class: "fas fa-wrench", name: "Wrench / Maintenance", cat: "engineering", keywords: "tools repair maintenance hangar technician" },
    { class: "fas fa-screwdriver-wrench", name: "Tools Combined", cat: "engineering", keywords: "workshop technician repair maintenance tools" },
    { class: "fas fa-industry", name: "Industry / Factory", cat: "engineering", keywords: "factory manufacturing aerospace plant industry" },
    { class: "fas fa-hard-hat", name: "Safety Helmet", cat: "engineering", keywords: "construction civil airport runway safety engineer" },
    { class: "fas fa-fire-extinguisher", name: "Fire Extinguisher", cat: "engineering", keywords: "rescue firefighting emergency safety rffs" },
    { class: "fas fa-truck-plane", name: "Airport Ground Ops", cat: "engineering", keywords: "cargo luggage ground handling pushback ramp" },
    { class: "fas fa-boxes-stacked", name: "Cargo / Logistics", cat: "engineering", keywords: "freight packages supply chain cargo delivery" },
    { class: "fas fa-warehouse", name: "Hangar / Warehouse", cat: "engineering", keywords: "storage aircraft hangar facility logistics" },
    { class: "fas fa-scale-balanced", name: "Scale / Regulatory", cat: "engineering", keywords: "law rules standards compliance regulatory pcaa icao" },

    // 📊 Management, Finance & Business
    { class: "fas fa-chart-line", name: "Chart Line", cat: "management", keywords: "growth analytics finance statistics trends chart" },
    { class: "fas fa-chart-pie", name: "Chart Pie", cat: "management", keywords: "breakdown metrics report share percentage" },
    { class: "fas fa-briefcase", name: "Briefcase / Business", cat: "management", keywords: "management executive job career business" },
    { class: "fas fa-coins", name: "Coins / Fees", cat: "management", keywords: "tuition challan financial money payment fee" },
    { class: "fas fa-hand-holding-dollar", name: "Scholarship", cat: "management", keywords: "aid grant funding waiver merit scholarship money" },
    { class: "fas fa-receipt", name: "Receipt / Challan", cat: "management", keywords: "challan invoice voucher bill receipt payment" },
    { class: "fas fa-building", name: "Corporate Building", cat: "management", keywords: "headquarters office corporate administration" },
    { class: "fas fa-users", name: "Users / Team", cat: "management", keywords: "people community faculty group team staff" },
    { class: "fas fa-user-tie", name: "Executive / Admin", cat: "management", keywords: "leadership manager rector director executive" },
    { class: "fas fa-handshake", name: "Partnership / MOU", cat: "management", keywords: "deal agreement industry collaboration partnership mou" },
    { class: "fas fa-lightbulb", name: "Lightbulb / Idea", cat: "management", keywords: "innovation strategy solution concept discovery" },
    { class: "fas fa-target", name: "Target / Vision", cat: "management", keywords: "goals vision 2030 objective mission target" },

    // 📢 News, Announcements & Media
    { class: "fas fa-newspaper", name: "Newspaper", cat: "news", keywords: "newspaper news press media release announcement article" },
    { class: "fas fa-star", name: "Featured Star", cat: "news", keywords: "star special highlight important top featured rating" },
    { class: "fas fa-fire", name: "Hot / Trending", cat: "news", keywords: "fire hot trend urgent breaking popular exciting" },
    { class: "fas fa-bell", name: "Notification Bell", cat: "news", keywords: "alert updates reminder news notification chime" },
    { class: "fas fa-calendar-alt", name: "Calendar Date", cat: "news", keywords: "calendar date schedule timetable meeting schedule" },
    { class: "fas fa-calendar-check", name: "Calendar Approved", cat: "news", keywords: "calendar check event approved deadline scheduled confirmed" },
    { class: "fas fa-clock", name: "Clock / Time", cat: "news", keywords: "time office hours period length schedule duration" },
    { class: "fas fa-envelope", name: "Envelope / Email", cat: "news", keywords: "mail contact message inquiry letter email" },
    { class: "fas fa-circle-exclamation", name: "Alert Notice", cat: "news", keywords: "warning caution important notice alert exclamation" },
    { class: "fas fa-image", name: "Image / Photo", cat: "news", keywords: "picture gallery media photo banner image" },
    { class: "fas fa-video", name: "Video / Film", cat: "news", keywords: "movie clip recording media video camera" },
    { class: "fas fa-camera", name: "Camera", cat: "news", keywords: "camera photo picture photography snapshot" },

    // ⛅ Weather & Skies
    { class: "fas fa-wind", name: "Wind / Weather", cat: "weather", keywords: "breeze air wind turbulence weather MET" },
    { class: "fas fa-cloud", name: "Cloud", cat: "weather", keywords: "weather cloud sky atmosphere ceiling" },
    { class: "fas fa-cloud-sun", name: "Cloud Sun", cat: "weather", keywords: "weather forecast daytime skies sunny cloudy" },
    { class: "fas fa-sun", name: "Sun / Bright", cat: "weather", keywords: "sun sunny daylight clear visibility" },
    { class: "fas fa-bolt", name: "Lightning / Storm", cat: "weather", keywords: "lightning bolt electricity thunder storm energy power" }
  ];

  var CATEGORIES = [
    { id: "all", label: "🌟 All" },
    { id: "flags", label: "🚩 Flags" },
    { id: "aviation", label: "✈️ Aviation" },
    { id: "academics", label: "🎓 Academics" },
    { id: "radar", label: "📡 Radar & ATC" },
    { id: "tech", label: "💻 IT & Cyber" },
    { id: "engineering", label: "⚙️ Engineering" },
    { id: "management", label: "📊 Business" },
    { id: "news", label: "📢 News & Media" },
    { id: "weather", label: "⛅ Weather" }
  ];

  // ===== ENHANCE AN INPUT FIELD INTO A SEARCHABLE ICON PICKER =====
  function enhanceInput(input) {
    if (!input) return;
    if (input.dataset.nauIconEnhanced === "true") return;
    if (input.classList.contains("nau-icon-live-search") || input.classList.contains("nd-icon-search-input")) return;
    if (input.closest(".nau-icon-searchable-group") || input.closest(".nd-icon-picker-box")) {
      input.dataset.nauIconEnhanced = "true";
      return;
    }

    var id = (input.id || "").toLowerCase();
    var name = (input.name || "").toLowerCase();

    // Check if input is specifically meant for icon (by id or name)
    var isIconInput =
      id === "icon" ||
      id.endsWith("icon") ||
      id.endsWith("iconclass") ||
      name === "icon" ||
      name.endsWith("icon") ||
      input.dataset.nauIcon === "true";

    if (!isIconInput) return;

    input.dataset.nauIconEnhanced = "true";

    // Create wrapper container
    var group = document.createElement("div");
    group.className = "nau-icon-searchable-group";

    var currentVal = (input.value || "fas fa-newspaper").trim();

    // Find initial matching icon name
    var initialMatch = ICON_DATABASE.find(function (i) { return i.class === currentVal; });
    var initialName = initialMatch ? initialMatch.name : (currentVal || "Choose Icon");

    group.innerHTML = [
      '<div class="nau-icon-bar-wrap">',
      '  <div class="nau-icon-preview-chip" title="Active Selected Icon">',
      '    <i class="' + currentVal + '"></i>',
      '  </div>',
      '  <div class="nau-icon-search-input-box">',
      '    <i class="fas fa-search search-icon"></i>',
      '    <input type="text" class="nau-icon-live-search" data-nau-icon-enhanced="true" placeholder="Search 100+ icons (e.g. flag, plane, award, book, shield, bell)..." />',
      '  </div>',
      '  <div class="nau-icon-current-tag" title="Selected Icon Class">' + initialName + '</div>',
      '</div>',
      '<div class="nau-icon-cat-scroll"></div>',
      '<div class="nau-icon-live-grid"></div>'
    ].join("\n");

    // Populate Category Pills
    var catScroll = group.querySelector(".nau-icon-cat-scroll");
    CATEGORIES.forEach(function (cat, idx) {
      var pill = document.createElement("button");
      pill.type = "button";
      pill.className = "nau-icon-cat-pill" + (idx === 0 ? " active" : "");
      pill.textContent = cat.label;
      pill.dataset.cat = cat.id;
      pill.addEventListener("click", function (e) {
        e.preventDefault();
        catScroll.querySelectorAll(".nau-icon-cat-pill").forEach(function (p) { p.classList.remove("active"); });
        pill.classList.add("active");
        renderGrid();
      });
      catScroll.appendChild(pill);
    });

    var grid = group.querySelector(".nau-icon-live-grid");
    var searchInput = group.querySelector(".nau-icon-live-search");
    var previewIcon = group.querySelector(".nau-icon-preview-chip i");
    var currentTag = group.querySelector(".nau-icon-current-tag");

    function renderGrid() {
      var query = (searchInput.value || "").toLowerCase().trim();
      var activePill = catScroll.querySelector(".nau-icon-cat-pill.active");
      var activeCat = activePill ? activePill.dataset.cat : "all";

      grid.innerHTML = "";

      var filtered = ICON_DATABASE.filter(function (item) {
        if (activeCat !== "all" && item.cat !== activeCat) return false;
        if (query) {
          var matchName = item.name.toLowerCase().indexOf(query) !== -1;
          var matchClass = item.class.toLowerCase().indexOf(query) !== -1;
          var matchKey = (item.keywords || "").toLowerCase().indexOf(query) !== -1;
          return matchName || matchClass || matchKey;
        }
        return true;
      });

      if (filtered.length === 0) {
        grid.innerHTML = '<div class="nau-icon-no-match"><i class="fas fa-magnifying-glass"></i> No icons found matching "' + query + '". Try typing "flag", "plane", "book", "award", "shield"...</div>';
        return;
      }

      filtered.forEach(function (item) {
        var chip = document.createElement("div");
        var isSelected = (input.value || "").trim() === item.class;
        chip.className = "nau-icon-card-chip" + (isSelected ? " active" : "");
        chip.title = item.name + " (" + item.class + ")";
        chip.innerHTML = '<i class="' + item.class + '"></i><span>' + item.name + '</span>';

        chip.addEventListener("click", function (e) {
          e.preventDefault();
          selectIcon(item.class, item.name);
        });

        grid.appendChild(chip);
      });
    }

    function selectIcon(cls, name) {
      input.value = cls;
      previewIcon.className = cls;
      currentTag.textContent = name || cls;

      grid.querySelectorAll(".nau-icon-card-chip").forEach(function (c) { c.classList.remove("active"); });
      var activeChip = grid.querySelector('.nau-icon-card-chip[title*="' + cls + '"]');
      if (activeChip) activeChip.classList.add("active");

      input.dispatchEvent(new Event("input", { bubbles: true }));
      input.dispatchEvent(new Event("change", { bubbles: true }));
    }

    searchInput.addEventListener("input", function () {
      renderGrid();
    });

    // Hide the original raw input, keep in DOM for ASP.NET model binding / fetch
    input.style.display = "none";
    input.parentNode.insertBefore(group, input);
    group.appendChild(input);

    // Initial render
    renderGrid();

    // Listen to programmatic updates
    input.addEventListener("change", function () {
      var val = (input.value || "").trim();
      previewIcon.className = val || "fas fa-newspaper";
      var match = ICON_DATABASE.find(function (i) { return i.class === val; });
      currentTag.textContent = match ? match.name : (val || "Custom Icon");
      renderGrid();
    });
  }

  // Scan and enhance icon inputs
  var isScanning = false;
  function scanAndEnhance() {
    if (isScanning) return;
    isScanning = true;
    try {
      var selector = 'input[id*="icon"]:not([data-nau-icon-enhanced="true"]), input[id*="Icon"]:not([data-nau-icon-enhanced="true"]), input[name*="Icon"]:not([data-nau-icon-enhanced="true"]), input[data-nau-icon="true"]:not([data-nau-icon-enhanced="true"])';
      document.querySelectorAll(selector).forEach(enhanceInput);
    } finally {
      isScanning = false;
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", scanAndEnhance);
  } else {
    scanAndEnhance();
  }

  window.NauIconPicker = {
    enhance: enhanceInput,
    scan: scanAndEnhance,
    icons: ICON_DATABASE,
    categories: CATEGORIES
  };

})(window, document);
