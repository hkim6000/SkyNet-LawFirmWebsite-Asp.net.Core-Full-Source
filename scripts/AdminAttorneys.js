var AdminAttorneysJs = (function () {

    var ttimer = null;
    var askBox = null, askDone = null, askCheck = null, askBack = null;
    var dirty = false;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function base() {
        return location.pathname.replace(/[^\/]*$/, '');
    }

    function getCookie(name) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + name + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function api(method, list) {
        list = list || [];
        list.push({ key: 'tok', vlu: getCookie('hv_tok') });
        $ApiRequest('AdminAttorneys/' + method, JSON.stringify(list));
    }

    function expired() {
        document.cookie = 'hv_tok=; path=' + base() + '; max-age=0';
        location.href = 'AdminLogin';
    }

    function signout() {
        var t = getCookie('hv_tok');
        document.cookie = 'hv_tok=; path=' + base() + '; max-age=0';
        setTimeout(function () {
            location.href = 'AdminLogin';
        }, 1500);
        $ApiRequest('AdminLogin/SignOut', JSON.stringify([{ key: 'tok', vlu: t }]));
    }

    function toast(msg) {
        var t = el('xa-toast');
        t.textContent = msg;
        t.classList.add('xa-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('xa-open');
        }, 3400);
    }

    function openMenu() {
        el('xa-rail').classList.add('xa-open');
        el('xa-scrim').classList.add('xa-open');
    }

    function closeMenu() {
        el('xa-rail').classList.remove('xa-open');
        el('xa-scrim').classList.remove('xa-open');
    }

    function openModal(title) {
        var m = el('xa-modal');
        el('xa-mtitle').textContent = title || '';
        dirty = false;
        m.classList.add('xa-open');
        m.setAttribute('aria-hidden', 'false');
        m.scrollTop = 0;
    }

    function shut() {
        var m = el('xa-modal');
        if (m) {
            m.classList.remove('xa-open');
            m.setAttribute('aria-hidden', 'true');
        }
        dirty = false;
    }

    function closeModal() {
        var m = el('xa-modal');
        if (!m || !m.classList.contains('xa-open')) {
            return;
        }
        if (dirty) {
            ask({ title: 'Leave without saving?', text: 'Your changes will be lost.', ok: 'Leave', no: 'Keep editing', danger: true }, shut);
            return;
        }
        shut();
    }

    function markDirty() {
        dirty = true;
    }

    function clean() {
        dirty = false;
    }

    function askParts() {
        return { h: askBox.querySelector('h2'), p: askBox.querySelector('p'), i: askBox.querySelector('input'), e: askBox.querySelector('em'), b: askBox.querySelectorAll('button') };
    }

    function askClose(ok) {
        if (!askBox || !askBox.classList.contains('xa-open')) {
            return;
        }
        var x = askParts(), f = askDone, v = x.i.value.trim();
        askBox.classList.remove('xa-open');
        askDone = null;
        askCheck = null;
        if (askBack && askBack.focus && document.body.contains(askBack)) {
            askBack.focus();
        }
        askBack = null;
        if (ok && f) {
            f(v);
        }
    }

    function askOk() {
        var x = askParts();
        var m = askCheck ? askCheck(x.i.value.trim()) : '';
        if (m) {
            x.e.textContent = m;
            x.i.focus();
            return;
        }
        askClose(true);
    }

    function askBuild() {
        askBox = document.createElement('div');
        askBox.className = 'xa-ask';
        askBox.innerHTML = '<div class="xa-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off"><em class="xa-err" role="alert"></em><div class="xa-askacts"><button type="button" class="xa-btn xa-btn-ghost"></button><button type="button" class="xa-btn"></button></div></div>';
        document.getElementById('xa-site-root').appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () { askClose(false); });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () { x.e.textContent = ''; });
        askBox.addEventListener('mousedown', function (e) { if (e.target === askBox) { askClose(false); } });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('xa-open')) {
                return;
            }
            if (e.key === 'Escape') {
                e.preventDefault();
                e.stopPropagation();
                askClose(false);
            } else if (e.key === 'Enter' && e.target !== x.b[0]) {
                e.preventDefault();
                e.stopPropagation();
                askOk();
            } else if (e.key === 'Tab') {
                var f = [x.i, x.b[0], x.b[1]].filter(function (n) { return n.offsetParent !== null; });
                var at = f.indexOf(document.activeElement);
                e.preventDefault();
                f[(at + (e.shiftKey ? f.length - 1 : 1)) % f.length].focus();
            }
        }, true);
    }

    function ask(o, done) {
        if (!askBox) {
            askBuild();
        }
        var x = askParts(), input = typeof o.value === 'string';
        askBack = document.activeElement;
        askDone = done;
        askCheck = o.check || null;
        x.h.textContent = o.title || '';
        x.p.textContent = o.text || '';
        x.e.textContent = '';
        x.b[0].textContent = o.no || 'Cancel';
        x.b[1].textContent = o.ok || 'OK';
        x.b[1].className = 'xa-btn' + (o.danger ? ' xa-askdel' : '');
        askBox.firstChild.classList.toggle('xa-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        askBox.classList.add('xa-open');
        if (input) {
            x.i.focus();
        } else {
            x.b[o.danger ? 0 : 1].focus();
        }
    }

    function collect(root, extra) {
        var list = extra || [];
        document.querySelectorAll('#' + root + ' [data-k]').forEach(function (n) {
            if (n.type === 'checkbox') {
                list.push({ key: n.getAttribute('data-k'), vlu: n.checked ? '1' : '0' });
            } else if (n.hasAttribute('data-src')) {
                list.push({ key: n.getAttribute('data-k'), vlu: n.getAttribute('data-src') || '' });
            } else if (n.type === 'password') {
                list.push({ key: n.getAttribute('data-k'), vlu: n.value });
            } else {
                list.push({ key: n.getAttribute('data-k'), vlu: (n.value || '').trim() });
            }
        });
        return list;
    }


    var pkDone = null;
    var pkTimer = null;

    function pkList() {
        api('MediaPick', [{ key: 'pkind', vlu: 'photo' }, { key: 'pterm', vlu: val('xa-pkq') }]);
    }

    function pkSearch() {
        clearTimeout(pkTimer);
        pkTimer = setTimeout(pkList, 300);
    }

    function pkOpen() {
        var d = el('xa-pk');
        return d && d.classList.contains('xa-open');
    }

    function pkClose() {
        var d = el('xa-pk');
        if (d) {
            d.classList.remove('xa-open');
            d.setAttribute('aria-hidden', 'true');
        }
        el('xa-pkfile').value = '';
        pkDone = null;
    }

    function pickMedia(done) {
        pkDone = done;
        el('xa-pkq').value = '';
        el('xa-pkmsg').textContent = '';
        var d = el('xa-pk');
        d.classList.add('xa-open');
        d.setAttribute('aria-hidden', 'false');
        pkList();
        setTimeout(function () {
            el('xa-pkq').focus();
        }, 30);
    }

    function pkPick(b) {
        var f = pkDone;
        var it = { src: b.getAttribute('data-src'), thumb: b.getAttribute('data-thumb') };
        pkClose();
        if (f) {
            f(it);
        }
    }

    function pkUpload() {
        el('xa-pkmsg').textContent = 'Uploading.';
        api('MediaUp');
    }

    function pkUploaded(ok, msg) {
        el('xa-pkfile').value = '';
        el('xa-pkmsg').textContent = msg;
        if (ok) {
            el('xa-pkq').value = '';
        }
    }

    function paintPic(box, it) {
        var b = box.querySelector('.xa-picb');
        box.setAttribute('data-src', it ? it.src : '');
        b.innerHTML = it ? '<img src="' + esc(it.thumb) + '" alt="">' : '<span>' + esc(box.getAttribute('data-ph') || 'Choose a picture') + '</span>';
        var x = box.querySelector('.xa-picx');
        if (x) {
            x.hidden = !it;
        }
    }

    function pic(btn) {
        var box = btn.closest('.xa-pic');
        pickMedia(function (it) {
            paintPic(box, it);
            markDirty();
        });
    }

    function picClear(btn) {
        paintPic(btn.closest('.xa-pic'), null);
        markDirty();
    }

    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }


    var pg = 1;
    var mtimer = null;

    function extra() {
        return [];
    }

    function filters() {
        return [{ key: 'term', vlu: val('xa-q') }, { key: 'pg', vlu: String(pg) }].concat(extra());
    }

    function list(reset) {
        if (reset) {
            pg = 1;
        }
        api('List', filters());
    }

    function type() {
        clearTimeout(mtimer);
        mtimer = setTimeout(function () {
            list(true);
        }, 250);
    }

    function go(n) {
        pg = n;
        list(false);
    }

    function opened(title) {
        openModal(title);
    }

    function saved() {
        clean();
        shut();
        toast('Saved');
    }

    function add() {
        api('Edit', [{ key: 'id', vlu: '' }]);
    }

    function edit(id) {
        api('Edit', [{ key: 'id', vlu: id }]);
    }

    function save() {
        var ids = [];
        document.querySelectorAll('#xa-form .xa-ar:checked').forEach(function (b) {
            ids.push(b.getAttribute('data-v'));
        });
        var list = collect('xa-form', [{ key: 'id', vlu: val('xa-f-id') }, { key: 'areas', vlu: ids.join(',') }]);
        api('Save', list.concat(filters()));
        return false;
    }

    function del(id, name) {
        ask({ title: 'Delete ' + name + '?', text: 'The attorney and the profile page are removed from the website. Articles they wrote stay, without an author.', ok: 'Delete attorney', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'id', vlu: id }].concat(filters()));
        });
    }

    function deleted() {
        clean();
        shut();
        toast('Attorney deleted');
    }

    function move(id, dir) {
        api('Move', [{ key: 'id', vlu: id }, { key: 'dir', vlu: String(dir) }].concat(filters()));
    }

    function reveal() {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                if (askBox && askBox.classList.contains('xa-open')) {
                    return;
                }
                if (pkOpen()) {
                    pkClose();
                    return;
                }
                closeMenu();
                closeModal();
            }
        });
        var m = el('xa-modal');
        if (m) {
            m.addEventListener('input', markDirty);
            m.addEventListener('change', markDirty);
            m.addEventListener('mousedown', function (e) {
                if (e.target === m) {
                    closeModal();
                }
            });
        }
        var d = el('xa-pk');
        if (d) {
            d.addEventListener('mousedown', function (e) {
                if (e.target === d) {
                    pkClose();
                }
            });
        }
        api('Load');
    }

    return {
        reveal: reveal,
        expired: expired,
        signout: signout,
        toast: toast,
        openMenu: openMenu,
        closeMenu: closeMenu,
        openModal: openModal,
        closeModal: closeModal,
        shut: shut,
        clean: clean,
        pic: pic,
        picClear: picClear,
        pkList: pkList,
        pkSearch: pkSearch,
        pkPick: pkPick,
        pkClose: pkClose,
        pkUpload: pkUpload,
        pkUploaded: pkUploaded,
        list: list,
        type: type,
        go: go,
        opened: opened,
        saved: saved,
        add: add,
        edit: edit,
        save: save,
        del: del,
        deleted: deleted,
        move: move
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminAttorneysJs.reveal);
} else {
    AdminAttorneysJs.reveal();
}

