/* =========================================================
   Mobile Navigation — ADDITIVE FILE
   Har .navbar ke andar ek hamburger button add karta hai
   (sirf <=900px par nazar aata hai) aur .menu ko open/close
   karta hai. Maujooda desktop hover-menu ya kisi aur JS
   se koi cherhchhaar nahi.
   ========================================================= */
(function () {
  // Turns the "Contact" mega-menu (dropdown with Get In Touch /
  // Departments / Student Portal columns) into a plain link that goes
  // straight to the new /Contact page — no dropdown, label "Contact Us".
  // Runs on every page that loads this script (including the DB-driven
  // homepage), since it matches by the nav-link's text rather than by
  // any specific page's markup.
  function simplifyContactMenu(navbar) {
    var menu = navbar.querySelector(':scope > .menu');
    if (!menu) return;

    menu.querySelectorAll(':scope > .menu-item').forEach(function (item) {
      var link = item.querySelector(':scope > .nav-link');
      if (!link) return;

      var label = link.textContent.replace(/\s+/g, ' ').trim().toLowerCase();
      if (label !== 'contact' && label !== 'contact us') return;

      var dropdown = item.querySelector(':scope > .dropdown');
      if (dropdown) dropdown.remove();

      link.textContent = 'Contact Us';
      link.setAttribute('href', '/Contact');
      link.removeAttribute('onclick');
    });
  }

  function initMobileNav(navbar) {
    var menu = navbar.querySelector(':scope > .menu');
    if (!menu || navbar.querySelector('.nav-toggle-btn')) return;

    var btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'nav-toggle-btn';
    btn.setAttribute('aria-label', 'Toggle navigation menu');
    btn.innerHTML = '<i class="fas fa-bars"></i>';
    navbar.insertBefore(btn, menu);

    btn.addEventListener('click', function () {
      var isOpen = menu.classList.toggle('nav-open');
      btn.innerHTML = isOpen ? '<i class="fas fa-times"></i>' : '<i class="fas fa-bars"></i>';
    });

    // Touch screens par hover kaam nahi karta, is liye tap se dropdown open/close hota hai
    menu.querySelectorAll(':scope > .menu-item').forEach(function (item) {
      var link = item.querySelector(':scope > .nav-link');
      var dropdown = item.querySelector(':scope > .dropdown');
      if (!link || !dropdown) return;

      link.addEventListener('click', function (e) {
        if (window.innerWidth > 900) return; // desktop par purana hover behaviour hi chalega
        e.preventDefault();
        var wasOpen = item.classList.contains('dd-open');
        menu.querySelectorAll('.menu-item.dd-open').forEach(function (i) {
          i.classList.remove('dd-open');
        });
        if (!wasOpen) item.classList.add('dd-open');
      });
    });

    // Menu se bahar tap karne par mobile menu band ho jaye
    document.addEventListener('click', function (e) {
      if (window.innerWidth > 900) return;
      if (!navbar.contains(e.target)) {
        menu.classList.remove('nav-open');
        btn.innerHTML = '<i class="fas fa-bars"></i>';
      }
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.navbar').forEach(function (navbar) {
      simplifyContactMenu(navbar);
      initMobileNav(navbar);
    });
  });
})();
