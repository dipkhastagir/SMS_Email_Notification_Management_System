// NotifyHub front-end helpers (no build step, plain JavaScript)
(function () {
    "use strict";

    // ---- mobile sidebar ----
    document.addEventListener("click", function (e) {
        if (e.target.closest("[data-toggle-nav]")) {
            document.body.classList.toggle("nav-open");
        } else if (document.body.classList.contains("nav-open") && !e.target.closest(".sidebar")) {
            document.body.classList.remove("nav-open");
        }
    });

    // ---- toasts ----
    window.showToast = function (message, type) {
        var stack = document.getElementById("toastStack");
        if (!stack) return;
        var icons = { success: "bi-check-circle-fill", error: "bi-exclamation-octagon-fill", info: "bi-lightning-charge-fill" };
        var el = document.createElement("div");
        el.className = "app-toast " + (type || "success");
        el.setAttribute("role", "status");
        el.innerHTML = '<i class="bi ' + (icons[type] || icons.success) + '"></i><div></div>' +
            '<button class="close-toast" aria-label="Dismiss"><i class="bi bi-x-lg"></i></button>';
        el.querySelector("div").textContent = message;
        el.querySelector(".close-toast").addEventListener("click", function () { el.remove(); });
        stack.appendChild(el);
        setTimeout(function () { el.remove(); }, 6500);
    };
    document.querySelectorAll("[data-toast]").forEach(function (n) {
        window.showToast(n.getAttribute("data-toast"), n.getAttribute("data-toast-type"));
    });

    // ---- confirm dialog for destructive forms: <form data-confirm="..."> ----
    var pendingForm = null;
    document.addEventListener("submit", function (e) {
        var form = e.target;
        if (!form.hasAttribute || !form.hasAttribute("data-confirm") || form.dataset.confirmed === "1") return;
        var modalEl = document.getElementById("confirmModal");
        if (!modalEl || !window.bootstrap) { if (!confirm(form.getAttribute("data-confirm"))) e.preventDefault(); return; }
        e.preventDefault();
        pendingForm = form;
        modalEl.querySelector("[data-confirm-text]").textContent = form.getAttribute("data-confirm");
        var okBtn = modalEl.querySelector("[data-confirm-ok]");
        okBtn.textContent = form.getAttribute("data-confirm-button") || "Delete";
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    });
    document.addEventListener("click", function (e) {
        if (e.target.closest("[data-confirm-ok]") && pendingForm) {
            pendingForm.dataset.confirmed = "1";
            bootstrap.Modal.getInstance(document.getElementById("confirmModal")).hide();
            pendingForm.submit();
            pendingForm = null;
        }
    });

    // ---- template editor: placeholder chips, live preview, character counter ----
    var body = document.querySelector("[data-template-body]");
    if (body) {
        var preview = document.querySelector("[data-preview]");
        var subjectInput = document.querySelector("[data-template-subject]");
        var subjectPreview = document.querySelector("[data-preview-subject]");
        var counter = document.querySelector("[data-counter]");
        var samples = {};
        try { samples = JSON.parse(document.getElementById("sampleValues").textContent); } catch (err) { samples = {}; }

        var escapeHtml = function (s) {
            return s.replace(/[&<>"']/g, function (c) { return ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]; });
        };
        var render = function (text) {
            return escapeHtml(text || "").replace(/\{(\w+)\}/g, function (m, key) {
                return samples[key] !== undefined ? "<mark>" + escapeHtml(samples[key]) + "</mark>" : "<mark>" + m + "</mark>";
            });
        };
        var refresh = function () {
            if (preview) preview.innerHTML = render(body.value) || '<span class="text-muted">Your message preview appears here.</span>';
            if (subjectPreview && subjectInput) subjectPreview.innerHTML = render(subjectInput.value) || "No subject";
            if (counter) {
                var len = body.value.length;
                var segs = len === 0 ? 0 : (len <= 160 ? 1 : Math.ceil(len / 153));
                counter.textContent = len + " characters, " + segs + " SMS segment" + (segs === 1 ? "" : "s");
            }
        };
        body.addEventListener("input", refresh);
        if (subjectInput) subjectInput.addEventListener("input", refresh);

        document.querySelectorAll("[data-insert]").forEach(function (chip) {
            chip.addEventListener("click", function () {
                var token = chip.getAttribute("data-insert");
                var start = body.selectionStart || body.value.length, end = body.selectionEnd || body.value.length;
                body.value = body.value.slice(0, start) + token + body.value.slice(end);
                body.focus();
                body.selectionStart = body.selectionEnd = start + token.length;
                refresh();
            });
        });

        // channel switch shows / hides the subject field
        var subjectWrap = document.querySelector("[data-subject-wrap]");
        var syncChannel = function () {
            var checked = document.querySelector("input[name='Channel']:checked");
            var isEmail = checked && checked.value === "Email";
            if (subjectWrap) subjectWrap.hidden = !isEmail;
            document.querySelectorAll("[data-email-only]").forEach(function (n) { n.hidden = !isEmail; });
            document.querySelectorAll("[data-sms-only]").forEach(function (n) { n.hidden = isEmail; });
        };
        document.querySelectorAll("input[name='Channel']").forEach(function (r) { r.addEventListener("change", syncChannel); });
        syncChannel();
        refresh();
    }

    // ---- manual send: start from a template ----
    var templatePicker = document.querySelector("[data-template-picker]");
    if (templatePicker && body) {
        templatePicker.addEventListener("change", function () {
            var opt = templatePicker.selectedOptions[0];
            if (!opt || !opt.value) return;
            body.value = opt.getAttribute("data-body") || "";
            var ch = opt.getAttribute("data-channel");
            var radio = document.querySelector("input[name='Channel'][value='" + ch + "']");
            if (radio) { radio.checked = true; radio.dispatchEvent(new Event("change")); }
            var subj = document.querySelector("[data-template-subject]");
            if (subj) subj.value = opt.getAttribute("data-subject") || "";
            body.dispatchEvent(new Event("input"));
        });
    }

    // ---- trigger form: fields & defaults follow the chosen event ----
    var eventSelect = document.querySelector("[data-event-select]");
    if (eventSelect) {
        var meta = JSON.parse(document.getElementById("eventMeta").textContent);
        var fieldSelect = document.querySelector("[data-field-select]");
        var opSelect = document.querySelector("[data-op-select]");
        var valueInput = document.querySelector("[data-value-input]");
        var help = document.querySelector("[data-event-help]");
        var chips = document.querySelector("[data-event-chips]");
        var recipientHint = document.querySelector("[data-recipient-hint]");
        var sentence = document.querySelector("[data-rule-sentence]");

        var fillFields = function (applyDefaults) {
            var info = meta[eventSelect.value];
            var current = fieldSelect.value;
            fieldSelect.innerHTML = "";
            if (!info) { if (help) help.textContent = "Choose which business event this trigger watches."; return; }
            info.fields.forEach(function (f) {
                var o = document.createElement("option"); o.value = f; o.textContent = f; fieldSelect.appendChild(o);
            });
            if (applyDefaults) {
                fieldSelect.value = info.defaultField; opSelect.value = info.defaultOperator; valueInput.value = info.defaultValue;
            } else if (info.fields.indexOf(current) >= 0) {
                fieldSelect.value = current;
            }
            if (help) help.textContent = info.description;
            if (chips) chips.innerHTML = info.placeholders.map(function (p) { return '<span class="chip">' + p + "</span>"; }).join(" ");
            if (recipientHint) recipientHint.textContent = eventSelect.value === "StockLow"
                ? "Required for low stock alerts: products have no contact of their own."
                : "Optional. Leave empty to message the customer or employee on the record.";
            updateSentence();
        };
        var updateSentence = function () {
            if (!sentence) return;
            var info = meta[eventSelect.value];
            sentence.textContent = info
                ? "Send when " + info.displayName.toLowerCase() + " has " + fieldSelect.value + " " + opSelect.value + " " + (valueInput.value || "…")
                : "";
        };
        eventSelect.addEventListener("change", function () { fillFields(true); });
        [fieldSelect, opSelect, valueInput].forEach(function (el) { el.addEventListener("input", updateSentence); el.addEventListener("change", updateSentence); });
        fillFields(!fieldSelect.getAttribute("data-initial"));
        if (fieldSelect.getAttribute("data-initial")) { fieldSelect.value = fieldSelect.getAttribute("data-initial"); updateSentence(); }
    }

    // ---- queue: delivery attempts drawer ----
    document.addEventListener("click", function (e) {
        var btn = e.target.closest("[data-attempts]");
        if (!btn) return;
        var drawer = document.getElementById("attemptsDrawer");
        if (!drawer || !window.bootstrap) return;
        drawer.querySelector("[data-drawer-title]").textContent = "Message #" + btn.getAttribute("data-attempts");
        drawer.querySelector("[data-drawer-body-text]").textContent = btn.getAttribute("data-body") || "";
        drawer.querySelector("[data-drawer-subject]").textContent = btn.getAttribute("data-subject") || "";
        drawer.querySelector("[data-drawer-subject]").hidden = !btn.getAttribute("data-subject");
        var list = drawer.querySelector("[data-drawer-attempts]");
        list.innerHTML = '<div class="text-muted small py-3">Loading attempts…</div>';
        bootstrap.Offcanvas.getOrCreateInstance(drawer).show();
        fetch(btn.getAttribute("data-url"), { headers: { "X-Requested-With": "fetch" } })
            .then(function (r) { return r.text(); })
            .then(function (html) { list.innerHTML = html; })
            .catch(function () { list.innerHTML = '<div class="text-danger small">Could not load attempts. Refresh and try again.</div>'; });
    });

    // ---- dashboard: live numbers every 15 s ----
    var live = document.querySelector("[data-live-url]");
    if (live) {
        var tick = function () {
            fetch(live.getAttribute("data-live-url"), { headers: { "X-Requested-With": "fetch" } })
                .then(function (r) { return r.ok ? r.json() : null; })
                .then(function (d) {
                    if (!d) return;
                    document.querySelectorAll("[data-stat]").forEach(function (el) {
                        var key = el.getAttribute("data-stat");
                        if (d[key] !== undefined) el.textContent = key === "successRate" ? d[key] + "%" : d[key];
                    });
                }).catch(function () { });
        };
        setInterval(tick, 15000);
    }
})();