(function () {
    var P = 'xa', PAGE = 'AdminAttorneys', K = 'hv_shell';
    var s = null, done = { menu: false, who: false, brand: false };

    try {
        s = JSON.parse(sessionStorage.getItem(K) || 'null');
    } catch (e) {
        s = null;
    }

    function menu(m) {
        if (m.children.length || !Array.isArray(s.items)) {
            return;
        }
        s.items.forEach(function (it) {
            if (!it || !/^Admin[A-Za-z]*$/.test(it.h || '')) {
                return;
            }
            var a = document.createElement('a');
            a.className = P + '-mi' + (it.h === PAGE ? ' ' + P + '-on' : '');
            a.setAttribute('href', it.h);
            a.textContent = String(it.t || '');
            if (/^[0-9]{1,5}$/.test(String(it.b || ''))) {
                var i = document.createElement('i');
                i.className = P + '-badge';
                i.textContent = it.b;
                a.appendChild(i);
            }
            m.appendChild(a);
        });
    }

    function who(w) {
        if (w.children.length || !s.name) {
            return;
        }
        var b = document.createElement('b');
        b.textContent = String(s.name);
        var sm = document.createElement('small');
        sm.textContent = String(s.role || '');
        w.appendChild(b);
        w.appendChild(sm);
    }

    function paint() {
        var m = document.getElementById(P + '-menu');
        var w = document.getElementById(P + '-who');
        var br = document.getElementById(P + '-brand');
        if (!done.menu && m) {
            menu(m);
            done.menu = true;
        }
        if (!done.who && w) {
            who(w);
            done.who = true;
        }
        if (!done.brand && br && br.textContent === 'Staff') {
            if (s.brand) {
                br.textContent = String(s.brand);
            }
            done.brand = true;
        }
        return done.menu && done.who && done.brand;
    }

    function keep() {
        var m = document.getElementById(P + '-menu');
        var w = document.getElementById(P + '-who');
        var br = document.getElementById(P + '-brand');
        if (!m || !w || !m.children.length) {
            return;
        }
        var items = [];
        Array.prototype.forEach.call(m.querySelectorAll('a'), function (a) {
            var i = a.querySelector('i');
            items.push({ h: a.getAttribute('href') || '', t: a.firstChild ? a.firstChild.textContent : '', b: i ? i.textContent : '' });
        });
        var b = w.querySelector('b'), sm = w.querySelector('small');
        var brand = br ? br.textContent : '';
        try {
            sessionStorage.setItem(K, JSON.stringify({ items: items, name: b ? b.textContent : '', role: sm ? sm.textContent : '', brand: brand === 'Staff' ? '' : brand }));
        } catch (e) {
        }
    }

    window.addEventListener('pagehide', keep);

    if (s && !paint()) {
        var ob = new MutationObserver(function () {
            if (paint()) {
                ob.disconnect();
            }
        });
        ob.observe(document.documentElement, { childList: true, subtree: true });
        document.addEventListener('DOMContentLoaded', function () {
            paint();
            ob.disconnect();
        });
    }
})();
