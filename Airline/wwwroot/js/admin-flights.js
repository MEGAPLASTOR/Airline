let flightModal;
let flightDeleteModal;

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

function closeAllFlightModals() {
    closeModalElement(flightModal);
    closeModalElement(flightDeleteModal);
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
    document.getElementById('mTitle').textContent = 'Add Flight';
    document.getElementById('fId').value = '0';
    document.getElementById('fNumber').value = '';
    document.getElementById('fRoute').value = '';
    openModal(flightModal);
}

async function openEditModal(id) {
    try {
        const response = await fetch('/Admin/GetFlight/' + id);
        if (!response.ok) {
            throw new Error('Failed to load flight');
        }

        const data = await response.json();
        document.getElementById('mTitle').textContent = 'Edit Flight';
        document.getElementById('fId').value = data.flightId || id;
        document.getElementById('fNumber').value = data.flightNumber || '';
        document.getElementById('fRoute').value = data.routeId || '';
        openModal(flightModal);
    } catch {
        showToast('Failed to load flight data.', true);
    }
}

function openDeleteModal(id, flightNumber) {
    document.getElementById('delId').value = id;
    document.getElementById('deleteTargetText').textContent = flightNumber || 'Selected flight';
    openModal(flightDeleteModal);
}

async function submitFlight() {
    const id = document.getElementById('fId').value;
    const flightNumber = document.getElementById('fNumber').value.trim().toUpperCase();
    const routeId = document.getElementById('fRoute').value;

    if (!flightNumber || !routeId) {
        showToast('Please enter a flight number and select a route.', true);
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const formData = new FormData();
    formData.append('flightNumber', flightNumber);
    formData.append('routeId', routeId);
    formData.append('__RequestVerificationToken', token);

    const url = id === '0' ? '/Admin/CreateFlight' : '/Admin/EditFlight/' + id;

    try {
        const response = await fetch(url, { method: 'POST', body: formData });
        const result = await response.json();

        if (result.success) {
            location.reload();
            return;
        }

        showToast(result.message || 'Unable to save flight.', true);
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
        const response = await fetch('/Admin/DeleteFlight/' + id, { method: 'POST', body: formData });
        const result = await response.json();

        if (result.success) {
            location.reload();
            return;
        }

        showToast(result.message || 'Unable to delete flight.', true);
    } catch {
        showToast('A network error occurred.', true);
    }
}

function filterFlights() {
    const keyword = (document.getElementById('flightSearch')?.value || '').trim().toLowerCase();
    const status = (document.getElementById('flightScheduleFilter')?.value || '').trim().toUpperCase();
    const rows = document.querySelectorAll('.flight-row');
    let visible = 0;

    rows.forEach(row => {
        const matchesKeyword = !keyword || (row.dataset.search || '').toLowerCase().includes(keyword);
        const matchesStatus = !status || (row.dataset.status || '').toUpperCase() === status;
        const shouldShow = matchesKeyword && matchesStatus;

        row.style.display = shouldShow ? '' : 'none';
        if (shouldShow) {
            visible += 1;
        }
    });

    const counter = document.getElementById('flightCount');
    if (counter) {
        counter.textContent = visible;
    }
}

document.addEventListener('DOMContentLoaded', function () {
    flightModal = document.getElementById('flightModal');
    flightDeleteModal = document.getElementById('deleteModal');

    document.querySelectorAll('.js-close-modal').forEach(button => {
        button.addEventListener('click', closeAllFlightModals);
    });

    [flightModal, flightDeleteModal].forEach(modal => {
        if (!modal) return;
        modal.addEventListener('click', function (event) {
            if (event.target === modal) {
                closeModalElement(modal);
            }
        });
    });

    document.getElementById('flightSearch')?.addEventListener('input', filterFlights);
    document.getElementById('flightScheduleFilter')?.addEventListener('change', filterFlights);

    document.querySelector('.js-refresh-page')?.addEventListener('click', function () {
        window.location.reload();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeAllFlightModals();
        }
    });
});

