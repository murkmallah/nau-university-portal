/* ===== "Academics" nav dropdown — School course preview =====
   Each school row in the "Schools" column has a flyout submenu preview.
   Includes a hover buffer and grace period so moving the cursor diagonally
   or across gaps into the submenu is smooth and never accidentally closes it. */
(function () {
  var cache = {};
  var closeTimers = new WeakMap();

  function escapeHtml(s) {
    return (s || '').replace(/[&<>"']/g, function (m) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[m];
    });
  }

  function renderSubmenu(submenu, data, school) {
    var courses = (data && data.courses) || [];
    var html = '';
    courses.forEach(function (c) {
      var courseParam = c.id ? '&course=' + encodeURIComponent(c.id) : '&course=' + encodeURIComponent(c.title);
      html += '<a href="/Courses/Public?school=' + encodeURIComponent(school) + courseParam + '" class="dd-sub-link" data-course-id="' + (c.id || '') + '">' +
        escapeHtml(c.title) + '</a>';
    });
    if (courses.length === 0) {
      html += '<div class="dd-sub-error">No courses listed yet.</div>';
    }
    html += '<a href="/Courses/Public?school=' + encodeURIComponent(school) +
      '" class="dd-sub-viewall">View All <i class="fas fa-arrow-right"></i></a>';
    submenu.innerHTML = html;
  }

  function loadSubmenu(item) {
    var submenu = item.querySelector('.dd-submenu');
    if (!submenu) return;
    var school = submenu.getAttribute('data-school') || '';
    if (!school) return;

    if (cache[school]) {
      renderSubmenu(submenu, cache[school], school);
      return;
    }

    submenu.innerHTML = '<div class="dd-sub-loading">Loading courses&hellip;</div>';
    fetch('/Courses/BySchool?school=' + encodeURIComponent(school) + '&take=4')
      .then(function (r) { if (!r.ok) throw new Error('bad response'); return r.json(); })
      .then(function (data) {
        cache[school] = data;
        renderSubmenu(submenu, data, school);
      })
      .catch(function () {
        submenu.innerHTML = '<div class="dd-sub-error">Couldn\'t load courses.</div>';
      });
  }

  function openItem(item) {
    if (closeTimers.has(item)) {
      clearTimeout(closeTimers.get(item));
      closeTimers.delete(item);
    }
    // Close other school items
    var allItems = document.querySelectorAll('.dd-school-item');
    allItems.forEach(function (other) {
      if (other !== item) {
        if (closeTimers.has(other)) {
          clearTimeout(closeTimers.get(other));
          closeTimers.delete(other);
        }
        other.classList.remove('open');
      }
    });
    item.classList.add('open');
    loadSubmenu(item);
  }

  function scheduleCloseItem(item) {
    if (closeTimers.has(item)) {
      clearTimeout(closeTimers.get(item));
    }
    var timer = setTimeout(function () {
      item.classList.remove('open');
      closeTimers.delete(item);
    }, 350); // 350ms grace period so moving cursor to submenu never drops it
    closeTimers.set(item, timer);
  }

  window.toggleSchoolSubmenu = function (arrowEl) {
    var item = arrowEl.closest('.dd-school-item');
    if (!item) return;
    var wasOpen = item.classList.contains('open');
    if (wasOpen) {
      item.classList.remove('open');
    } else {
      openItem(item);
    }
  };

  function initHover() {
    var items = document.querySelectorAll('.dd-school-item');
    items.forEach(function (item) {
      item.addEventListener('mouseenter', function () {
        openItem(item);
      });
      item.addEventListener('mouseleave', function () {
        scheduleCloseItem(item);
      });

      var submenu = item.querySelector('.dd-submenu');
      if (submenu) {
        submenu.addEventListener('mouseenter', function () {
          if (closeTimers.has(item)) {
            clearTimeout(closeTimers.get(item));
            closeTimers.delete(item);
          }
          item.classList.add('open');
        });
        submenu.addEventListener('mouseleave', function () {
          scheduleCloseItem(item);
        });
      }
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initHover);
  } else {
    initHover();
  }
})();
