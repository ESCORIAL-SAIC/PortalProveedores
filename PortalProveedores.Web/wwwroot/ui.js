// El navegador puede restaurar /login desde la caché al volver atrás con el botón ya deshabilitado.
addEventListener('pageshow', () => {
    document.querySelectorAll('form.login button[type=submit]').forEach(b => {
        b.disabled = false;
        b.classList.remove('ocupado');
        b.textContent = b.dataset.texto;
    });
});
