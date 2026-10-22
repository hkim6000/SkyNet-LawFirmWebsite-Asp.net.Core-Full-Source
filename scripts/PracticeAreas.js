var PracticeAreasJs = (function () {

    var ttimer = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function call(method, list) {
        $ApiRequest('PracticeAreas/' + method, JSON.stringify(list || []));
    }

    function toast(msg) {
        var t = el('pa-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('pa-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('pa-open');
        }, 3200);
    }

    function openNav() {
        el('pa-nav').classList.add('pa-open');
        el('pa-scrim').classList.add('pa-open');
    }

    function closeNav() {
        el('pa-nav').classList.remove('pa-open');
        el('pa-scrim').classList.remove('pa-open');
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
