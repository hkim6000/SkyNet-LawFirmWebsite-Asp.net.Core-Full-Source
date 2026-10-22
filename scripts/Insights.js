var InsightsJs = (function () {

    var ttimer = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function call(method, list) {
        $ApiRequest('Insights/' + method, JSON.stringify(list || []));
    }

    function toast(msg) {
        var t = el('in-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('in-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('in-open');
        }, 3200);
    }

    function openNav() {
        el('in-nav').classList.add('in-open');
        el('in-scrim').classList.add('in-open');
    }

    function closeNav() {
        el('in-nav').classList.remove('in-open');
        el('in-scrim').classList.remove('in-open');
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeNav();
        }
    });

    var cur = '';

    function filter(topic, pg) {
        call('Filter', [{ key: 'topic', vlu: topic }, { key: 'pg', vlu: String(pg || 1) }]);
    }

    function filtered(topic, pg) {
        cur = topic;
        var q = [];
        if (topic) {
            q.push('t=' + encodeURIComponent(topic));
        }
        if (pg > 1) {
            q.push('p=' + pg);
        }
        try {
            history.pushState({ t: topic }, '', 'Insights' + (q.length ? '?' + q.join('&') : ''));
        } catch (e) {
        }
        top('in-stage');
    }

    function top(id) {
        var e = el(id);
        if (e) {
            e.scrollIntoView({ behavior: 'auto', block: 'start' });
        }
    }

    function opened(key, title) {
        try {
            history.pushState({ a: key }, '', 'Insights?a=' + encodeURIComponent(key));
        } catch (e) {
        }
        document.title = title;
        window.scrollTo(0, 0);
    }

    function back() {
        filter(cur, 1);
        return false;
    }

    document.addEventListener('click', function (e) {
        var a = e.target.closest ? e.target.closest('a[href^="Insights?a="]') : null;
        if (!a || e.metaKey || e.ctrlKey || e.shiftKey || e.button !== 0) {
            return;
        }
        e.preventDefault();
        call('Open', [{ key: 'art', vlu: decodeURIComponent(a.getAttribute('href').split('a=')[1]) }]);
    });

    window.addEventListener('popstate', function () {
        location.reload();
    });

    return {
        openNav: openNav,
        closeNav: closeNav,
        toast: toast,
        filter: filter,
        filtered: filtered,
        opened: opened,
        back: back
    };

})();
