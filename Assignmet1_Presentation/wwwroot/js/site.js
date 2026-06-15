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
            console.error("SignalR connection failed.", error);
        }
    }

    function createConnection(handlers) {
        if (!window.signalR) {
            console.warn("SignalR client library is not loaded.");
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
                console.debug("SignalR connection stop skipped.", error);
            }
        };

        window.addEventListener("beforeunload", stop, { once: true });

        return { connection, stop };
    }

    return {
        createConnection,
        escapeHtml,
        formatDateTime
    };
})();
