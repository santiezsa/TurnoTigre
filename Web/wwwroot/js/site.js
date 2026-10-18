// TurnoTigre - tema claro/oscuro y ojito de contraseña

function aplicarTema(t) {
    document.documentElement.setAttribute('data-theme', t);
    var txt = t === 'dark' ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro';
    document.querySelectorAll('.theme-btn').forEach(function (b) {
        b.title = txt;
        b.setAttribute('aria-label', txt);
    });
}

function alternarTema() {
    var t = document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
    aplicarTema(t);
    try { localStorage.setItem('tt-tema', t); } catch (e) { }
}

(function () {
    var t = 'light';
    try { t = localStorage.getItem('tt-tema') || 'light'; } catch (e) { }
    aplicarTema(t);

    document.querySelectorAll('.theme-btn').forEach(function (b) { b.addEventListener('click', alternarTema); });

    // Ojito para mostrar/ocultar contraseña en todos los campos de contraseña
    var ojo = '<svg class="i-ojo" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-7 11-7 11 7 11 7-4 7-11 7S1 12 1 12z"/><circle cx="12" cy="12" r="3"/></svg>' +
        '<svg class="i-ojo-x" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 19c-7 0-11-7-11-7a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 7 11 7a18.5 18.5 0 0 1-2.16 3.19M14.12 14.12a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></svg>';

    document.querySelectorAll('input[type=password]').forEach(function (input) {
        if (input.closest('.pw-wrap')) return;
        var wrap = document.createElement('div');
        wrap.className = 'pw-wrap';
        input.parentNode.insertBefore(wrap, input);
        wrap.appendChild(input);

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'pw-eye';
        btn.setAttribute('aria-label', 'Mostrar u ocultar contraseña');
        btn.innerHTML = ojo;
        btn.addEventListener('click', function () {
            var ver = input.type === 'password';
            input.type = ver ? 'text' : 'password';
            btn.classList.toggle('on', ver);
        });
        wrap.appendChild(btn);
    });
})();


// Envía un formulario de búsqueda (GET) sin arrastrar el token antifalsificación a la URL.
function ttBuscar(form) {
    var t = form.querySelector('input[name="__RequestVerificationToken"]');
    if (t) t.disabled = true;
    form.submit();
}


// =====================================================================
// Ventanas modales (estilo del prototipo): avisos, confirmaciones y modales fijos
// =====================================================================
var ttPila = [];   // modales abiertos (el último es el de arriba)

function ttEsc(s) { var d = document.createElement('div'); d.textContent = s == null ? '' : String(s); return d.innerHTML; }

function ttActualizarCuerpo() { document.body.classList.toggle('tt-modal-abierta', ttPila.length > 0); }

function ttAbrirOverlay(ov, cancelar, foco) {
    var previo = document.activeElement;
    ov.classList.add('open');
    ttPila.push({ ov: ov, cancelar: cancelar, previo: previo });
    ttActualizarCuerpo();
    var f = foco || ov.querySelector('[data-foco]') || ov.querySelector('button, input, a[href]');
    if (f) setTimeout(function () { try { f.focus(); } catch (e) { } }, 30);
}

function ttCerrarOverlay(ov) {
    ov.classList.remove('open');
    var i = ttPila.findIndex(function (m) { return m.ov === ov; });
    if (i >= 0) {
        var previo = ttPila[i].previo;
        ttPila.splice(i, 1);
        if (previo && previo.focus && document.body.contains(previo)) { try { previo.focus(); } catch (e) { } }
    }
    ttActualizarCuerpo();
}

document.addEventListener('keydown', function (e) {
    if (!ttPila.length) return;
    var top = ttPila[ttPila.length - 1];
    if (e.key === 'Escape') { e.preventDefault(); if (top.cancelar) top.cancelar(); return; }
    if (e.key === 'Tab') {   // el foco no sale del modal
        var items = Array.prototype.filter.call(top.ov.querySelectorAll('button:not([disabled]), input:not([disabled]):not([type=hidden]), select, a[href]'), function (x) { return x.offsetParent !== null; });
        if (!items.length) return;
        var primero = items[0], ultimo = items[items.length - 1];
        if (e.shiftKey && document.activeElement === primero) { e.preventDefault(); ultimo.focus(); }
        else if (!e.shiftKey && document.activeElement === ultimo) { e.preventDefault(); primero.focus(); }
    }
});

function ttCrearOverlay(html, alClickFuera) {
    var ov = document.createElement('div');
    ov.className = 'tt-modal-overlay';
    ov.innerHTML = html;
    ov.addEventListener('mousedown', function (e) { if (e.target === ov && alClickFuera) alClickFuera(); });
    document.body.appendChild(ov);
    return ov;
}

// Aviso de resultado: tipo 'ok' (verde), 'err' (rojo) o 'warn' (amarillo). Devuelve una promesa que se cumple al cerrarlo.
function ttAviso(o) {
    return new Promise(function (resolver) {
        var tipo = o.tipo || 'ok';
        var icono = tipo === 'ok' ? '✓' : tipo === 'err' ? '✕' : '!';
        var lista = (o.lista && o.lista.length) ? '<ul class="tt-lista-errores">' + o.lista.map(function (x) { return '<li>' + ttEsc(x) + '</li>'; }).join('') + '</ul>' : '';
        var ov;
        function cerrar() { ttCerrarOverlay(ov); setTimeout(function () { ov.remove(); }, 50); resolver(); }
        ov = ttCrearOverlay('<div class="tt-modal-box" role="alertdialog" aria-modal="true" aria-labelledby="tt-av-tit" aria-describedby="tt-av-txt">' +
            '<div class="tt-modal-icon ' + tipo + '" aria-hidden="true">' + icono + '</div>' +
            '<h2 id="tt-av-tit">' + ttEsc(o.titulo) + '</h2><p id="tt-av-txt">' + ttEsc(o.texto) + '</p>' + lista +
            '<div class="tt-modal-acciones"><button type="button" class="tt-button tt-button-red" data-foco>Aceptar</button></div></div>', cerrar);
        ov.querySelector('button').addEventListener('click', cerrar);
        ttAbrirOverlay(ov, cerrar);
    });
}

