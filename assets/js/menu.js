document.addEventListener("DOMContentLoaded", function () {
    fetch("/assets/json/menu.json")
        .then(response => {
            if (!response.ok) throw new Error('Failed to load menu.json: ' + response.status);
            return response.json();
        })
        .then(data => {
            const sidebar = document.getElementById("sidebarMenu");
            const navbarUser = document.getElementById("navbarUser");
            // current page filename (lowercase), strip querystring
            let currentPage = (window.location.pathname.split("/").pop() || "").toLowerCase().split('?')[0];
            sidebar.innerHTML = ''; // clear sidebar

            (data.menu || []).forEach(item => {
                if (item.submenu) {
                    let submenuHTML = "";
                    let hasActive = false;

                    item.submenu.forEach(sub => {
                        const subLink = (sub.link || "").toLowerCase();
                        let active = currentPage === subLink ? "active" : "";
                        if (active) hasActive = true;
                        submenuHTML += `
<li class="nav-item">
  <a href="${sub.link}" class="nav-link ${active}">
    <p>${sub.name}</p>
  </a>
</li>
`;
                    });

                    sidebar.insertAdjacentHTML("beforeend", `
<li class="nav-item has-treeview ${hasActive ? "menu-open" : ""}">
  <a href="#" class="nav-link ${hasActive ? "active" : ""}" aria-expanded="${hasActive}">
    <i class="nav-icon ${item.icon}"></i>
    <p>
      ${item.name}
      <i class="right fas fa-angle-left ${hasActive ? "is-open" : ""}"></i>
    </p>
  </a>
  <ul class="nav nav-treeview">
    ${submenuHTML}
  </ul>
</li>
`);
                } else {
                    const itemLink = (item.link || "").toLowerCase();
                    let active = currentPage === itemLink ? "active" : "";
                    sidebar.insertAdjacentHTML("beforeend", `
<li class="nav-item">
  <a href="${item.link}" class="nav-link ${active}">
    <i class="nav-icon ${item.icon}"></i>
    <p>${item.name}</p>
  </a>
</li>
`);
                }
            });

            sidebar.querySelectorAll(".has-treeview > .nav-link").forEach(toggle => {
                toggle.addEventListener("click", event => {
                    event.preventDefault();
                    const parent = toggle.closest(".has-treeview");
                    const isOpen = parent.classList.toggle("menu-open");
                    toggle.setAttribute("aria-expanded", isOpen);
                    toggle.querySelector(".right").classList.toggle("is-open", isOpen);
                });
            });

            // Navbar user menu
            let dropdownItems = "";
            (data.navbar && data.navbar.user && data.navbar.user.menu || []).forEach(i => {
                dropdownItems += `
<a href="${i.link}" class="dropdown-item">
  <i class="${i.icon} mr-2"></i> ${i.name}
</a>
`;
            });

            navbarUser.innerHTML = `
<li class="nav-item dropdown">
  <a class="nav-link dropdown-toggle" data-toggle="dropdown">
    <i class="fas fa-user mr-1"></i> ${data.navbar && data.navbar.user ? data.navbar.user.name : 'User'}
  </a>
  <div class="dropdown-menu dropdown-menu-right">
    ${dropdownItems}
  </div>
</li>
`;
        })
        .catch(err => {
            console.error('menu.js error:', err);
        });
});