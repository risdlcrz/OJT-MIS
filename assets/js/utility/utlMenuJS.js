function prepareMainMenu() {

    $.ajax({
        type: 'POST',
        url: baseUrl + '/services/utility/apiMainMenu.asmx/GetMainMenu',
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        error: function (errorDesc) {            
            ValidateError(errorDesc)
        },
        success: function (data) {
            LoadMainMenu(data.TABS, data.PAGES)

            ActivateMenu()
        }
    })

}

function LoadMainMenu(tabs, pages) {

    APage();

    var ctr = document.getElementById("side-menu")
    ctr.innerHTML = ''

    tabs.forEach(tab => {
        var li = document.createElement("li")

        if (tab.TG_TYP == "GROUP") {
            /*GROUPS*/
            li.classList = 'menu-title text-uppercase'
            li.innerHTML = tab.TG_TEXT
        }
        else {
            /*TABS*/
            var a = document.createElement("a")

            if (tab.TG_TYP == "PAGE") {
                /*Tab With URL*/
                a.href = baseUrl + tab.TG_URL
                a.classList.add(tab.TG_NAME)

                if (tab.TG_ICON != "" || tab.TG_ICON != null) {
                    var i = document.createElement("i")
                    i.classList = tab.TG_ICON
                    a.appendChild(i)
                }

                var span = document.createElement("span")
                span.innerHTML = tab.TG_TEXT

                li.classList.add(tab.TG_NAME)

                a.appendChild(span)
                li.appendChild(a)
            }
            else {
                /*Tab Collapsible*/
                a.href = "javascript: void(0);"
                a.classList = "has-arrow waves-effect"
                if (tab.TG_ICON != "" || tab.TG_ICON != null) {
                    var i = document.createElement("i")
                    i.classList = tab.TG_ICON
                    a.appendChild(i)
                }

                var span = document.createElement("span")
                span.innerHTML = tab.TG_TEXT

                a.appendChild(span)

                li.appendChild(a)

                var childPages = pages.filter(pg => pg.PAGE_PARENT == tab.TG_NAME)

                if (childPages.length > 0) {

                    var children = document.createElement("ul")
                    children.classList = "sub-menu"
                    children.setAttribute("aria-expanded", "false")

                    childPages.forEach(childPage => {
                        var cLi = document.createElement("li")
                        var cA = document.createElement("a")
                        cA.href = baseUrl + childPage.PAGE_URL
                        cA.textContent = childPage.PAGE_TEXT


                        cLi.classList.add(childPage.PAGE_NAME)
                        cA.classList.add(childPage.PAGE_NAME)
                        li.classList.add(childPage.PAGE_NAME)
                        a.classList.add(childPage.PAGE_NAME)

                        cLi.appendChild(cA)
                        children.appendChild(cLi)
                    })

                    li.appendChild(children)
                }

            }

            
        }
        ctr.appendChild(li)
        $(ctr).metisMenu('dispose').metisMenu()
    })

}

function APage() {

    $.ajax({
        type: 'POST',
        url: baseUrl + '/services/utility/apiMainMenu.asmx/AuthenticatePage',
        data: JSON.stringify( { "cpage": thisPage }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        error: function (errorDesc) {

            
            ValidateError(errorDesc)
        },
        success: function (data) {
            _pageAdd = data.PAGE_ADD
            _pageEdt = data.PAGE_EDT
        }
    })

}
