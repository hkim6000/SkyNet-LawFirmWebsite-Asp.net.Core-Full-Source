var ContactJs = (function () {

    var ttimer = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function call(method, list) {
        $ApiRequest('Contact/' + method, JSON.stringify(list || []));
    }

    function toast(msg) {
        var t = el('ct-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('ct-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('ct-open');
        }, 3200);
    }

    function openNav() {
        el('ct-nav').classList.add('ct-open');
        el('ct-scrim').classList.add('ct-open');
    }

    function closeNav() {
        el('ct-nav').classList.remove('ct-open');
        el('ct-scrim').classList.remove('ct-open');
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeNav();
        }
    });

    function send() {
        var b = el('ct-send');
        b.disabled = true;
        call('Submit', [
            { key: 'name', vlu: val('ct-name') },
            { key: 'email', vlu: val('ct-email') },
            { key: 'phone', vlu: val('ct-phone') },
            { key: 'area', vlu: el('ct-area').value },
            { key: 'msg', vlu: val('ct-msg') },
            { key: 'ack', vlu: el('ct-ack').checked ? '1' : '' },
            { key: 'web', vlu: val('ct-web') }
        ]);
        return false;
    }

    function failed() {
        el('ct-send').disabled = false;
        var bad = document.querySelector('.ct-err:not(:empty)');
        if (bad) {
            var f = bad.parentNode.querySelector('input,textarea,select');
            if (f) {
                f.focus();
            }
        }
    }

    function sent() {
        el('ct-send').disabled = false;
        el('ct-form').reset();
        el('ct-sent').scrollIntoView({ behavior: 'auto', block: 'center' });
    }

    return {
        openNav: openNav,
        closeNav: closeNav,
        toast: toast,
        send: send,
        failed: failed,
        sent: sent
    };

})();
