var PracticeAreaJs = (function () {

    var ttimer = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function call(method, list) {
        $ApiRequest('PracticeArea/' + method, JSON.stringify(list || []));
    }

    function toast(msg) {
        var t = el('pd-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('pd-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('pd-open');
        }, 3200);
    }

    function openNav() {
        el('pd-nav').classList.add('pd-open');
        el('pd-scrim').classList.add('pd-open');
    }

    function closeNav() {
        el('pd-nav').classList.remove('pd-open');
        el('pd-scrim').classList.remove('pd-open');
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
