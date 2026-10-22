var AttorneysJs = (function () {

    var ttimer = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function call(method, list) {
        $ApiRequest('Attorneys/' + method, JSON.stringify(list || []));
    }

    function toast(msg) {
        var t = el('at-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('at-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('at-open');
        }, 3200);
    }

    function openNav() {
        el('at-nav').classList.add('at-open');
        el('at-scrim').classList.add('at-open');
    }

    function closeNav() {
        el('at-nav').classList.remove('at-open');
        el('at-scrim').classList.remove('at-open');
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeNav();
        }
    });

    function filter(area) {
        call('Filter', [{ key: 'area', vlu: area }]);
    }

    function filtered(area) {
        try {
            history.pushState({ f: area }, '', 'Attorneys' + (area ? '?f=' + encodeURIComponent(area) : ''));
        } catch (e) {
        }
    }

    window.addEventListener('popstate', function () {
        location.reload();
    });

    return {
        openNav: openNav,
        closeNav: closeNav,
        toast: toast,
        filter: filter,
        filtered: filtered
    };

})();
