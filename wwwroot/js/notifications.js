// ========== NOTIFICATIONS ==========

let notificationPollInterval = null;

async function initNotifications() {
    const btn = document.getElementById("btnNotifications");
    if (!btn) return;

    // Dropdown oluştur
    const dropdown = document.createElement("div");
    dropdown.id = "notifDropdown";
    dropdown.className = "notif-dropdown";
    dropdown.innerHTML = `
        <div class="notif-header">
            <span>Notifications</span>
            <button id="btnMarkAllRead">Mark all read</button>
        </div>
        <div class="notif-list" id="notifList">
            <div class="notif-empty">No notifications yet.</div>
        </div>
    `;

    // Wrapper div'e ekle
    btn.parentElement.appendChild(dropdown);

    // Toggle aç/kapat
    btn.addEventListener("click", async (e) => {
        e.stopPropagation();
        const isOpen = dropdown.classList.contains("open");
        dropdown.classList.toggle("open");
        if (!isOpen) {
            await loadNotifications();
        }
    });

    // Dışarı tıklayınca kapat
    document.addEventListener("click", (e) => {
        if (!dropdown.contains(e.target) && e.target !== btn) {
            dropdown.classList.remove("open");
        }
    });

    // Mark all read
    document.getElementById("btnMarkAllRead").addEventListener("click", async (e) => {
        e.stopPropagation();
        await fetch(`${window.API_BASE_URL}/notifications/mark-all-read`, {
            method: "PUT",
            credentials: "include"
        });
        await loadNotifications();
    });

    // İlk yükleme + polling (30 saniyede bir)
    await loadNotifications();
    notificationPollInterval = setInterval(loadNotifications, 30000);
}

async function loadNotifications() {
    try {
        const res = await fetch(`${window.API_BASE_URL}/notifications`, {
            credentials: "include"
        });
        if (!res.ok) return;

        const data = await res.json();
        if (!data.success) return;

        updateBadge(data.data);
        renderNotifications(data.data);
    } catch (err) {
        console.error("Notification load error:", err);
    }
}

function updateBadge(notifications) {
    const btn = document.getElementById("btnNotifications");
    if (!btn) return;

    const unreadCount = notifications.filter(n => !n.isRead).length;
    let badge = btn.querySelector(".notif-badge");

    if (unreadCount > 0) {
        if (!badge) {
            badge = document.createElement("span");
            badge.className = "notif-badge";
            btn.appendChild(badge);
        }
        badge.textContent = unreadCount > 9 ? "9+" : unreadCount;
    } else {
        if (badge) badge.remove();
    }
}

function renderNotifications(notifications) {
    const list = document.getElementById("notifList");
    if (!list) return;

    if (!notifications || notifications.length === 0) {
        list.innerHTML = `<div class="notif-empty">No notifications yet.</div>`;
        return;
    }

    list.innerHTML = notifications.map(n => `
        <div class="notif-item ${n.isRead ? "read" : "unread"}" data-id="${n.notificationId}">
            <div class="notif-icon">${getNotifIcon(n.type)}</div>
            <div class="notif-body">
                <div class="notif-msg">${escapeHtmlNotif(n.message)}</div>
                <div class="notif-time">${timeAgo(n.createdAt)}</div>
            </div>
            ${!n.isRead ? `<div class="notif-dot"></div>` : ""}
        </div>
    `).join("");

    list.querySelectorAll(".notif-item.unread").forEach(item => {
        item.addEventListener("click", async (e) => {
            e.stopPropagation();
            const id = item.dataset.id;
            await fetch(`${window.API_BASE_URL}/notifications/${id}/read`, {
                method: "PUT",
                credentials: "include"
            });
            await loadNotifications();
        });
    });
}

function getNotifIcon(type) {
    if (type === "NewRequest") return "📩";
    if (type === "Approved") return "✅";
    if (type === "Rejected") return "❌";
    return "🔔";
}

function timeAgo(dateStr) {
    const date = new Date(dateStr);
    const diff = Math.floor((Date.now() - date.getTime()) / 1000);
    if (diff < 60) return "Just now";
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
    if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
    return `${Math.floor(diff / 86400)}d ago`;
}

function escapeHtmlNotif(text) {
    if (!text) return "";
    return String(text)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;");
}

window.initNotifications = initNotifications;
window.loadNotifications = loadNotifications;