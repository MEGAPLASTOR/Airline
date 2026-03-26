/* =============================================
   ADMIN PAGES — Shared JavaScript
   Common utilities: toast, modal close
   ============================================= */

function showToast(msg, isError = false) {
    const toast = document.getElementById('toast');
    toast.style.borderLeftColor = isError ? 'var(--accent-coral)' : 'var(--accent-gold)';
    const icon = isError
        ? '<i class="fa-solid fa-circle-exclamation" style="color:var(--accent-coral)"></i>'
        : '<i class="fa-solid fa-circle-check" style="color:var(--accent-gold)"></i>';
    toast.innerHTML = icon + ' ' + msg;
    toast.classList.add('show');
    setTimeout(() => toast.classList.remove('show'), 3000);
}

function closeModal(id) {
    document.getElementById(id).classList.remove('active');
}
