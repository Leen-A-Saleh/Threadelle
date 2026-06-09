// ThreadElle — brand interactions, animations & storefront AJAX

(function () {
    'use strict';

    const TE = window.TE || { token: '', isAuthenticated: false };

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

    // ── Toasts ───────────────────────────────────────────────
    function toast(message, type) {
        type = type || 'success';
        const wrap = document.getElementById('toastWrap');
        if (!wrap) { alert(message); return; }
        const el = document.createElement('div');
        el.className = 'te-toast te-toast-' + type;
        el.innerHTML = '<i class="fas fa-heart"></i><span></span><button class="te-toast-close" aria-label="Close">&times;</button>';
        el.querySelector('span').innerHTML = message;
        wrap.appendChild(el);
        requestAnimationFrame(() => el.classList.add('is-visible'));
        const close = () => { el.classList.remove('is-visible'); setTimeout(() => el.remove(), 350); };
        el.querySelector('.te-toast-close').addEventListener('click', close);
        setTimeout(close, 3800);
    }
    window.teToast = toast;

    // ── Badges ───────────────────────────────────────────────
    function setBadge(id, count) {
        const b = document.getElementById(id);
        if (!b) return;
        b.textContent = count;
        b.classList.toggle('d-none', !count || count <= 0);
    }
    function refreshCartBadge() { getJson('/Cart/Count').then(d => setBadge('cartBadge', d.count)).catch(() => {}); }
    function refreshWishlistBadge() { getJson('/Wishlist/Count').then(d => setBadge('wishlistBadge', d.count)).catch(() => {}); }
    function refreshNotifBadge() { if (!TE.isAuthenticated || TE.isAdmin) return; getJson('/Notification/Count').then(d => setBadge('notifBadge', d.unread)).catch(() => {}); }
    window.teRefreshCartBadge = refreshCartBadge;

    // ── Add to cart ──────────────────────────────────────────
    function addToCart(productId, colorId, qty, btn) {
        if (btn) { btn.classList.add('is-loading'); btn.disabled = true; }
        return postForm('/Cart/Add', { productId, colorId, quantity: qty || 1 })
            .then(res => {
                if (res.ok) {
                    setBadge('cartBadge', res.count);
                    toast(res.message || 'Added to your bag');
                    if (btn) bumpHeartIfAny(btn);
                } else {
                    toast(res.message || 'Could not add to bag', 'danger');
                }
            })
            .catch(() => toast('Something went wrong. Please try again.', 'danger'))
            .finally(() => { if (btn) { btn.classList.remove('is-loading'); btn.disabled = false; } });
    }
    window.teAddToCart = addToCart;

    function bumpHeartIfAny(btn) {
        btn.classList.add('te-pop');
        setTimeout(() => btn.classList.remove('te-pop'), 350);
    }

    // ── Wishlist toggle (works for guests + logged-in users) ─
    function toggleWishlist(productId, btn) {
        return postForm('/Wishlist/Toggle', { productId }).then(res => {
            if (!res.ok) { toast(res.message || 'Could not update wishlist', 'danger'); return; }
            setBadge('wishlistBadge', res.count);
            document.querySelectorAll('.te-wishlist-btn[data-product-id="' + productId + '"]').forEach(b => {
                const icon = b.querySelector('i');
                if (b.classList.contains('te-wishlist-remove')) return;
                b.classList.toggle('is-active', res.added);
                b.setAttribute('aria-pressed', res.added ? 'true' : 'false');
                if (icon) icon.className = (res.added ? 'fas' : 'far') + ' fa-heart';
                if (res.added) bumpHeartIfAny(b);
            });
            toast(res.message);
            const canvas = document.getElementById('wishlistCanvas');
            if (canvas && canvas.classList.contains('show')) loadWishlistSidebar();
        }).catch(() => toast('Something went wrong.', 'danger'));
    }
    window.teToggleWishlist = toggleWishlist;

    // ── Wishlist sidebar ─────────────────────────────────────
    function loadWishlistSidebar() {
        const body = document.getElementById('wishlistCanvasBody');
        if (!body) return;
        fetch('/Wishlist/Sidebar', { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(r => r.text())
            .then(html => { body.innerHTML = html; })
            .catch(() => { body.innerHTML = '<p class="text-center text-muted py-4">Could not load your wishlist.</p>'; });
    }

    // ── Cart sidebar ─────────────────────────────────────────
    function loadCartSidebar() {
        const body = document.getElementById('cartCanvasBody');
        if (!body) return;
        body.innerHTML = '<div class="te-skeleton-list">' +
            '<div class="te-skeleton-item"><div class="te-skeleton-thumb"></div><div class="te-skeleton-content"><div class="te-skeleton-title"></div><div class="te-skeleton-text"></div></div></div>' +
            '<div class="te-skeleton-item"><div class="te-skeleton-thumb"></div><div class="te-skeleton-content"><div class="te-skeleton-title"></div><div class="te-skeleton-text"></div></div></div>' +
            '<div class="te-skeleton-item"><div class="te-skeleton-thumb"></div><div class="te-skeleton-content"><div class="te-skeleton-title"></div><div class="te-skeleton-text"></div></div></div>' +
            '</div>';
        fetch('/Cart/Sidebar', { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(r => r.text())
            .then(html => {
                body.innerHTML = html;
                initDrawerCart();
                const sp = document.getElementById('drawerShipProgress');
                if (sp) updateShipProgress(parseFloat(sp.dataset.subtotal || '0'), 'drawerShipProgress');
            })
            .catch(() => { body.innerHTML = '<p class="text-center text-muted py-4">Could not load your bag.</p>'; });
    }

    function initDrawerCart() {
        document.querySelectorAll('#cartCanvasBody .te-cart-line').forEach(row => {
            const id = row.dataset.itemId;
            const input = row.querySelector('.te-cart-qty-input');
            row.querySelector('.te-cart-inc')?.addEventListener('click', () => {
                const max = parseInt(input.max, 10) || 99;
                const v = Math.min(max, (parseInt(input.value, 10) || 1) + 1);
                input.value = v;
                postForm('/Cart/Update', { cartItemId: id, quantity: v }).then(res => { if (res.ok) applyDrawerTotals(res); });
            });
            row.querySelector('.te-cart-dec')?.addEventListener('click', () => {
                const v = Math.max(1, (parseInt(input.value, 10) || 1) - 1);
                input.value = v;
                postForm('/Cart/Update', { cartItemId: id, quantity: v }).then(res => { if (res.ok) applyDrawerTotals(res); });
            });
            row.querySelector('.te-cart-remove')?.addEventListener('click', () => {
                postForm('/Cart/Remove', { cartItemId: id }).then(res => {
                    if (res.ok) {
                        row.style.opacity = '0'; row.style.transform = 'translateX(20px)';
                        setTimeout(() => { row.remove(); if (res.isEmpty) loadCartSidebar(); else applyDrawerTotals(res); }, 250);
                    }
                });
            });
        });
    }

    function applyDrawerTotals(res) {
        if (res.isEmpty) { loadCartSidebar(); return; }
        setBadge('cartBadge', res.count);
        const sub = document.getElementById('drawerSubtotal');
        if (sub) sub.textContent = res.subTotal.toFixed(2);
        updateShipProgress(res.subTotal, 'drawerShipProgress');
        if (res.lines) {
            Object.keys(res.lines).forEach(id => {
                const row = document.querySelector('#cartCanvasBody .te-cart-line[data-item-id="' + id + '"]');
                if (!row) return;
                const lt = row.querySelector('.te-line-total'); if (lt) lt.textContent = res.lines[id].lineTotal.toFixed(2);
                const qi = row.querySelector('.te-cart-qty-input'); if (qi) qi.value = res.lines[id].quantity;
            });
        }
    }

    // ── Notifications drawer ──────────────────────────────────
    function loadNotifications() {
        if (window.TE && window.TE.isAdmin) return; // admin panel uses its own notification loader
        const body = document.getElementById('notifCanvasBody');
        if (!body) return;
        body.innerHTML = '<div class="te-skeleton-list">' +
            '<div class="te-skeleton-item"><div class="te-skeleton-thumb" style="border-radius:50%;width:36px;height:36px;"></div><div class="te-skeleton-content"><div class="te-skeleton-title" style="width:50%;"></div><div class="te-skeleton-text" style="width:80%;"></div></div></div>' +
            '<div class="te-skeleton-item"><div class="te-skeleton-thumb" style="border-radius:50%;width:36px;height:36px;"></div><div class="te-skeleton-content"><div class="te-skeleton-title" style="width:40%;"></div><div class="te-skeleton-text" style="width:70%;"></div></div></div>' +
            '<div class="te-skeleton-item"><div class="te-skeleton-thumb" style="border-radius:50%;width:36px;height:36px;"></div><div class="te-skeleton-content"><div class="te-skeleton-title" style="width:60%;"></div><div class="te-skeleton-text" style="width:75%;"></div></div></div>' +
            '</div>';
        getJson('/Notification/Recent').then(d => {
            setBadge('notifBadge', d.unread);
            if (!d.items || d.items.length === 0) {
                body.innerHTML = '<div class="te-empty-state py-5"><i class="far fa-bell te-empty-icon"></i><h3 style="font-size:1.1rem;">No notifications yet</h3><p>You\'ll see order updates and news here.</p></div>';
                return;
            }
            const items = d.items.map(n => {
                const cls = n.isRead ? '' : 'is-unread';
                const inner = '<div class="te-notif-page-icon"><i class="fas fa-' + iconFor(n.type) + '"></i></div>' +
                    '<div class="flex-grow-1"><strong>' + esc(n.title) + '</strong>' +
                    '<p>' + esc(n.message) + '</p><small>' + esc(n.timeAgo) + '</small></div>';
                return n.url
                    ? '<a class="te-notif-page-item ' + cls + ' text-decoration-none" href="' + n.url + '">' + inner + '</a>'
                    : '<div class="te-notif-page-item ' + cls + '">' + inner + '</div>';
            }).join('');
            body.innerHTML = '<div class="te-notif-page-list p-3">' + items + '</div>';
        }).catch(() => { body.innerHTML = '<p class="text-center text-muted py-3">Could not load notifications.</p>'; });
    }

    function iconFor(type) {
        switch (type) { case 'OrderUpdate': return 'box'; case 'CustomOrder': return 'palette'; case 'Promotion': return 'tag'; default: return 'bell'; }
    }
    function esc(s) { const d = document.createElement('div'); d.textContent = s || ''; return d.innerHTML; }

    // ── Star rating widget ───────────────────────────────────
    function initStarRating(containerId, hiddenId) {
        const container = document.getElementById(containerId);
        const hidden = document.getElementById(hiddenId);
        if (!container || !hidden) return;
        const stars = Array.from(container.querySelectorAll('.te-star'));
        function paint(val) { stars.forEach((s, i) => { s.querySelector('i').className = (i < val ? 'fas' : 'far') + ' fa-star'; }); }
        stars.forEach((s, i) => {
            s.addEventListener('click', () => { hidden.value = i + 1; paint(i + 1); });
            s.addEventListener('mouseenter', () => paint(i + 1));
        });
        container.addEventListener('mouseleave', () => paint(parseInt(hidden.value, 10) || 0));
        paint(parseInt(hidden.value, 10) || 0);
    }
    window.teInitStarRating = initStarRating;

    // ── Cart page (full page) ────────────────────────────────
    function initCartPage() {
        function applyTotals(res) {
            if (res.isEmpty) { window.location.reload(); return; }
            setBadge('cartBadge', res.count);
            const sub = document.getElementById('sumSubtotal'); if (sub) sub.textContent = res.subTotal.toFixed(2);
            const ship = document.getElementById('sumShipping'); if (ship) ship.textContent = res.shipping <= 0 ? 'Free' : res.shipping.toFixed(2) + ' JOD';
            const tot = document.getElementById('sumTotal'); if (tot) tot.textContent = res.total.toFixed(2);
            updateShipProgress(res.subTotal, 'shipProgress');
            if (res.lines) {
                Object.keys(res.lines).forEach(id => {
                    const row = document.querySelector('.te-cart-line[data-item-id="' + id + '"]');
                    if (!row) return;
                    const lt = row.querySelector('.te-line-total'); if (lt) lt.textContent = res.lines[id].lineTotal.toFixed(2);
                    const qi = row.querySelector('.te-cart-qty-input'); if (qi) qi.value = res.lines[id].quantity;
                });
            }
        }
        function update(itemId, qty) { postForm('/Cart/Update', { cartItemId: itemId, quantity: qty }).then(res => { if (res.ok) applyTotals(res); }); }

        document.querySelectorAll('.te-cart-line').forEach(row => {
            const id = row.dataset.itemId;
            const input = row.querySelector('.te-cart-qty-input');
            row.querySelector('.te-cart-inc')?.addEventListener('click', () => {
                const max = parseInt(input.max, 10) || 99; const v = Math.min(max, (parseInt(input.value, 10) || 1) + 1); input.value = v; update(id, v);
            });
            row.querySelector('.te-cart-dec')?.addEventListener('click', () => {
                const v = Math.max(1, (parseInt(input.value, 10) || 1) - 1); input.value = v; update(id, v);
            });
            row.querySelector('.te-cart-remove')?.addEventListener('click', () => {
                postForm('/Cart/Remove', { cartItemId: id }).then(res => {
                    if (res.ok) {
                        row.style.opacity = '0'; row.style.transform = 'translateX(20px)';
                        setTimeout(() => { row.remove(); if (res.isEmpty) window.location.reload(); else applyTotals(res); }, 250);
                    }
                });
            });
        });
        updateShipProgress(parseFloat(document.getElementById('shipProgress')?.dataset.subtotal || '0'), 'shipProgress');
    }
    window.teInitCartPage = initCartPage;

    function updateShipProgress(subtotal, progressId) {
        const el = document.getElementById(progressId || 'shipProgress');
        if (!el) return;
        const threshold = parseFloat(el.dataset.threshold || '50');
        const pct = Math.min(100, (subtotal / threshold) * 100);
        const bar = el.querySelector('.te-ship-progress-bar span');
        const text = el.querySelector('.te-ship-progress-text');
        if (bar) bar.style.width = pct + '%';
        if (text) {
            if (subtotal >= threshold) text.innerHTML = '<i class="fas fa-check-circle"></i> You\'ve unlocked free shipping! 🎉';
            else text.innerHTML = 'Add <strong>' + (threshold - subtotal).toFixed(2) + ' JOD</strong> more for free shipping';
        }
    }

    // ── Custom order wizard ──────────────────────────────────
    function initCustomWizard() {
        const form = document.getElementById('customForm');
        if (!form) return;
        const panels = Array.from(form.querySelectorAll('.te-step-panel'));
        const steps = Array.from(document.querySelectorAll('#customSteps .te-step'));
        let current = 1;

        function show(step) {
            // Apply horizontal sliding direction class
            if (step > current) {
                form.classList.remove('slide-backward');
                form.classList.add('slide-forward');
            } else if (step < current) {
                form.classList.remove('slide-forward');
                form.classList.add('slide-backward');
            }
            current = step;

            panels.forEach(p => p.classList.toggle('is-active', parseInt(p.dataset.panel, 10) === step));
            steps.forEach(s => {
                const n = parseInt(s.dataset.step, 10);
                s.classList.toggle('is-active', n === step);
                s.classList.toggle('is-done', n < step);

                // Dynamically replace step numbers with checkmark icons for finished steps
                const dot = s.querySelector('.te-step-dot');
                if (dot) {
                    if (n < step) {
                        dot.innerHTML = '<i class="fas fa-check" style="font-size:0.75rem;"></i>';
                    } else {
                        dot.innerHTML = '0' + n;
                    }
                }
            });

            // Animate dynamic progress fill line
            const fill = document.getElementById('customStepsProgress');
            if (fill) {
                const pct = ((step - 1) / (panels.length - 1)) * 100;
                fill.style.width = pct + '%';
            }

            // Scroll card into view smoothly
            window.scrollTo({ top: form.getBoundingClientRect().top + window.scrollY - 150, behavior: 'smooth' });
        }

        function validateStep(step) {
            const panel = panels.find(p => parseInt(p.dataset.panel, 10) === step);
            let ok = true;
            panel.querySelectorAll('input[required], textarea[required], [data-required="true"]').forEach(f => {
                if (!f.value || !f.value.trim()) { f.classList.add('is-invalid'); ok = false; } else { f.classList.remove('is-invalid'); }
            });
            if (step === 1) {
                const title = form.querySelector('[name="Title"]'); const desc = form.querySelector('[name="Description"]');
                [title, desc].forEach(f => { if (f && !f.value.trim()) { f.classList.add('is-invalid'); ok = false; } });
                if (!ok) toast('Please fill in the title and your vision first.', 'info');
            }
            if (step === 4) {
                const input = document.getElementById('imageInput');
                if (!input || !input.files || input.files.length === 0) {
                    toast('Please upload at least one reference image so we can understand your idea.', 'info');
                    ok = false;
                }
            }
            return ok;
        }

        form.querySelectorAll('.te-step-next').forEach(b => b.addEventListener('click', () => { if (validateStep(current)) show(Math.min(panels.length, current + 1)); }));
        form.querySelectorAll('.te-step-prev').forEach(b => b.addEventListener('click', () => show(Math.max(1, current - 1))));
        steps.forEach(s => s.addEventListener('click', () => { const n = parseInt(s.dataset.step, 10); if (n < current) show(n); }));

        // Auto-expanding textarea
        const descTextarea = form.querySelector('[name="Description"]');
        if (descTextarea) {
            descTextarea.addEventListener('input', function () {
                this.style.height = 'auto';
                this.style.height = (this.scrollHeight + 2) + 'px';
            });
        }

        // Intercept form submission to require login
        form.addEventListener('submit', function (e) {
            if (!validateStep(4)) {
                e.preventDefault();
                return;
            }

            if (!window.TE.isAuthenticated) {
                e.preventDefault();

                // Trigger login notification toast
                toast('Please sign in or create an account before submitting your custom order.', 'info');

                // Trigger authentication modal
                setTimeout(function() {
                    window.teOpenAuthModal('login', '#stay', 'Please sign in or create an account before submitting your custom order.');
                }, 300);
                return;
            }

            // Elegant submit button loading state
            const submitBtn = document.getElementById('customSubmit');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Sending request...';
            }
        });

        window.addEventListener('teAuthSuccess', function() {
            toast('You are now signed in. Click "Send My Request" again to submit your custom order.', 'success');
        });

        function saveFormDataToLocal() {
            var data = {};
            data.Title = form.querySelector('[name="Title"]').value;
            var cat = form.querySelector('[name="CategoryId"]');
            data.CategoryId = cat ? cat.value : "";
            data.Description = form.querySelector('[name="Description"]').value;
            var size = form.querySelector('[name="PreferredSize"]');
            data.PreferredSize = size ? size.value : "";
            
            var colors = [];
            form.querySelectorAll('input[name="ColorIds"]:checked').forEach(cb => {
                colors.push(cb.value);
            });
            data.ColorIds = colors;
            
            var budget = form.querySelector('[name="Budget"]');
            data.Budget = budget ? budget.value : "";
            var deadline = form.querySelector('[name="Deadline"]');
            data.Deadline = deadline ? deadline.value : "";

            const input = document.getElementById('imageInput');
            data.hasFiles = input && input.files && input.files.length > 0;

            localStorage.setItem('te_custom_order_draft', JSON.stringify(data));
        }

        function restoreFormDataFromLocal() {
            var saved = localStorage.getItem('te_custom_order_draft');
            if (!saved) return;
            try {
                var data = JSON.parse(saved);
                if (data.Title) form.querySelector('[name="Title"]').value = data.Title;
                if (data.CategoryId) form.querySelector('[name="CategoryId"]').value = data.CategoryId;
                if (data.Description) form.querySelector('[name="Description"]').value = data.Description;
                if (data.PreferredSize) form.querySelector('[name="PreferredSize"]').value = data.PreferredSize;
                if (data.ColorIds && data.ColorIds.length > 0) {
                    form.querySelectorAll('input[name="ColorIds"]').forEach(cb => {
                        cb.checked = data.ColorIds.includes(cb.value);
                    });
                }
                if (data.Budget) form.querySelector('[name="Budget"]').value = data.Budget;
                if (data.Deadline) form.querySelector('[name="Deadline"]').value = data.Deadline;

                localStorage.removeItem('te_custom_order_draft');

                // Shift view directly to form
                setTimeout(function () {
                    form.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }, 100);

                if (data.hasFiles) {
                    show(4);
                    toast('Your custom details have been restored. Please re-select your inspiration photos and click Send to submit!', 'info');
                } else {
                    show(4);
                    toast('Your custom details have been restored. Click Send to submit.', 'success');
                }
            } catch (ex) {
                console.error('Failed to restore custom order draft:', ex);
            }
        }

        // Onload auto-restore
        if (window.location.search.indexOf('restore=true') > -1) {
            restoreFormDataFromLocal();
        } else {
            show(1);
        }

        // Luxury Drag and Drop + Interactive File Previews
        const input = document.getElementById('imageInput');
        const previews = document.getElementById('uploadPreviews');
        const zone = document.getElementById('uploadZone');
        let selectedFiles = [];

        function updatePreviews() {
            previews.innerHTML = '';
            selectedFiles.forEach((file, index) => {
                const reader = new FileReader();
                reader.onload = e => {
                    const card = document.createElement('div');
                    card.className = 'te-upload-preview-card';
                    card.innerHTML = `
                        <img src="${e.target.result}" alt="${escapeHtml(file.name)}">
                        <div class="te-preview-info">
                            <span class="te-preview-name">${escapeHtml(file.name)}</span>
                            <span class="te-preview-size">${(file.size / 1024 / 1024).toFixed(2)} MB</span>
                        </div>
                        <button type="button" class="te-preview-remove" data-index="${index}" aria-label="Remove image">&times;</button>
                    `;
                    previews.appendChild(card);

                    card.querySelector('.te-preview-remove').addEventListener('click', () => {
                        selectedFiles.splice(index, 1);
                        syncFiles();
                        updatePreviews();
                    });
                };
                reader.readAsDataURL(file);
            });
        }

        function syncFiles() {
            const dt = new DataTransfer();
            selectedFiles.forEach(file => dt.items.add(file));
            input.files = dt.files;
        }

        function escapeHtml(str) {
            return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
        }

        if (input && previews && zone) {
            // Drag states
            ['dragenter', 'dragover'].forEach(eventName => {
                zone.addEventListener(eventName, (e) => {
                    e.preventDefault();
                    zone.classList.add('is-dragover');
                }, false);
            });

            ['dragleave', 'drop'].forEach(eventName => {
                zone.addEventListener(eventName, (e) => {
                    e.preventDefault();
                    zone.classList.remove('is-dragover');
                }, false);
            });

            // Handle drop
            zone.addEventListener('drop', (e) => {
                const dt = e.dataTransfer;
                const files = Array.from(dt.files).filter(f => f.type.startsWith('image/'));
                if (files.length > 0) {
                    selectedFiles = [...selectedFiles, ...files].slice(0, 5);
                    syncFiles();
                    updatePreviews();
                }
            });

            // Handle file input selection
            input.addEventListener('change', () => {
                const files = Array.from(input.files);
                selectedFiles = [...selectedFiles, ...files].slice(0, 5);
                syncFiles();
                updatePreviews();
            });
    }
    }
    window.teInitCustomWizard = initCustomWizard;

    // ── DOM ready ────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {

        // ── Announce bar + Navbar independent scroll behaviour ─
        const announceBar = document.getElementById('announceBar');
        const siteHeader  = document.getElementById('siteHeader');
        const nav         = document.getElementById('mainNav');
        const ANNOUNCE_H  = 34; // matches --te-announce-h in CSS

        if (siteHeader) {
            let lastScrollY    = window.scrollY;
            let announceGone   = false;
            let ticking        = false;

            window.addEventListener('scroll', () => {
                if (ticking) return;
                ticking = true;
                requestAnimationFrame(() => {
                    const scrollY = window.scrollY;

                    // 1. Announce bar: hide after scrolling past its height
                    if (scrollY > ANNOUNCE_H && !announceGone) {
                        announceBar?.classList.add('is-hidden');
                        siteHeader.classList.add('announce-gone');
                        announceGone = true;
                    } else if (scrollY <= 4 && announceGone) {
                        announceBar?.classList.remove('is-hidden');
                        siteHeader.classList.remove('announce-gone');
                        announceGone = false;
                    }

                    // 2. Navbar shadow
                    if (nav) nav.classList.toggle('scrolled', scrollY > 40);

                    // 3. Navbar hide/show on scroll direction (only after announce bar gone)
                    if (announceGone) {
                        if (scrollY > lastScrollY + 6) {
                            siteHeader.classList.add('nav-hidden');
                        } else if (scrollY < lastScrollY - 6) {
                            siteHeader.classList.remove('nav-hidden');
                        }
                    } else {
                        siteHeader.classList.remove('nav-hidden');
                    }

                    lastScrollY = scrollY;
                    ticking = false;
                });
            }, { passive: true });
        }

        // ── Back to top ──────────────────────────────────────
        const btt = document.getElementById('backToTop');
        if (btt) {
            window.addEventListener('scroll', () => {
                btt.classList.toggle('is-visible', window.scrollY > 300);
            }, { passive: true });
            btt.addEventListener('click', () => {
                window.scrollTo({ top: 0, behavior: 'smooth' });
            });
        }

        // ── Gentle page fade in ──────────────────────────────
        document.body.style.opacity = '0';
        document.body.style.transition = 'opacity .4s ease';
        requestAnimationFrame(() => { document.body.style.opacity = '1'; });

        // ── Scroll reveal ────────────────────────────────────
        const revealObs = new IntersectionObserver((entries) => {
            entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('te-visible'); revealObs.unobserve(e.target); } });
        }, { rootMargin: '0px 0px -50px 0px', threshold: 0.08 });
        document.querySelectorAll('.te-observe').forEach(el => revealObs.observe(el));

        // ── Active nav link ──────────────────────────────────
        const path = window.location.pathname.toLowerCase();
        document.querySelectorAll('.te-navbar .nav-link').forEach(a => {
            const href = (a.getAttribute('href') || '').toLowerCase();
            if (href && href !== '#' && href !== '/' && path.startsWith(href)) a.classList.add('active');
            if (href === '/' && path === '/') a.classList.add('active');
        });

        // ── Auto-show TempData toast ─────────────────────────
        document.querySelectorAll('.te-toast[data-autoshow="true"]').forEach(t => {
            requestAnimationFrame(() => t.classList.add('is-visible'));
            const close = () => { t.classList.remove('is-visible'); setTimeout(() => t.remove(), 350); };
            t.querySelector('.te-toast-close')?.addEventListener('click', close);
            setTimeout(close, 4000);
        });

        // ── Delegated: add to cart + wishlist ────────────────
        document.addEventListener('click', function (e) {
            const addBtn = e.target.closest('.te-add-cart-btn');
            if (addBtn && !addBtn.closest('#addToCartForm')) {
                e.preventDefault();
                addToCart(addBtn.dataset.productId, null, 1, addBtn);
                return;
            }
            const wishBtn = e.target.closest('.te-wishlist-btn');
            if (wishBtn) { e.preventDefault(); toggleWishlist(wishBtn.dataset.productId, wishBtn); return; }
        });

        // ── Wishlist offcanvas ───────────────────────────────
        const wishlistCanvas = document.getElementById('wishlistCanvas');
        if (wishlistCanvas) wishlistCanvas.addEventListener('show.bs.offcanvas', loadWishlistSidebar);

        // ── Cart offcanvas ───────────────────────────────────
        const cartCanvas = document.getElementById('cartCanvas');
        if (cartCanvas) cartCanvas.addEventListener('show.bs.offcanvas', loadCartSidebar);

        // ── Notifications offcanvas ──────────────────────────
        if (!window.TE?.isAdmin) {
            const notifCanvas = document.getElementById('notifCanvas');
            if (notifCanvas) {
                notifCanvas.addEventListener('show.bs.offcanvas', loadNotifications);
                document.getElementById('notifReadAll')?.addEventListener('click', function (e) {
                    e.preventDefault();
                    postForm('/Notification/MarkAllRead').then(() => { setBadge('notifBadge', 0); loadNotifications(); });
                });
            }
        }

        // ── Testimonial modal (auth required) ───────────────
        const openTestimonialBtn = document.getElementById('openTestimonialModalBtn');
        if (openTestimonialBtn) {
            openTestimonialBtn.addEventListener('click', () => {
                const modalEl = document.getElementById('testimonialModal');
                if (modalEl && window.bootstrap) new bootstrap.Modal(modalEl).show();
            });
        }
        initStarRating('testimonialStars', 'testimonialRating');
        
        const tForm = document.getElementById('testimonialForm');
        if (tForm) {
            // Restore draft
            const draft = localStorage.getItem('te_testimonial_draft');
            if (draft) {
                try {
                    const data = JSON.parse(draft);
                    document.getElementById('testimonialRating').value = data.rating || 5;
                    document.getElementById('testimonialMessage').value = data.message || '';
                    initStarRating('testimonialStars', 'testimonialRating');
                } catch(e) {}
                if (TE.isAuthenticated && window.location.search.indexOf('restoreTestimonial=true') > -1) {
                    const modalEl = document.getElementById('testimonialModal');
                    if (modalEl && window.bootstrap) new bootstrap.Modal(modalEl).show();
                }
            }
            
            tForm.addEventListener('input', () => {
                const data = {
                    rating: document.getElementById('testimonialRating').value,
                    message: document.getElementById('testimonialMessage').value
                };
                localStorage.setItem('te_testimonial_draft', JSON.stringify(data));
            });

            // Also save draft when clicking stars
            const starsContainer = document.getElementById('testimonialStars');
            if (starsContainer) {
                starsContainer.addEventListener('click', () => {
                    const data = {
                        rating: document.getElementById('testimonialRating').value,
                        message: document.getElementById('testimonialMessage').value
                    };
                    localStorage.setItem('te_testimonial_draft', JSON.stringify(data));
                });
            }

            tForm.addEventListener('submit', function (e) {
                e.preventDefault();
                
                if (!TE.isAuthenticated) {
                    if (typeof window.teOpenAuthModal === 'function') {
                        window.teOpenAuthModal('login', window.location.pathname + '?restoreTestimonial=true', 'Sign in to submit your review.');
                        const modalEl = document.getElementById('testimonialModal');
                        if (modalEl && window.bootstrap) bootstrap.Modal.getInstance(modalEl)?.hide();
                    } else {
                        toast('Sign in to submit your review.', 'info');
                    }
                    return;
                }
                
                const msgBox = document.getElementById('testimonialFormMsg');
                const submitBtn = document.getElementById('testimonialSubmitBtn');
                const submitIcon = document.getElementById('testimonialSubmitIcon');
                const submitSpinner = document.getElementById('testimonialSubmitSpinner');
                const submitText = document.getElementById('testimonialSubmitText');
                
                // client-side validation
                const imgInput = document.getElementById('testimonialImage');
                if (imgInput && imgInput.files.length > 0) {
                    const file = imgInput.files[0];
                    if (file.size > 5 * 1024 * 1024) {
                        msgBox.className = 'te-form-msg is-error';
                        msgBox.textContent = "Image size cannot exceed 5MB.";
                        return;
                    }
                }
                
                if (submitBtn) submitBtn.disabled = true;
                if (submitIcon) submitIcon.classList.add('d-none');
                if (submitSpinner) submitSpinner.classList.remove('d-none');
                if (submitText) submitText.textContent = "Uploading...";

                const formData = new FormData(tForm);
                formData.append('__RequestVerificationToken', TE.token);

                fetch('/Testimonial/Create', {
                    method: 'POST',
                    body: formData,
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                })
                .then(r => r.json())
                .then(res => {
                    if (res.ok) {
                        msgBox.className = 'te-form-msg is-success';
                        msgBox.textContent = res.message;
                        tForm.reset(); 
                        initStarRating('testimonialStars', 'testimonialRating');
                        localStorage.removeItem('te_testimonial_draft');
                        
                        // Insert temporary card
                        if (res.testimonial && window.teInitTestimonialSwiper) {
                            const swiperWrapper = document.getElementById('testimonialSwiperWrapper');
                            const emptyState = document.getElementById('testimonialEmptyState');
                            const swiperContainer = document.getElementById('testimonialSwiperContainer');
                            
                            if (emptyState) emptyState.classList.add('d-none');
                            if (swiperContainer) swiperContainer.classList.remove('d-none');
                            
                            const avatarHtml = res.testimonial.imageUrl 
                                ? `<img src="${res.testimonial.imageUrl}" class="te-avatar" style="object-fit: cover; border: 2px solid var(--te-blush);" alt="${res.testimonial.authorName}" />`
                                : `<div class="te-avatar" style="background: linear-gradient(135deg, #fcd3d3 0%, #f7a8a8 100%); color: #fff;">${res.testimonial.initial}</div>`;
                                
                            const starsHtml = '★'.repeat(res.testimonial.rating) + '☆'.repeat(5 - res.testimonial.rating);
                                
                            const slideHtml = `
                            <div class="swiper-slide">
                                <div class="te-testimonial-card te-tcard-slide" style="border: 2px dashed var(--te-blush);">
                                    <div class="te-badge te-badge-warning" style="position:absolute; top: 10px; right: 10px; font-size: 0.7rem;">Pending Approval</div>
                                    <div class="te-tcard-quote">❝</div>
                                    <div class="te-stars mb-3">${starsHtml}</div>
                                    <p class="te-testimonial-text">"${res.testimonial.message}"</p>
                                    <div class="te-tcard-author">
                                        ${avatarHtml}
                                        <div>
                                            <div class="te-tcard-name">${res.testimonial.authorName}</div>
                                            <div class="te-tcard-label"><i class="fas fa-check-circle me-1"></i>Verified Buyer</div>
                                        </div>
                                    </div>
                                </div>
                            </div>`;
                            
                            if (swiperWrapper) {
                                swiperWrapper.insertAdjacentHTML('afterbegin', slideHtml);
                                window.teInitTestimonialSwiper();
                            }
                        }
                        
                        setTimeout(() => {
                            const modalEl = document.getElementById('testimonialModal');
                            if (modalEl && window.bootstrap) bootstrap.Modal.getInstance(modalEl)?.hide();
                            toast('Thank you for sharing your experience. Your review is pending approval.', 'success');
                            msgBox.className = 'te-form-msg';
                            msgBox.textContent = '';
                        }, 2000);
                    } else {
                        msgBox.className = 'te-form-msg is-error';
                        msgBox.textContent = res.message;
                    }
                })
                .catch(() => {
                    msgBox.className = 'te-form-msg is-error';
                    msgBox.textContent = 'Something went wrong. Please try again.';
                })
                .finally(() => {
                    if (submitBtn) submitBtn.disabled = false;
                    if (submitIcon) submitIcon.classList.remove('d-none');
                    if (submitSpinner) submitSpinner.classList.add('d-none');
                    if (submitText) submitText.textContent = "Send My Note";
                });
            });
        }

        // ── Initial badges ───────────────────────────────────
        refreshCartBadge();
        refreshWishlistBadge();
        refreshNotifBadge();
    });
})();
