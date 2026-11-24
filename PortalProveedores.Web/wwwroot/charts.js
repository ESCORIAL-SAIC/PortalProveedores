// Interop mínimo con Chart.js. Un gráfico por canvas id; se destruye el anterior al redibujar.
window.portalCharts = {
    _inst: {},
    // ref: DotNetObjectReference con un método Ir(id, indice). clickable: bool por segmento/barra.
    dibujar(id, tipo, etiquetas, datos, colores, titulo, ref, clickable) {
        const el = document.getElementById(id);
        if (!el) return;
        this._inst[id]?.destroy();
        const css = getComputedStyle(document.documentElement);
        const v = n => css.getPropertyValue(n).trim();
        const texto = v('--suave');
        const grilla = v('--borde');
        // colores: hex o nombre de estado (venc, warn, azul, gris) -> --g-<nombre>.
        const pinta = c => c.startsWith('#') ? c : v('--g-' + c);
        const fondo = Array.isArray(colores) ? colores.map(pinta) : pinta(colores);
        const circular = tipo === 'doughnut';
        this._inst[id] = new Chart(el, {
            type: tipo,
            data: { labels: etiquetas, datasets: [{ label: titulo, data: datos, backgroundColor: fondo, borderColor: v('--card'), borderWidth: 2, borderRadius: circular ? 0 : 4 }] },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                onClick: (_, els) => {
                    if (ref && els.length && clickable?.[els[0].index]) ref.invokeMethodAsync('Ir', id, els[0].index);
                },
                onHover: (e, els) => {
                    e.native.target.style.cursor = els.length && clickable?.[els[0].index] ? 'pointer' : 'default';
                },
                plugins: { legend: { display: circular, position: 'bottom', labels: { color: texto } } },
                scales: circular ? {} : {
                    x: { ticks: { color: texto }, grid: { display: false } },
                    y: { beginAtZero: true, ticks: { color: texto, precision: 0 }, grid: { color: grilla } },
                },
            },
        });
    },
    destruir(id) { this._inst[id]?.destroy(); delete this._inst[id]; },
};
