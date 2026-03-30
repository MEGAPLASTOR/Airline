let routeModal;
let routeDeleteModal;

function openModal(modal) {
    if (!modal) return;
    modal.classList.add('active');
    document.body.style.overflow = 'hidden';
}

function closeModalElement(modal) {
    if (!modal) return;
    modal.classList.remove('active');
    document.body.style.overflow = '';
}

function closeAllRouteModals() {
    closeModalElement(routeModal);
    closeModalElement(routeDeleteModal);
}

function showToast(message, isError = false) {
    const toast = document.getElementById('toast');
    if (!toast) return;

    toast.style.borderLeftColor = isError ? 'var(--accent-coral)' : 'var(--ocean-aqua)';
    toast.innerHTML = isError
        ? '<i class="fa-solid fa-circle-exclamation" style="color:var(--accent-coral)"></i><span>' + message + '</span>'
        : '<i class="fa-solid fa-circle-check" style="color:var(--accent-green)"></i><span>' + message + '</span>';
    toast.classList.add('show');

    setTimeout(() => {
        toast.classList.remove('show');
    }, 3000);
}

function openCreateModal() {
    document.getElementById('mTitle').textContent = 'Add Route';
    document.getElementById('rId').value = '0';
    document.getElementById('rDep').value = '';
    document.getElementById('rArr').value = '';
    openModal(routeModal);
}

async function openEditModal(id) {
    try {
        const response = await fetch('/Admin/GetRoute/' + id);
        if (!response.ok) {
            throw new Error('Failed to load route');
        }

        const data = await response.json();
        document.getElementById('mTitle').textContent = 'Edit Route';
        document.getElementById('rId').value = id;
        document.getElementById('rDep').value = data.departureCity || '';
        document.getElementById('rArr').value = data.arrivalCity || '';
        openModal(routeModal);
    } catch {
        showToast('Failed to load route data.', true);
    }
}

function openDeleteModal(id, departure, arrival) {
    document.getElementById('delId').value = id;
    document.getElementById('deleteTargetText').textContent = (departure || '-') + ' -> ' + (arrival || '-');
    openModal(routeDeleteModal);
}

async function submitRoute() {
    const id = document.getElementById('rId').value;
    const departureCity = document.getElementById('rDep').value;
    const arrivalCity = document.getElementById('rArr').value;

    if (!departureCity || !arrivalCity) {
        showToast('Please select both departure and arrival cities.', true);
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const formData = new FormData();
    formData.append('departureCity', departureCity);
    formData.append('arrivalCity', arrivalCity);
    formData.append('__RequestVerificationToken', token);

    const url = id === '0' ? '/Admin/CreateRoute' : '/Admin/EditRoute';
    if (id !== '0') {
        formData.append('id', id);
    }

    try {
        const response = await fetch(url, { method: 'POST', body: formData });
        const result = await response.json();

        if (result.success) {
            location.reload();
            return;
        }

        showToast(result.message || 'Unable to save route.', true);
    } catch {
        showToast('A network error occurred.', true);
    }
}

async function submitDelete() {
    const id = document.getElementById('delId').value;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const formData = new FormData();
    formData.append('id', id);
    formData.append('__RequestVerificationToken', token);

    try {
        const response = await fetch('/Admin/DeleteRoute', { method: 'POST', body: formData });
        const result = await response.json();

        if (result.success) {
            location.reload();
            return;
        }

        showToast(result.message || 'Unable to delete route.', true);
    } catch {
        showToast('A network error occurred.', true);
    }
}

function filterRoutes() {
    const keyword = (document.getElementById('routeSearch')?.value || '').trim().toLowerCase();
    const usage = (document.getElementById('routeUsageFilter')?.value || '').trim().toUpperCase();
    const rows = document.querySelectorAll('.route-row');
    let visible = 0;

    rows.forEach(row => {
        const matchesKeyword = !keyword || (row.dataset.search || '').toLowerCase().includes(keyword);
        const matchesUsage = !usage || (row.dataset.usage || '').toUpperCase() === usage;
        const shouldShow = matchesKeyword && matchesUsage;

        row.style.display = shouldShow ? '' : 'none';
        if (shouldShow) {
            visible += 1;
        }
    });

    const counter = document.getElementById('routeCount');
    if (counter) {
        counter.textContent = visible;
    }
}

document.addEventListener('DOMContentLoaded', function () {
    routeModal = document.getElementById('routeModal');
    routeDeleteModal = document.getElementById('deleteModal');

    document.querySelectorAll('.js-close-modal').forEach(button => {
        button.addEventListener('click', closeAllRouteModals);
    });

    [routeModal, routeDeleteModal].forEach(modal => {
        if (!modal) return;
        modal.addEventListener('click', function (event) {
            if (event.target === modal) {
                closeModalElement(modal);
            }
        });
    });

    document.getElementById('routeSearch')?.addEventListener('input', filterRoutes);
    document.getElementById('routeUsageFilter')?.addEventListener('change', filterRoutes);

    document.querySelector('.js-refresh-page')?.addEventListener('click', function () {
        window.location.reload();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeAllRouteModals();
        }
    });
});

