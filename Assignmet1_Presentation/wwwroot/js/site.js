window.appHubRealtime = (() => {
    function escapeHtml(value) {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function formatDateTime(value) {
        if (!value) {
            return "-";
        }

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return "-";
        }

        return date.toLocaleString("vi-VN", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit"
        });
    }

    async function startConnection(connection) {
        try {
            await connection.start();
        } catch (error) {
            console.error("Không thể kết nối SignalR.", error);
        }
    }

    function createConnection(handlers) {
        if (!window.signalR) {
            console.warn("Thư viện SignalR chưa được tải.");
            return null;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/app")
            .withAutomaticReconnect()
            .build();

        Object.entries(handlers ?? {}).forEach(([eventName, handler]) => {
            connection.on(eventName, handler);
        });

        void startConnection(connection);

        const stop = async () => {
            try {
                await connection.stop();
            } catch (error) {
                console.debug("Đã bỏ qua thao tác dừng kết nối SignalR.", error);
            }
        };

        return { connection, stop };
    }

    return {
        createConnection,
        escapeHtml,
        formatDateTime
    };
})();

window.appUi = (() => {
    const reducedMotionQuery = window.matchMedia("(prefers-reduced-motion: reduce)");

    function escapeHtml(value) {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function initializeIcons() {
        if (window.lucide?.createIcons) {
            window.lucide.createIcons();
        }
    }

    function normalizePath(path) {
        const normalized = String(path || "/").split("?")[0].replace(/\/+$/, "").toLowerCase();
        return normalized || "/";
    }

    function initializeNavigation() {
        const currentPath = normalizePath(window.location.pathname);
        const links = document.querySelectorAll(".app-nav-links a[href]");

        links.forEach(link => {
            const href = normalizePath(link.getAttribute("href"));
            const isHome = href === "/" || href === "/home" || href === "/home/index";
            const isActive = isHome
                ? currentPath === "/" || currentPath.startsWith("/home")
                : currentPath === href || currentPath.startsWith(`${href}/`);

            if (!isActive) {
                return;
            }

            link.classList.add("active");
            link.setAttribute("aria-current", "page");
            link.closest(".dropdown")?.querySelector(".dropdown-toggle")?.classList.add("active");
        });

        const navCollapse = document.getElementById("mainNav");
        if (!navCollapse || !window.bootstrap?.Collapse) {
            return;
        }

        navCollapse.querySelectorAll("a:not(.dropdown-toggle)").forEach(link => {
            link.addEventListener("click", () => {
                if (window.innerWidth >= 992 || !navCollapse.classList.contains("show")) {
                    return;
                }

                window.bootstrap.Collapse.getOrCreateInstance(navCollapse).hide();
            });
        });
    }

    function initializeScrollExperience() {
        const header = document.querySelector("[data-app-header]");
        const progress = document.querySelector("[data-scroll-progress]");

        const update = () => {
            const top = window.scrollY || document.documentElement.scrollTop;
            header?.classList.toggle("is-scrolled", top > 12);

            if (!progress) {
                return;
            }

            const scrollable = document.documentElement.scrollHeight - window.innerHeight;
            const percentage = scrollable > 0 ? Math.min(100, Math.max(0, (top / scrollable) * 100)) : 0;
            progress.style.transform = `scaleX(${percentage / 100})`;
        };

        update();
        window.addEventListener("scroll", update, { passive: true });
        window.addEventListener("resize", update, { passive: true });
    }

    function initializeRevealMotion() {
        const elements = Array.from(document.querySelectorAll([
            ".app-main .page-hero",
            ".app-main .learning-hero",
            ".app-main .surface-card",
            ".app-main .content-panel",
            ".app-main .stat-card",
            ".app-main .auth-card",
            ".app-main .toolbar-shell",
            ".app-main .detail-shell",
            ".app-main .home-capability-card",
            ".app-main .editor-aside-card",
            ".app-main .privacy-card"
        ].join(",")));

        if (reducedMotionQuery.matches || !("IntersectionObserver" in window)) {
            elements.forEach(element => element.classList.add("ui-reveal-visible"));
            return;
        }

        elements.forEach((element, index) => {
            element.classList.add("ui-reveal");
            element.style.setProperty("--reveal-delay", `${Math.min(index % 6, 5) * 55}ms`);
        });

        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) {
                    return;
                }

                entry.target.classList.add("ui-reveal-visible");
                observer.unobserve(entry.target);
            });
        }, {
            rootMargin: "0px 0px -7% 0px",
            threshold: 0.08
        });

        requestAnimationFrame(() => elements.forEach(element => observer.observe(element)));
    }

    function initializeSurfaceSpotlight() {
        if (reducedMotionQuery.matches || !window.matchMedia("(pointer: fine)").matches) {
            return;
        }

        const surfaces = document.querySelectorAll([
            ".surface-card",
            ".stat-card",
            ".home-capability-card",
            ".editor-aside-card",
            ".privacy-card"
        ].join(","));

        surfaces.forEach(surface => {
            surface.classList.add("ui-spotlight");
            surface.addEventListener("pointermove", event => {
                const bounds = surface.getBoundingClientRect();
                surface.style.setProperty("--pointer-x", `${event.clientX - bounds.left}px`);
                surface.style.setProperty("--pointer-y", `${event.clientY - bounds.top}px`);
            });
        });
    }

    function initializeButtonFeedback() {
        if (reducedMotionQuery.matches) {
            return;
        }

        document.addEventListener("pointerdown", event => {
            const button = event.target.closest(".btn");
            if (!button || button.disabled || button instanceof HTMLInputElement) {
                return;
            }

            const bounds = button.getBoundingClientRect();
            const ripple = document.createElement("span");
            const diameter = Math.max(bounds.width, bounds.height) * 1.45;

            ripple.className = "button-ripple";
            ripple.style.width = `${diameter}px`;
            ripple.style.height = `${diameter}px`;
            ripple.style.left = `${event.clientX - bounds.left - diameter / 2}px`;
            ripple.style.top = `${event.clientY - bounds.top - diameter / 2}px`;

            button.querySelector(".button-ripple")?.remove();
            button.appendChild(ripple);
            ripple.addEventListener("animationend", () => ripple.remove(), { once: true });
        });
    }

    function initializeDeleteConfirmation() {
        document.body.addEventListener("click", event => {
            const button = event.target.closest(".btn-delete-confirm");
            if (!button) {
                return;
            }

            event.preventDefault();
            const form = button.closest("form");
            const itemName = escapeHtml(button.getAttribute("data-item-name") || "mục này");
            const itemType = escapeHtml(button.getAttribute("data-item-type") || "nội dung");
            const canRestore = button.getAttribute("data-can-restore") !== "false";
            const actionLabel = canRestore ? "chuyển vào thùng rác" : "xóa vĩnh viễn";

            if (!window.Swal) {
                if (window.confirm(`Bạn có chắc chắn muốn ${actionLabel} ${itemType} “${itemName}” không?`)) {
                    form?.submit();
                }
                return;
            }

            window.Swal.fire({
                title: canRestore ? "Chuyển vào thùng rác?" : "Xác nhận xóa vĩnh viễn?",
                html: `<div class="delete-confirm-copy">
                           <p>Bạn chuẩn bị ${actionLabel} ${itemType}: <strong>${itemName}</strong>.</p>
                           ${canRestore
                            ? "<p class=\"delete-confirm-note\">Bạn có thể khôi phục nội dung này trong Thùng rác.</p>"
                            : "<p class=\"delete-confirm-note is-danger\">Hành động này không thể hoàn tác.</p>"}
                       </div>`,
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#e11d48",
                cancelButtonColor: "#64748b",
                confirmButtonText: canRestore ? "Chuyển vào thùng rác" : "Xóa vĩnh viễn",
                cancelButtonText: "Hủy bỏ",
                reverseButtons: true,
                focusCancel: true
            }).then(result => {
                if (result.isConfirmed) {
                    form?.submit();
                }
            });
        });
    }

    function initializeVersionRestoreConfirmation() {
        document.body.addEventListener("submit", event => {
            const form = event.target.closest(".version-restore-form");
            if (!form || form.dataset.confirmed === "true") {
                return;
            }

            event.preventDefault();
            const versionNumber = form.getAttribute("data-version-number") || "";
            const quizTitle = escapeHtml(form.getAttribute("data-quiz-title") || "Quiz");
            const fallbackMessage =
                `Khôi phục phiên bản ${versionNumber} của “${quizTitle}”? Bản hiện tại sẽ được sao lưu tự động.`;

            if (!window.Swal) {
                if (window.confirm(fallbackMessage)) {
                    form.dataset.confirmed = "true";
                    form.submit();
                }
                return;
            }

            window.Swal.fire({
                title: `Khôi phục phiên bản ${escapeHtml(versionNumber)}?`,
                html: `<div class="delete-confirm-copy">
                           <p>Quiz sẽ được đưa về nội dung của <strong>${quizTitle}</strong> tại phiên bản đã chọn.</p>
                           <p class="delete-confirm-note">Bản hiện tại được sao lưu tự động và Quiz sẽ trở thành bản nháp.</p>
                       </div>`,
                icon: "question",
                showCancelButton: true,
                confirmButtonColor: "#4f46e5",
                cancelButtonColor: "#64748b",
                confirmButtonText: "Khôi phục phiên bản",
                cancelButtonText: "Hủy bỏ",
                reverseButtons: true,
                focusCancel: true
            }).then(result => {
                if (result.isConfirmed) {
                    form.dataset.confirmed = "true";
                    form.submit();
                }
            });
        });
    }

    function initializeModalLayering() {
        document.querySelectorAll(".app-main .modal").forEach(modal => {
            document.body.appendChild(modal);
        });
    }

    function initializeAlerts() {
        document.querySelectorAll(".app-main > .container > .alert").forEach(alert => {
            window.setTimeout(() => {
                alert.classList.add("is-dismissing");
                alert.addEventListener("transitionend", () => alert.remove(), { once: true });
            }, 6500);
        });
    }

    function initialize() {
        document.documentElement.classList.add("ui-enhanced");
        initializeIcons();
        initializeModalLayering();
        initializeNavigation();
        initializeScrollExperience();
        initializeRevealMotion();
        initializeSurfaceSpotlight();
        initializeButtonFeedback();
        initializeDeleteConfirmation();
        initializeVersionRestoreConfirmation();
        initializeAlerts();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initialize, { once: true });
    } else {
        initialize();
    }

    return {
        initializeIcons
    };
})();
