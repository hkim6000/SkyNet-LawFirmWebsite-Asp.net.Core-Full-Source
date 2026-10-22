var AdminPagesJs = (function () {

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
        $ApiRequest('AdminPages/' + method, JSON.stringify(list));
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
        var t = el('pe-toast');
        t.textContent = msg;
        t.classList.add('pe-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('pe-open');
        }, 3400);
    }

    function openMenu() {
        el('pe-rail').classList.add('pe-open');
        el('pe-scrim').classList.add('pe-open');
    }

    function closeMenu() {
        el('pe-rail').classList.remove('pe-open');
        el('pe-scrim').classList.remove('pe-open');
    }

    function openModal(title) {
        var m = el('pe-modal');
        el('pe-mtitle').textContent = title || '';
        dirty = false;
        m.classList.add('pe-open');
        m.setAttribute('aria-hidden', 'false');
        m.scrollTop = 0;
    }

    function shut() {
        var m = el('pe-modal');
        if (m) {
            m.classList.remove('pe-open');
            m.setAttribute('aria-hidden', 'true');
        }
        dirty = false;
    }

    function closeModal() {
        var m = el('pe-modal');
        if (!m || !m.classList.contains('pe-open')) {
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
        if (!askBox || !askBox.classList.contains('pe-open')) {
            return;
        }
        var x = askParts(), f = askDone, v = x.i.value.trim();
        askBox.classList.remove('pe-open');
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
        askBox.className = 'pe-ask';
        askBox.innerHTML = '<div class="pe-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off"><em class="pe-err" role="alert"></em><div class="pe-askacts"><button type="button" class="pe-btn pe-btn-ghost"></button><button type="button" class="pe-btn"></button></div></div>';
        document.getElementById('pe-site-root').appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () { askClose(false); });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () { x.e.textContent = ''; });
        askBox.addEventListener('mousedown', function (e) { if (e.target === askBox) { askClose(false); } });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('pe-open')) {
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
        x.b[1].className = 'pe-btn' + (o.danger ? ' pe-askdel' : '');
        askBox.firstChild.classList.toggle('pe-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        askBox.classList.add('pe-open');
        if (input) {
            x.i.focus();
        } else {
            x.b[o.danger ? 0 : 1].focus();
        }
    }

    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    var page = 'home';
    var dirty = false;
    var drag = null;
    var startOrder = '';
    var mediaFor = null;
    var richAt = null;
    var range = null;
    var rtTimer = null;
    var pkAllow = '';
    var pkDone = null;
    var pkTimer = null;

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function loaded() {
        show(qs('pg') || 'home');
        window.addEventListener('beforeunload', function (e) {
            if (dirty) {
                e.preventDefault();
                e.returnValue = '';
            }
        });
    }

    function show(p) {
        api('Show', [{ key: 'page', vlu: p }]);
    }

    function tab(p) {
        if (dirty) {
            ask({ title: 'Leave without saving?', text: 'Your changes to this page will be lost.', ok: 'Leave', no: 'Stay', danger: true }, function () {
                dirty = false;
                show(p);
            });
            return;
        }
        show(p);
    }

    function mark() {
        var d = el('pe-dirty');
        if (d) {
            d.textContent = dirty ? 'Unsaved changes' : '';
        }
    }

    function touch() {
        dirty = true;
        mark();
    }

    function shown(p, href) {
        page = p;
        dirty = false;
        mark();
        var v = el('pe-viewlink');
        if (v) {
            v.setAttribute('data-href', href);
        }
        try {
            history.replaceState(null, '', 'AdminPages?pg=' + p);
        } catch (e) {
        }
        var c = el('pe-canvas');
        if (!c || c.getAttribute('data-bound')) {
            return;
        }
        c.setAttribute('data-bound', '1');
        c.addEventListener('input', touch);
        c.addEventListener('change', touch);
        c.addEventListener('paste', function (e) {
            var t = e.target.closest ? e.target.closest('[contenteditable]') : null;
            if (t) {
                e.preventDefault();
                var txt = (e.clipboardData || window.clipboardData).getData('text/plain');
                document.execCommand('insertText', false, t.getAttribute('data-ml') === '1' || t.classList.contains('pe-er') ? txt : txt.replace(/\s*\n\s*/g, ' '));
            }
        });
        c.addEventListener('keydown', function (e) {
            var t = e.target;
            if (e.key === 'Enter' && t.getAttribute && t.getAttribute('contenteditable') === 'plaintext-only' && t.getAttribute('data-ml') !== '1') {
                e.preventDefault();
            }
        });
        c.addEventListener('focusin', function (e) {
            if (e.target.classList && e.target.classList.contains('pe-er')) {
                richAt = e.target;
                clearTimeout(rtTimer);
                tools(true);
            }
        });
        window.addEventListener('scroll', function () {
            if (richAt && el('pe-rt').classList.contains('pe-open')) {
                tools(true);
            }
        }, { passive: true });
        c.addEventListener('focusout', function (e) {
            if (e.target.classList && e.target.classList.contains('pe-er')) {
                rtTimer = setTimeout(function () {
                    tools(false);
                }, 180);
            }
        });
    }

    function tools(on) {
        var t = el('pe-rt');
        if (!t) {
            return;
        }
        if (!on || !richAt) {
            t.classList.remove('pe-open');
            return;
        }
        var r = richAt.getBoundingClientRect();
        t.style.left = Math.max(8, Math.min(r.left, window.innerWidth - 260)) + 'px';
        t.style.top = Math.max(64, r.top - 42) + 'px';
        t.classList.add('pe-open');
    }

    function fmt(cmd) {
        if (!richAt) {
            return;
        }
        richAt.focus();
        if (cmd === 'link') {
            var sel = window.getSelection();
            range = sel.rangeCount ? sel.getRangeAt(0).cloneRange() : null;
            if (!range || range.collapsed) {
                toast('Select some words first, then add the link.');
                return;
            }
            var target = richAt;
            ask({ title: 'Add a link', text: 'A page name like Shop or Contact, or a full address starting with https://', value: 'https://', ok: 'Add link', check: function (v) {
                return /^(https?:\/\/\S+|mailto:\S+|tel:[0-9+() .-]+|#[A-Za-z0-9_-]+|[A-Za-z][A-Za-z0-9_-]*(\?[A-Za-z0-9=&%._+-]*)?(#[A-Za-z0-9_-]+)?)$/.test(v) ? '' : 'Use a page name or a full https:// address.';
            } }, function (v) {
                target.focus();
                var s2 = window.getSelection();
                s2.removeAllRanges();
                s2.addRange(range);
                document.execCommand('createLink', false, v);
                touch();
            });
            return;
        }
        if (cmd === 'clear') {
            document.execCommand('removeFormat');
            document.execCommand('unlink');
        } else {
            document.execCommand(cmd);
        }
        touch();
    }

    function blocks() {
        return el('pe-canvas').querySelectorAll(':scope > .pe-blk');
    }

    function order() {
        return [].map.call(blocks(), function (b) {
            return b.getAttribute('data-id');
        }).join(',');
    }

    function grab(e, btn) {
        e.preventDefault();
        drag = btn.closest('.pe-blk');
        startOrder = order();
        drag.classList.add('pe-dragging');
        document.addEventListener('pointermove', dragMove);
        document.addEventListener('pointerup', dragEnd);
        document.addEventListener('pointercancel', dragEnd);
    }

    function dragMove(ev) {
        if (!drag) {
            return;
        }
        ev.preventDefault();
        var t = document.elementFromPoint(ev.clientX, ev.clientY);
        var over = t && t.closest ? t.closest('#pe-canvas > .pe-blk') : null;
        if (over && over !== drag) {
            var r = over.getBoundingClientRect();
            over.parentNode.insertBefore(drag, ev.clientY > r.top + r.height / 2 ? over.nextSibling : over);
        }
        if (ev.clientY < 120) {
            window.scrollBy(0, -16);
        } else if (ev.clientY > window.innerHeight - 60) {
            window.scrollBy(0, 16);
        }
    }

    function dragEnd() {
        document.removeEventListener('pointermove', dragMove);
        document.removeEventListener('pointerup', dragEnd);
        document.removeEventListener('pointercancel', dragEnd);
        if (!drag) {
            return;
        }
        drag.classList.remove('pe-dragging');
        drag = null;
        if (order() !== startOrder) {
            touch();
        }
    }

    function move(btn, dir) {
        var b = btn.closest('.pe-blk');
        var sib = dir < 0 ? b.previousElementSibling : b.nextElementSibling;
        if (!sib || !sib.classList.contains('pe-blk')) {
            return;
        }
        if (dir < 0) {
            b.parentNode.insertBefore(b, sib);
        } else {
            b.parentNode.insertBefore(sib, b);
        }
        b.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        touch();
    }

    function hide(btn) {
        var b = btn.closest('.pe-blk');
        var on = b.getAttribute('data-hidden') !== '1';
        b.setAttribute('data-hidden', on ? '1' : '0');
        b.classList.toggle('pe-hid', on);
        btn.querySelector('span').textContent = on ? 'Show' : 'Hide';
        btn.setAttribute('aria-pressed', on ? 'true' : 'false');
        touch();
    }

    function drop(btn) {
        var b = btn.closest('.pe-blk');
        ask({ title: 'Remove this block?', text: 'It disappears from the page when you save. Use Hide to keep it for later.', ok: 'Remove', no: 'Keep it', danger: true }, function () {
            b.parentNode.removeChild(b);
            touch();
        });
    }

    function media(btn) {
        mediaFor = btn.closest('.pe-em');
        var allow = mediaFor.getAttribute('data-allow') || '';
        pickMedia(allow, function (it) {
            mediaFor.setAttribute('data-src', it.src);
            paintMedia(mediaFor, it);
            touch();
        });
    }

    function noMedia(btn) {
        var m = btn.closest('.pe-em');
        m.setAttribute('data-src', '');
        paintMedia(m, null);
        touch();
    }

    function paintMedia(m, it) {
        var b = m.querySelector('.pe-emb');
        if (!it) {
            b.innerHTML = '<span>' + esc(m.getAttribute('data-ph') || 'Add a picture') + '</span>';
        } else if (it.kind === 'video') {
            b.innerHTML = '<video src="' + esc(it.src) + '#t=0.1" muted preload="metadata" playsinline></video><span class="pe-emv">Video</span><span class="pe-emc">Change</span>';
        } else {
            b.innerHTML = '<img src="' + esc(it.thumb) + '" alt=""><span class="pe-emc">Change</span>';
        }
        var x = m.querySelector('.pe-emx');
        if (x) {
            x.hidden = !it;
        }
    }

    function pickMedia(allow, done) {
        pkAllow = allow;
        pkDone = done;
        el('pe-pkt').textContent = allow === 'photo' ? 'Choose a photo' : 'Choose a photo or video';
        el('pe-pkq').value = '';
        el('pe-pkmsg').textContent = '';
        var k = el('pe-pkkind');
        k.value = allow;
        k.disabled = !!allow;
        el('pe-pkfile').setAttribute('accept', allow === 'photo' ? 'image/jpeg,image/png,image/webp,image/gif' : 'image/jpeg,image/png,image/webp,image/gif,video/mp4');
        var d = el('pe-pk');
        d.classList.add('pe-open');
        d.setAttribute('aria-hidden', 'false');
        pkList();
        setTimeout(function () {
            el('pe-pkq').focus();
        }, 30);
    }

    function pkList() {
        api('MediaPick', [{ key: 'pallow', vlu: pkAllow }, { key: 'pkind', vlu: val('pe-pkkind') }, { key: 'pterm', vlu: val('pe-pkq') }]);
    }

    function pkSearch() {
        clearTimeout(pkTimer);
        pkTimer = setTimeout(pkList, 300);
    }

    function pkPick(b) {
        var f = pkDone;
        var it = { src: b.getAttribute('data-src'), thumb: b.getAttribute('data-thumb'), kind: b.getAttribute('data-kind') };
        pkClose();
        if (f) {
            f(it);
        }
    }

    function pkOpen() {
        var d = el('pe-pk');
        return d && d.classList.contains('pe-open');
    }

    function pkClose() {
        var d = el('pe-pk');
        if (d) {
            d.classList.remove('pe-open');
            d.setAttribute('aria-hidden', 'true');
        }
        el('pe-pkfile').value = '';
        pkDone = null;
    }

    function pkUpload() {
        el('pe-pkmsg').textContent = 'Uploading. Large videos can take a minute.';
        api('MediaUp', [{ key: 'pallow', vlu: pkAllow }]);
    }

    function pkUploaded(ok, msg) {
        el('pe-pkfile').value = '';
        el('pe-pkmsg').textContent = msg;
        if (ok) {
            el('pe-pkq').value = '';
            el('pe-pkkind').value = pkAllow;
        }
    }

    function addBlock(type) {
        api('AddBlock', [{ key: 'btype', vlu: type }]);
    }

    function added() {
        var h = el('pe-holder');
        var b = h.firstElementChild;
        if (!b) {
            return;
        }
        el('pe-canvas').insertBefore(b, el('pe-addp'));
        b.scrollIntoView({ block: 'center', behavior: 'smooth' });
        var f = b.querySelector('[contenteditable]');
        if (f) {
            setTimeout(function () {
                f.focus();
            }, 300);
        }
        touch();
    }

    function addItem(btn) {
        var box = btn.parentNode.querySelector('.pe-eitems');
        var tpl = btn.parentNode.querySelector('template');
        if (box.children.length >= 12) {
            toast('A block can have up to 12 items.');
            return;
        }
        box.insertAdjacentHTML('beforeend', tpl.innerHTML);
        var f = box.lastElementChild.querySelector('[contenteditable]');
        if (f) {
            f.focus();
        }
        touch();
    }

    function itemMove(btn, dir) {
        var it = btn.closest('.pe-eit');
        var sib = dir < 0 ? it.previousElementSibling : it.nextElementSibling;
        if (!sib) {
            return;
        }
        if (dir < 0) {
            it.parentNode.insertBefore(it, sib);
        } else {
            it.parentNode.insertBefore(sib, it);
        }
        touch();
    }

    function itemDrop(btn) {
        var it = btn.closest('.pe-eit');
        it.parentNode.removeChild(it);
        touch();
    }

    function cleanNode(n) {
        var out = '';
        [].forEach.call(n.childNodes, function (c) {
            if (c.nodeType === 3) {
                out += esc(c.nodeValue);
                return;
            }
            if (c.nodeType !== 1) {
                return;
            }
            var t = c.tagName.toLowerCase();
            if (/^(script|style|template|iframe|object|img|video|svg)$/.test(t)) {
                return;
            }
            var inner = cleanNode(c);
            if (t === 'br') {
                out += '<br>';
            } else if (t === 'b' || t === 'strong') {
                out += inner ? '<b>' + inner + '</b>' : '';
            } else if (t === 'i' || t === 'em') {
                out += inner ? '<i>' + inner + '</i>' : '';
            } else if (t === 'a') {
                var h = c.getAttribute('href') || '';
                out += h ? '<a href="' + esc(h) + '">' + inner + '</a>' : inner;
            } else if (t === 'ul' || t === 'ol' || t === 'li') {
                out += '<' + t + '>' + inner + '</' + t + '>';
            } else if (t === 'p' || t === 'div' || /^h[1-6]$/.test(t)) {
                out += '<p>' + inner + '</p>';
            } else {
                out += inner;
            }
        });
        return out;
    }

    function textOf(f) {
        if (f.classList.contains('pe-er')) {
            return cleanNode(f);
        }
        return (f.innerText || '').replace(/ /g, ' ').replace(/\n{3,}/g, '\n\n').trim();
    }

    function collect() {
        return [].map.call(blocks(), function (b) {
            var o = { id: b.getAttribute('data-id'), type: b.getAttribute('data-type'), source: b.getAttribute('data-src') || '', hidden: b.getAttribute('data-hidden') === '1', kicker: '', title: '', text: '', media: '', label: '', link: '', label2: '', link2: '', flip: false, items: [] };
            [].forEach.call(b.querySelectorAll('[data-f]'), function (f) {
                var k = f.getAttribute('data-f');
                if (k === 'media') {
                    o.media = f.getAttribute('data-src') || '';
                } else if (k === 'flip') {
                    o.flip = f.checked;
                } else if (k === 'items') {
                    o.items = [].map.call(f.querySelectorAll(':scope > .pe-eit'), function (it) {
                        var r = { icon: '', media: '', title: '', text: '', link: '', label: '' };
                        [].forEach.call(it.querySelectorAll('[data-i]'), function (x) {
                            var n = x.getAttribute('data-i');
                            r[n] = n === 'media' ? (x.getAttribute('data-src') || '') : x.tagName === 'INPUT' || x.tagName === 'SELECT' ? x.value.trim() : textOf(x);
                        });
                        return r;
                    });
                } else if (f.tagName === 'INPUT' || f.tagName === 'SELECT') {
                    o[k] = f.value.trim();
                } else {
                    o[k] = textOf(f);
                }
            });
            return o;
        });
    }

    function save() {
        tools(false);
        el('pe-savebtn').disabled = true;
        api('Save', [{ key: 'page', vlu: page }, { key: 'blocks', vlu: JSON.stringify(collect()) }]);
    }

    function saved(msg) {
        var b = el('pe-savebtn');
        if (b) {
            b.disabled = false;
        }
        dirty = false;
        mark();
        toast(msg);
    }

    function fail(msg) {
        el('pe-savebtn').disabled = false;
        toast(msg);
    }

    function versions() {
        api('Versions', [{ key: 'page', vlu: page }]);
    }

    function opened(title) {
        openModal(title);
    }

    function restore(i, when) {
        ask({ title: 'Restore the version from ' + when + '?', text: 'The page goes back to that version right away. The current version is kept in the list, so you can switch back.', ok: 'Restore', no: 'Cancel' }, function () {
            shut();
            api('Restore', [{ key: 'page', vlu: page }, { key: 'vi', vlu: String(i) }]);
        });
    }

    function view() {
        if (dirty) {
            toast('Save first to see your changes on the website.');
        }
        window.open(el('pe-viewlink').getAttribute('data-href'), '_blank', 'noopener');
    }

    function reveal() {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                if (askBox && askBox.classList.contains('pe-open')) {
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
        var m = el('pe-modal');
        if (m) {
            m.addEventListener('mousedown', function (e) {
                if (e.target === m) {
                    closeModal();
                }
            });
        }
        var d = el('pe-pk');
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
        loaded: loaded,
        tab: tab,
        shown: shown,
        fmt: fmt,
        grab: grab,
        move: move,
        hide: hide,
        drop: drop,
        media: media,
        noMedia: noMedia,
        pkList: pkList,
        pkSearch: pkSearch,
        pkPick: pkPick,
        pkClose: pkClose,
        pkUpload: pkUpload,
        pkUploaded: pkUploaded,
        addBlock: addBlock,
        added: added,
        addItem: addItem,
        itemMove: itemMove,
        itemDrop: itemDrop,
        save: save,
        saved: saved,
        fail: fail,
        versions: versions,
        opened: opened,
        restore: restore,
        view: view
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminPagesJs.reveal);
} else {
    AdminPagesJs.reveal();
}
