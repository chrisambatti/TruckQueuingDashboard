// ──────────────────────────────────────────────
// SignalR Connection
// ──────────────────────────────────────────────

if (typeof signalR !== 'undefined') {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/fleetHub")
        .withAutomaticReconnect()
        .build();

    connection.start()
        .then(() => console.log('✅ SignalR connected'))
        .catch(err => console.error('❌ SignalR connection error:', err));

    // ── Refresh Dashboard ──
    connection.on("RefreshDashboard", function () {
        if (typeof window.refreshDashboardData === 'function') {
            window.refreshDashboardData();
        } else {
            location.reload();
        }
    });

    // ── Truck Called (legacy) ──
    connection.on("TruckCalled", function (vehicleNumber, username) {
        showToast(`Truck ${vehicleNumber} called to the front`, true);
    });

    // ── Real-time Notifications ──
    connection.on("ReceiveNotification", function (message, type, timestamp) {
        addNotification(message, type, timestamp);
        showToast(message, type !== "Exit");
    });

} else {
    console.warn('⚠️ SignalR library not loaded – check script order.');
}

// ──────────────────────────────────────────────
// Notification Management
// ──────────────────────────────────────────────

const MAX_NOTIFICATIONS = 20;
let notificationCount = 0;

function addNotification(message, type, timestamp) {
    const list = document.getElementById('notificationList');
    if (!list) return;

    const placeholder = list.querySelector('.text-muted');
    if (placeholder && notificationCount === 0) {
        placeholder.remove();
    }

    const item = document.createElement('div');
    item.className = 'dropdown-item notification-item';
    const timeStr = new Date(timestamp).toLocaleTimeString();
    // message already contains HTML – render it as is
    item.innerHTML = `
        <div class="d-flex justify-content-between align-items-start">
            <span>${message}</span>
            <small class="text-muted" style="font-size:0.7rem;">${timeStr}</small>
        </div>
    `;
    list.prepend(item);

    while (list.children.length > MAX_NOTIFICATIONS) {
        list.removeChild(list.lastChild);
    }

    notificationCount = Math.min(notificationCount + 1, MAX_NOTIFICATIONS);
    const badge = document.getElementById('notificationBadge');
    if (badge) {
        badge.textContent = notificationCount;
        badge.style.display = notificationCount > 0 ? 'inline-block' : 'none';
    }
}

function clearNotificationBadge() {
    notificationCount = 0;
    const badge = document.getElementById('notificationBadge');
    if (badge) {
        badge.textContent = '0';
        badge.style.display = 'none';
    }
}

// Optional: clear badge when dropdown is opened
document.addEventListener('DOMContentLoaded', function () {
    const dropdown = document.getElementById('notificationDropdown');
    if (dropdown) {
        dropdown.addEventListener('shown.bs.dropdown', function () {
        });
    }
});

// ──────────────────────────────────────────────
// Toast Notification Helper
// ──────────────────────────────────────────────

function showToast(message, isSuccess = true) {
    const toast = document.getElementById('toastNotification');
    const toastBody = document.getElementById('toastBody');
    if (!toast) return;
    toastBody.textContent = message;
    toast.className = `toast align-items-center text-white ${isSuccess ? 'bg-success' : 'bg-danger'} border-0 show`;
    setTimeout(() => {
        toast.classList.remove('show');
    }, 3000);
}

// ──────────────────────────────────────────────
// Global Utility Functions
// ──────────────────────────────────────────────

function formatDate(dateString) {
    if (!dateString) return '—';
    const date = new Date(dateString);
    return date.toLocaleString('en-GB', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: true
    });
}

// ──────────────────────────────────────────────
// Document Ready
// ──────────────────────────────────────────────

$(document).ready(function () {
    console.log('Truck Queuing Dashboard loaded.');
});