var AttorneyJs = (function () {

    var ttimer = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function call(method, list) {
        $ApiRequest('Attorney/' + method, JSON.stringify(list || []));
    }

    function toast(msg) {
        var t = el('ap-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('ap-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('ap-open');
        }, 3200);
    }

    function openNav() {
        el('ap-nav').classList.add('ap-open');
        el('ap-scrim').classList.add('ap-open');
    }

    function closeNav() {
        el('ap-nav').classList.remove('ap-open');
        el('ap-scrim').classList.remove('ap-open');
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeNav();
        }
    });

    return {
        openNav: openNav,
        closeNav: closeNav,
        toast: toast
    };

})();
