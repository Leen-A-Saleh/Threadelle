// ThreadElle — Admin panel: interactions + real-time notification system

(function () {
    'use strict';

    const TE = window.TE || { token: '', isAdmin: true };

    // ── Helpers ──────────────────────────────────────────────
    function postForm(url, data) {
        const body = new URLSearchParams();
        body.append('__RequestVerificationToken', TE.token);
        if (data) Object.keys(data).forEach(k => { if (data[k] !== null && data[k] !== undefined) body.append(k, data[k]); });
        return fetch(url, {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': TE.token },
            body
        }).then(r => r.json());
    }

    function getJson(url) {
        return fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } }).then(r => r.json());
    }

    function setNotifBadge(count) {
        const b = document.getElementById('notifBadge');
        if (!b) return;
        b.textContent = count > 99 ? '99+' : count;
        b.classList.toggle('d-none', !count || count <= 0);
    }

    function esc(s) {
        const d = document.createElement('div');
        d.textContent = s || '';
        return d.innerHTML;
    }

    // ── Sidebar toggle (mobile) ───────────────────────────────
    const sidebar  = document.getElementById('adminSidebar');
    const toggle   = document.getElementById('adminSidebarToggle');
    const backdrop = document.getElementById('adminBackdrop');

    function openSidebar()  { sidebar && sidebar.classList.add('is-open');  backdrop && backdrop.classList.add('is-visible'); }
    function closeSidebar() { sidebar && sidebar.classList.remove('is-open'); backdrop && backdrop.classList.remove('is-visible'); }

    toggle   && toggle.addEventListener('click', openSidebar);
    backdrop && backdrop.addEventListener('click', closeSidebar);

    // ── Delete buttons (.te-delete-btn) ──────────────────────
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.te-delete-btn');
        if (!btn) return;

        const url  = btn.dataset.url;
        const name = btn.dataset.name || 'this item';

        Swal.fire({
            title: 'Move to Recycle Bin?',
            html: '<span style="color:#7A3B5A">' + esc(name) + '</span> will be soft-deleted and can be restored later.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#C97B8E',
            cancelButtonColor: '#aaa',
            confirmButtonText: 'Yes, delete it',
            cancelButtonText: 'Cancel'
        }).then(function (result) {
            if (!result.isConfirmed) return;
            postForm(url)
                .then(function (data) {
                    if (data.ok) {
                        window.teToast && window.teToast(data.message || 'Deleted successfully.', 'success');
                        const row = btn.closest('tr');
                        if (row) {
                            row.style.transition = 'opacity .3s';
                            row.style.opacity = '0';
                            setTimeout(function () { row.remove(); }, 320);
                        }
                    } else {
                        window.teToast && window.teToast(data.message || 'Could not delete.', 'error');
                    }
                })
                .catch(function () {
                    window.teToast && window.teToast('Something went wrong. Please try again.', 'error');
                });
        });
    });

    // ── Auto-show flash toasts ────────────────────────────────
    document.querySelectorAll('.te-toast[data-autoshow="true"]').forEach(function (el) {
        requestAnimationFrame(function () { el.classList.add('is-visible'); });
        var close = function () { el.classList.remove('is-visible'); setTimeout(function () { el.remove(); }, 350); };
        var closeBtn = el.querySelector('.te-toast-close');
        if (closeBtn) closeBtn.addEventListener('click', close);
        setTimeout(close, 3800);
    });

    // ── Admin notification system ─────────────────────────────

    // Type → { icon, color }
    var NOTIF_META = {
        AdminCreate:      { icon: 'check-circle',       color: '#22c55e' },
        AdminUpdate:      { icon: 'pen',                color: '#f59e0b' },
        AdminDelete:      { icon: 'trash-can',          color: '#ef4444' },
        NewOrder:         { icon: 'bag-shopping',       color: '#3b82f6' },
        NewCustomOrder:   { icon: 'wand-magic-sparkles',color: '#8b5cf6' },
        NewReview:        { icon: 'star',               color: '#ec4899' },
        NewTestimonial:   { icon: 'comment-dots',       color: '#ec4899' },
        NewUserRegistered:{ icon: 'user-plus',          color: '#14b8a6' }
    };

    function metaFor(type) {
        return NOTIF_META[type] || { icon: 'bell', color: 'var(--te-dusty-rose)' };
    }

    function refreshAdminBadge() {
        getJson('/Admin/Notifications/AdminCount')
            .then(function (d) { setNotifBadge(d.unread); })
            .catch(function () {});
    }

    function buildNotifItem(n) {
        var m = metaFor(n.type);
        var unreadClass = n.isRead ? '' : ' is-unread';
        var dotHtml = n.isRead ? '' : '<span class="te-notif-dot"></span>';

        var inner =
            '<div class="te-admin-notif-icon" style="background:' + m.color + '22;color:' + m.color + '">' +
                '<i class="fas fa-' + m.icon + '"></i>' +
            '</div>' +
            '<div class="te-admin-notif-body">' +
                dotHtml +
                '<div class="te-admin-notif-title">' + esc(n.title) + '</div>' +
                '<div class="te-admin-notif-msg">' + esc(n.message) + '</div>' +
                '<div class="te-admin-notif-time">' + esc(n.timeAgo) + '</div>' +
            '</div>';

        var item;
        if (n.url) {
            item = document.createElement('a');
            item.href = n.url;
            item.className = 'te-admin-notif-item' + unreadClass;
        } else {
            item = document.createElement('div');
            item.className = 'te-admin-notif-item' + unreadClass;
        }
        item.innerHTML = inner;
        item.dataset.id = n.id;

        // Mark as read on click
        if (!n.isRead) {
            item.addEventListener('click', function () {
                postForm('/Admin/Notifications/AdminMarkRead', { id: n.id });
                item.classList.remove('is-unread');
                var dot = item.querySelector('.te-notif-dot');
                if (dot) dot.remove();
                // decrement badge
                var b = document.getElementById('notifBadge');
                if (b) {
                    var cur = parseInt(b.textContent, 10) || 0;
                    setNotifBadge(Math.max(0, cur - 1));
                }
            });
        }

        return item;
    }

    function loadAdminNotifications() {
        var body = document.getElementById('notifCanvasBody');
        if (!body) return;

        body.innerHTML =
            '<div class="te-skeleton-list p-3">' +
            [1,2,3].map(function() {
                return '<div class="te-skeleton-item"><div class="te-skeleton-thumb" style="border-radius:8px;width:36px;height:36px;flex-shrink:0"></div>' +
                    '<div class="te-skeleton-content"><div class="te-skeleton-title" style="width:55%"></div><div class="te-skeleton-text" style="width:80%"></div></div></div>';
            }).join('') + '</div>';

        getJson('/Admin/Notifications/AdminRecent')
            .then(function (d) {
                setNotifBadge(d.unread);

                if (!d.items || d.items.length === 0) {
                    body.innerHTML =
                        '<div class="te-empty-state py-5 text-center">' +
                        '<i class="far fa-bell" style="font-size:2.2rem;color:var(--te-blush);display:block;margin-bottom:.6rem;"></i>' +
                        '<p style="color:var(--te-text-muted);font-size:.85rem;">No notifications yet</p></div>';
                    return;
                }

                var list = document.createElement('div');
                list.className = 'te-admin-notif-list';

                d.items.forEach(function (n) {
                    list.appendChild(buildNotifItem(n));
                });

                var footer = document.createElement('div');
                footer.className = 'te-admin-notif-footer';
                footer.innerHTML =
                    '<a href="/Admin/Notifications" class="te-admin-notif-history-link">' +
                    '<i class="fas fa-history me-1"></i>View full history</a>';

                body.innerHTML = '';
                body.appendChild(list);
                body.appendChild(footer);
            })
            .catch(function () {
                body.innerHTML = '<p class="text-center text-muted py-3 small">Could not load notifications.</p>';
            });
    }

    // Wire offcanvas
    var notifCanvas = document.getElementById('notifCanvas');
    if (notifCanvas) {
        notifCanvas.addEventListener('show.bs.offcanvas', loadAdminNotifications);

        document.getElementById('notifReadAll') && document.getElementById('notifReadAll').addEventListener('click', function (e) {
            e.preventDefault();
            postForm('/Admin/Notifications/AdminMarkAllRead')
                .then(function () {
                    setNotifBadge(0);
                    // Visually clear unread state
                    document.querySelectorAll('.te-admin-notif-item.is-unread').forEach(function (el) {
                        el.classList.remove('is-unread');
                        var dot = el.querySelector('.te-notif-dot');
                        if (dot) dot.remove();
                    });
                });
        });
    }

    // Initial badge load
    refreshAdminBadge();

    // ── SignalR real-time connection ──────────────────────────
    var _pollTimer = null;

    function startPolling() {
        if (_pollTimer) return;
        _pollTimer = setInterval(refreshAdminBadge, 30000);
    }

    if (window.signalR) {
        var connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/admin-notifications')
            .withAutomaticReconnect([0, 3000, 10000, 30000])
            .build();

        connection.on('NewNotification', function () {
            refreshAdminBadge();
            // If canvas is open, reload it too
            if (notifCanvas && notifCanvas.classList.contains('show')) {
                loadAdminNotifications();
            }
        });

        connection.start()
            .then(function () {
                // SignalR connected — still poll as a backup every 2 minutes
                _pollTimer = setInterval(refreshAdminBadge, 120000);
            })
            .catch(function () {
                // Fall back to 30-second polling
                startPolling();
            });

        connection.onclose(function () { startPolling(); });
        connection.onreconnecting(function () { startPolling(); });
        connection.onreconnected(function () {
            clearInterval(_pollTimer);
            _pollTimer = setInterval(refreshAdminBadge, 120000);
            refreshAdminBadge();
        });
    } else {
        startPolling();
    }

}());
