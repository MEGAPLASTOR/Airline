/* ManageFlights page-specific logic */

function openCreateModal() {
    document.getElementById('mTitle').innerHTML = '<i class="fa-solid fa-plus" style="color:var(--accent-gold);margin-right:6px"></i> Add Flight';
    document.getElementById('fId').value = '0';
    document.getElementById('fNumber').value = '';
    document.getElementById('fRoute').value = '';
    document.getElementById('flightModal').classList.add('active');
}

async function openEditModal(id) {
    const res = await fetch('/Admin/GetFlight/' + id);
    if (!res.ok) { showToast('Failed to load data', true); return; }
    const f = await res.json();
    document.getElementById('mTitle').innerHTML = '<i class="fa-solid fa-pen" style="color:var(--accent-gold);margin-right:6px"></i> Edit Flight';
    document.getElementById('fId').value = f.flightId;
    document.getElementById('fNumber').value = f.flightNumber;
    document.getElementById('fRoute').value = f.routeId;
    document.getElementById('flightModal').classList.add('active');
}

async function submitFlight() {
    const id = document.getElementById('fId').value;
    const flightNum = document.getElementById('fNumber').value.trim();
    const routeId = document.getElementById('fRoute').value;

    if (!flightNum || !routeId) {
        showToast('Please enter flight number and select a route', true);
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const url = id == '0' ? '/Admin/CreateFlight' : '/Admin/EditFlight/' + id;

    const fd = new FormData();
    if (id != '0') fd.append('id', id);
    fd.append('flightNumber', flightNum);
    fd.append('routeId', routeId);
    fd.append('__RequestVerificationToken', token);

    try {
        const res = await fetch(url, { method: 'POST', body: fd });
        const data = await res.json();
        if (data.success) {
            location.reload();
        } else {
            showToast(data.message, true);
        }
    } catch (e) {
        showToast('A network error occurred', true);
    }
}

function openDeleteModal(id) {
    document.getElementById('delId').value = id;
    document.getElementById('deleteModal').classList.add('active');
}

async function submitDelete() {
    const id = document.getElementById('delId').value;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const fd = new FormData();
    fd.append('id', id);
    fd.append('__RequestVerificationToken', token);

    try {
        const res = await fetch('/Admin/DeleteFlight/' + id, { method: 'POST', body: fd });
        const data = await res.json();
        if (data.success) {
            location.reload();
        } else {
            showToast(data.message, true);
        }
    } catch (e) {
        showToast('A network error occurred', true);
    }
    closeModal('deleteModal');
}
