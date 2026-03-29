/* FlightReschedule page-specific logic */
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

// --- RESCHEDULE LOGIC ---
async function openRescheduleModal(id) {
    const res = await fetch('/Admin/GetSchedule/' + id);
    if (!res.ok) { showToast('Failed to load data', true); return; }
    const s = await res.json();

    document.getElementById('rsId').value = s.scheduleId;
    document.getElementById('rsDepTime').value = s.departureTime.substring(0, 16);
    document.getElementById('rsArrTime').value = s.arrivalTime.substring(0, 16);

    if (s.status === 'SCHEDULED' || s.status === 'DELAYED') {
        document.getElementById('rsStatus').value = 'DELAYED';
    }

    document.getElementById('rsInfoBox').innerHTML = `
        Current Time: <br/>
        Dep: <span>${new Date(s.departureTime).toLocaleString('en-GB')}</span> <br/>
        Arr: <span>${new Date(s.arrivalTime).toLocaleString('en-GB')}</span>
    `;

    document.getElementById('rescheduleModal').classList.add('active');
}

async function submitReschedule() {
    const id = document.getElementById('rsId').value;
    const newDep = document.getElementById('rsDepTime').value;
    const newArr = document.getElementById('rsArrTime').value;
    const status = document.getElementById('rsStatus').value;

    if (!newDep || !newArr) {
        showToast('Please specify the new times', true);
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const fd = new FormData();
    fd.append('id', id);
    fd.append('newDepartureTime', newDep);
    fd.append('newArrivalTime', newArr);
    fd.append('status', status);
    fd.append('__RequestVerificationToken', token);

    try {
        const res = await fetch('/Admin/ProcessReschedule', { method: 'POST', body: fd });
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

// --- CANCEL FLIGHT LOGIC ---
function openCancelModal(id) {
    document.getElementById('cancelId').value = id;
    document.getElementById('cancelModal').classList.add('active');
}

async function submitCancelFlight() {
    const id = document.getElementById('cancelId').value;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const fd = new FormData();
    fd.append('id', id);
    fd.append('__RequestVerificationToken', token);

    try {
        const res = await fetch('/Admin/CancelFlightSchedule', { method: 'POST', body: fd });
        const data = await res.json();
        if (data.success) {
            location.reload();
        } else {
            showToast(data.message, true);
        }
    } catch (e) {
        showToast('A network error occurred', true);
    }
    closeModal('cancelModal');
}
