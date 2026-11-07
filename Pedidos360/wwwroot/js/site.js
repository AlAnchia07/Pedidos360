// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


(function () {
    const CLAVE = 'pedidos360_alerta';

    window.mostrarAlerta = function (tipo, titulo, detalle) {
        const cont = document.getElementById('alertas-js');
        if (!cont) return;
        const ok = tipo !== 'error';
        const div = document.createElement('div');
        div.className = 'alerta-p360 ' + (ok ? 'alerta-ok' : 'alerta-error');
        div.setAttribute('role', 'alert');
        div.innerHTML =
            '<i class="bi ' + (ok ? 'bi-check-circle' : 'bi-x-circle') + ' alerta-icono"></i>' +
            '<div class="alerta-texto"><div class="alerta-titulo"></div><div class="alerta-detalle"></div></div>' +
            '<button type="button" class="alerta-cerrar" aria-label="Cerrar"><i class="bi bi-x-lg"></i></button>';
        div.querySelector('.alerta-titulo').textContent = titulo;
        div.querySelector('.alerta-detalle').textContent = detalle || '';
        div.querySelector('.alerta-cerrar').addEventListener('click', function () { div.remove(); });
        cont.appendChild(div);
        setTimeout(function () { div.remove(); }, 6000);
    };

    function guardarAviso(form, titulo, detalle) {
        try {
            sessionStorage.setItem(CLAVE, JSON.stringify({
                titulo: titulo,
                detalle: detalle,
                origen: new URL(form.action, location.href).pathname,
                t: Date.now()
            }));
        } catch (e) { }
    }

    // Mostrar el aviso pendiente al cargar la página
    document.addEventListener('DOMContentLoaded', function () {
        try {
            const raw = sessionStorage.getItem(CLAVE);
            if (!raw) return;
            sessionStorage.removeItem(CLAVE);
            const a = JSON.parse(raw);
            // Si volvimos a la misma URL del POST, hubo error de validación: no mostrar
            if (Date.now() - a.t < 15000 && a.origen !== location.pathname) {
                mostrarAlerta('ok', a.titulo, a.detalle);
            }
        } catch (e) { }
    });

    let formPendiente = null;

    document.addEventListener('submit', function (e) {
        const form = e.target;

        // Eliminar: abrir el modal de confirmación
        if (form.matches && form.matches('form[data-eliminar]')) {
            e.preventDefault();
            formPendiente = form;
            document.getElementById('eliminar-nombre').textContent = form.dataset.eliminar || 'este registro';
            bootstrap.Modal.getOrCreateInstance(document.getElementById('modalEliminar')).show();
            return;
        }

        // Crear/Editar: guardar aviso si el formulario es válido en el cliente
        if (form.dataset && form.dataset.alertaTitulo) {
            if (window.jQuery && jQuery.fn.valid && !jQuery(form).valid()) return;
            guardarAviso(form, form.dataset.alertaTitulo, form.dataset.alertaDetalle);
        }
    }, true);

    document.addEventListener('click', function (e) {
        if (e.target.closest('#btn-confirmar-eliminar') && formPendiente) {
            guardarAviso(formPendiente, 'Cliente eliminado correctamente', 'El registro se eliminó de la lista.');
            const f = formPendiente;
            formPendiente = null;
            f.submit(); // no dispara el evento submit, evita el bucle
        }
    });
})();