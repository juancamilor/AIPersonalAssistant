(function () {
    'use strict';

    var tools = [
        { icon: '♟️', name: 'Chess Trainer', desc: 'Play, solve puzzles, and learn chess', href: '/chess-trainer.html' },
        { icon: '🗺️', name: 'Travel Map', desc: 'Pin and explore places you\'ve visited', href: '/travel-map.html' },
        { icon: '💱', name: 'Rate Exchange', desc: 'Convert currencies and view trends', href: '/rate-exchange.html' },
        { icon: '📈', name: 'Stock Tools', desc: 'Analyze stock performance and trends', href: '/stock-tools.html' },
        { icon: '🧾', name: 'Taxes Manager', desc: 'Estimate your tax return from W2 data', href: '/taxes-manager.html' },
        { icon: '📜', name: 'Final Wishes', desc: 'Document and manage your final wishes', href: '/wishes.html' },
        { icon: '🍳', name: 'Cooking Recipes', desc: 'Browse and save your favorite recipes', href: '/recipes.html' }
    ];

    function create(tag, attrs, parent) {
        var el = document.createElement(tag);
        if (attrs) {
            Object.keys(attrs).forEach(function (k) {
                if (k === 'text') el.textContent = attrs[k];
                else if (k === 'html') el.innerHTML = attrs[k];
                else if (k === 'className') el.className = attrs[k];
                else el.setAttribute(k, attrs[k]);
            });
        }
        if (parent) parent.appendChild(el);
        return el;
    }

    function init() {
        // Help button
        var btn = create('button', { className: 'help-palette-btn', 'aria-label': 'Help', text: '❓' }, document.body);

        // Overlay
        var overlay = create('div', { className: 'help-palette-overlay' }, document.body);

        // Panel
        var panel = create('div', { className: 'help-palette-panel' }, document.body);

        // Header
        var header = create('div', { className: 'help-palette-header' }, panel);
        create('h2', { text: 'Tools' }, header);
        var search = create('input', { className: 'help-palette-search', type: 'text', placeholder: 'Search tools…' }, header);

        // List container
        var list = create('div', { className: 'help-palette-list' }, panel);

        function renderList(filter) {
            list.innerHTML = '';
            var q = (filter || '').toLowerCase();
            var matched = tools.filter(function (t) {
                return !q || t.name.toLowerCase().indexOf(q) !== -1 || t.desc.toLowerCase().indexOf(q) !== -1;
            });
            if (matched.length === 0) {
                create('div', { className: 'help-palette-empty', text: 'No tools found.' }, list);
                return;
            }
            matched.forEach(function (t) {
                var a = create('a', { className: 'help-palette-item', href: t.href }, list);
                create('span', { className: 'help-palette-item-icon', text: t.icon }, a);
                var info = create('div', { className: 'help-palette-item-info' }, a);
                create('div', { className: 'help-palette-item-name', text: t.name }, info);
                create('div', { className: 'help-palette-item-desc', text: t.desc }, info);
            });
        }

        function open() {
            overlay.classList.add('open');
            panel.classList.add('open');
            search.value = '';
            renderList('');
            setTimeout(function () { search.focus(); }, 100);
        }

        function close() {
            overlay.classList.remove('open');
            panel.classList.remove('open');
        }

        btn.addEventListener('click', open);
        overlay.addEventListener('click', close);
        search.addEventListener('input', function () { renderList(search.value); });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && panel.classList.contains('open')) {
                close();
            }
        });

        renderList('');
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
