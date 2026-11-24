// El navegador puede restaurar /login desde la caché al volver atrás con el botón ya deshabilitado.
addEventListener('pageshow', () => {
    document.querySelectorAll('.pw input').forEach(i => { i.type = 'password'; });
    document.querySelectorAll('.pw .ver').forEach(b => {
        b.textContent = 'Mostrar';
        b.setAttribute('aria-pressed', 'false');
        b.setAttribute('aria-label', 'Mostrar contraseña');
    });
    document.querySelectorAll('form.login button[type=submit]').forEach(b => {
        b.disabled = false;
        b.classList.remove('ocupado');
        b.textContent = b.dataset.texto;
    });
});
// Mostrar/ocultar contraseña (el login es SSR estático: JS plano).
document.addEventListener('click', e => {
    const b = e.target.closest('.pw .ver');
    if (!b) return;
    const i = b.previousElementSibling, ver = i.type === 'password';
    i.type = ver ? 'text' : 'password';
    b.textContent = ver ? 'Ocultar' : 'Mostrar';
    b.setAttribute('aria-pressed', ver);
    b.setAttribute('aria-label', ver ? 'Ocultar contraseña' : 'Mostrar contraseña');
});
