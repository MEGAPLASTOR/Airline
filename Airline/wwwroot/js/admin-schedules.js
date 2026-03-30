/* FlightSchedules page-specific logic */
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

function openCreateModal() {
    document.getElementById('mTitle').innerHTML = '<i class="fa-solid fa-plus" style="color:var(--accent-gold);margin-right:6px"></i> Add Schedule';
    document.getElementById('sId').value = '0';
    document.getElementById('sFlight').value = '';
    document.getElementById('sDepTime').value = '';
    document.getElementById('sArrTime').value = '';
    document.getElementById('sTotal').value = '180';
    document.getElementById('sAvail').value = '180';
    document.getElementById('sStatus').value = 'SCHEDULED';

    document.getElementById('sAvail').disabled = true;
    document.getElementById('sTotal').disabled = false;
    document.getElementById('sTotal').oninput = function () {
        document.getElementById('sAvail').value = this.value;
    };

    document.getElementById('scheduleModal').classList.add('active');
}

async function openEditModal(id) {
    const res = await fetch('/Admin/GetSchedule/' + id);
    if (!res.ok) { showToast('Failed to load data', true); return; }
    const s = await res.json();

    document.getElementById('mTitle').innerHTML = '<i class="fa-solid fa-pen" style="color:var(--accent-gold);margin-right:6px"></i> Edit Schedule';
    document.getElementById('sId').value = s.scheduleId;
    document.getElementById('sFlight').value = s.flightId;
    document.getElementById('sDepTime').value = s.departureTime;
    document.getElementById('sArrTime').value = s.arrivalTime;
    document.getElementById('sTotal').value = s.totalSeats;
    document.getElementById('sAvail').value = s.availableSeats;
    document.getElementById('sStatus').value = s.status;

    document.getElementById('sAvail').disabled = true;
    document.getElementById('sTotal').disabled = false;
    document.getElementById('sTotal').oninput = null;

    document.getElementById('scheduleModal').classList.add('active');
}

async function submitSchedule() {
    const id = document.getElementById('sId').value;
    const flightId = document.getElementById('sFlight').value;
    const depTime = document.getElementById('sDepTime').value;
    const arrTime = document.getElementById('sArrTime').value;
    const total = document.getElementById('sTotal').value;
    const avail = document.getElementById('sAvail').value;
    const status = document.getElementById('sStatus').value;

    if (!flightId || !depTime || !arrTime || !total) {
        showToast('Please fill all required fields', true);
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const url = id == '0' ? '/Admin/CreateSchedule' : '/Admin/EditSchedule/' + id;

    const fd = new FormData();
    if (id != '0') fd.append('id', id);
    fd.append('flightId', flightId);
    fd.append('departureTime', depTime);
    fd.append('arrivalTime', arrTime);
    fd.append('totalSeats', total);
    if (id != '0') fd.append('availableSeats', avail);
    fd.append('status', status);
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
        const res = await fetch('/Admin/DeleteSchedule/' + id, { method: 'POST', body: fd });
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