// Pregunta de confirmación (Sí / No). Devuelve una promesa con true o false.
function ttConfirmar(o) {
    return new Promise(function (resolver) {
        var tipo = o.tipo || 'warn';
        var ov;
        function fin(v) { ttCerrarOverlay(ov); setTimeout(function () { ov.remove(); }, 50); resolver(v); }
        ov = ttCrearOverlay('<div class="tt-modal-box" role="alertdialog" aria-modal="true" aria-labelledby="tt-cf-tit" aria-describedby="tt-cf-txt">' +
            '<div class="tt-modal-icon ' + tipo + '" aria-hidden="true">' + (tipo === 'info' ? '?' : '!') + '</div>' +
            '<h2 id="tt-cf-tit">' + ttEsc(o.titulo || 'Confirmar') + '</h2><p id="tt-cf-txt">' + ttEsc(o.texto) + '</p>' +
            '<div class="tt-modal-acciones"><button type="button" class="tt-button tt-button-outline" data-no data-foco>' + ttEsc(o.no || 'Cancelar') + '</button>' +
            '<button type="button" class="tt-button tt-button-red" data-si>' + ttEsc(o.si || 'Aceptar') + '</button></div></div>', function () { fin(false); });
        ov.querySelector('[data-no]').addEventListener('click', function () { fin(false); });
        ov.querySelector('[data-si]').addEventListener('click', function () { fin(true); });
        ttAbrirOverlay(ov, function () { fin(false); });
    });
}

// Modales fijos de la página (por ejemplo "Recuperar contraseña")
function ttAbrirModal(id) {
    var ov = document.getElementById(id);
    if (!ov) return;
    ttAbrirOverlay(ov, function () { ttCerrarOverlay(ov); });
    ov.dispatchEvent(new CustomEvent('tt-abierto'));
}
function ttCerrarModal(id) { var ov = document.getElementById(id); if (ov) ttCerrarOverlay(ov); }

document.addEventListener('click', function (e) {
    var a = e.target.closest('[data-modal-abrir]');
    if (a) { e.preventDefault(); ttAbrirModal(a.getAttribute('data-modal-abrir')); return; }
    var c = e.target.closest('[data-modal-cerrar]');
    if (c) { e.preventDefault(); var ov = c.closest('.tt-modal-overlay'); if (ov) ttCerrarOverlay(ov); return; }
    if (e.target.classList && e.target.classList.contains('tt-modal-overlay') && e.target.id) ttCerrarModal(e.target.id);
});

// Confirmación antes de una acción: <form data-confirm="..."> o <button data-confirm="...">
//   opcionales: data-confirm-titulo, data-confirm-si, data-confirm-tipo (warn | info)
function ttPreguntar(el) {
    return ttConfirmar({
        titulo: el.getAttribute('data-confirm-titulo') || 'Confirmar',
        texto: el.getAttribute('data-confirm'),
        si: el.getAttribute('data-confirm-si') || 'Aceptar',
        tipo: el.getAttribute('data-confirm-tipo') || 'warn'
    });
}
document.addEventListener('click', function (e) {
    var b = e.target.closest('button[data-confirm], input[data-confirm]');
    if (!b || b.disabled) return;
    if (b.dataset.ttOk) { delete b.dataset.ttOk; return; }
    e.preventDefault();
    ttPreguntar(b).then(function (si) { if (si) { b.dataset.ttOk = '1'; b.click(); } });
}, true);
document.addEventListener('submit', function (e) {
    var f = e.target;
    if (!f.hasAttribute || !f.hasAttribute('data-confirm')) return;
    if (f.dataset.ttOk) { delete f.dataset.ttOk; return; }
    e.preventDefault();
    ttPreguntar(f).then(function (si) { if (si) { f.dataset.ttOk = '1'; f.requestSubmit ? f.requestSubmit() : f.submit(); } });
}, true);

// Avisos que llegan desde el servidor (resultado de una acción o error de ingreso):
//   <div class="tt-aviso-datos" data-tipo="ok|err" data-titulo="..." data-texto="..." data-foco="#id"></div>
(function () {
    var d = document.querySelector('.tt-aviso-datos');
    if (!d) return;
    var foco = d.getAttribute('data-foco');
    ttAviso({ tipo: d.getAttribute('data-tipo'), titulo: d.getAttribute('data-titulo'), texto: d.getAttribute('data-texto') })
        .then(function () { if (foco) { var f = document.querySelector(foco); if (f) f.focus(); } });
})();

// Botón principal deshabilitado hasta completar los campos obligatorios: <form data-habilitar>
document.querySelectorAll('form[data-habilitar]').forEach(function (f) {
    var b = f.querySelector('button[type=submit]');
    var campos = Array.prototype.slice.call(f.querySelectorAll('input[required]'));
    if (!b || !campos.length) return;
    function revisar() { b.disabled = !campos.every(function (c) { return c.value.trim() !== '' && c.checkValidity(); }); }
    campos.forEach(function (c) { c.addEventListener('input', revisar); c.addEventListener('change', revisar); });
    revisar(); setTimeout(revisar, 300); setTimeout(revisar, 1200);   // por si el navegador autocompleta
});
