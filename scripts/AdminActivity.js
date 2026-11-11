var AdminActivityJs = (function () {

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
        $ApiRequest('AdminActivity/' + method, JSON.stringify(list));
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
        var t = el('lo-toast');
        t.textContent = msg;
        t.classList.add('lo-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('lo-open');
        }, 3400);
    }

    function openMenu() {
        el('lo-rail').classList.add('lo-open');
        el('lo-scrim').classList.add('lo-open');
    }

    function closeMenu() {
        el('lo-rail').classList.remove('lo-open');
        el('lo-scrim').classList.remove('lo-open');
    }

    function openModal(title) {
        var m = el('lo-modal');
        el('lo-mtitle').textContent = title || '';
        dirty = false;
        m.classList.add('lo-open');
        m.setAttribute('aria-hidden', 'false');
        m.scrollTop = 0;
    }

    function shut() {
        var m = el('lo-modal');
        if (m) {
            m.classList.remove('lo-open');
            m.setAttribute('aria-hidden', 'true');
        }
        dirty = false;
    }

    function closeModal() {
        var m = el('lo-modal');
        if (!m || !m.classList.contains('lo-open')) {
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
        if (!askBox || !askBox.classList.contains('lo-open')) {
            return;
        }
        var x = askParts(), f = askDone, v = x.i.value.trim();
        askBox.classList.remove('lo-open');
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
        askBox.className = 'lo-ask';
        askBox.innerHTML = '<div class="lo-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off"><em class="lo-err" role="alert"></em><div class="lo-askacts"><button type="button" class="lo-btn lo-btn-ghost"></button><button type="button" class="lo-btn"></button></div></div>';
        document.getElementById('lo-site-root').appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () { askClose(false); });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () { x.e.textContent = ''; });
        askBox.addEventListener('mousedown', function (e) { if (e.target === askBox) { askClose(false); } });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('lo-open')) {
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
        x.b[1].className = 'lo-btn' + (o.danger ? ' lo-askdel' : '');
        askBox.firstChild.classList.toggle('lo-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        askBox.classList.add('lo-open');
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



    var pg = 1;
    var mtimer = null;

    function extra() {
        return [{ key: 'res', vlu: val('lo-fr') }];
    }

    function filters() {
        return [{ key: 'term', vlu: val('lo-q') }, { key: 'pg', vlu: String(pg) }].concat(extra());
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

    function reveal() {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                if (askBox && askBox.classList.contains('lo-open')) {
                    return;
                }
                closeMenu();
                closeModal();
            }
        });
        var m = el('lo-modal');
        if (m) {
            m.addEventListener('input', markDirty);
            m.addEventListener('change', markDirty);
            m.addEventListener('mousedown', function (e) {
                if (e.target === m) {
                    closeModal();
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
        list: list,
        type: type,
        go: go
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminActivityJs.reveal);
} else {
    AdminActivityJs.reveal();
}

(function () {
    var P = 'lo', PAGE = 'AdminActivity', K = 'hv_shell';
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
