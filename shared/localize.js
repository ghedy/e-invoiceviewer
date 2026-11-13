"use strict";
document.documentElement.lang = browser.i18n.getMessage("locale");
for (const element of document.querySelectorAll("[data-i18n]")) {
    element.textContent = browser.i18n.getMessage(element.dataset.i18n);
}
