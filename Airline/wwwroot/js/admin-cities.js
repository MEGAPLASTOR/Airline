/* ManageCity page-specific logic */
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
    document.getElementById('mTitle').innerHTML = '<i class="fa-solid fa-plus" style="color:var(--accent-gold);margin-right:6px"></i> Add City';
    document.getElementById('cId').value = 0;
    document.getElementById('cName').value = '';
    document.getElementById('cCountry').value = '';
    document.getElementById('cityModal').classList.add('active');
}

async function openEditModal(id) {
    document.getElementById('mTitle').innerHTML = '<i class="fa-solid fa-pen" style="color:var(--accent-gold);margin-right:6px"></i> Edit City';
    document.getElementById('cId').value = id;
    try {
        const res = await fetch('/Admin/GetCity/' + id);
        if (!res.ok) throw new Error();
        const data = await res.json();
        document.getElementById('cName').value = data.cityName;
        document.getElementById('cCountry').value = data.country;
        document.getElementById('cityModal').classList.add('active');
    } catch {
        showToast('Failed to load city data.', true);
    }
}

async function submitCity() {
    const id = document.getElementById('cId').value;
    const cName = document.getElementById('cName').value;
    const cCountry = document.getElementById('cCountry').value;

    if (!cName || !cCountry) {
        showToast('Please fill in both fields.', true);
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const fd = new FormData();
    fd.append('cityName', cName);
    fd.append('country', cCountry);
    fd.append('__RequestVerificationToken', token);

    const url = id == 0 ? '/Admin/CreateCity' : '/Admin/EditCity';
    if (id != 0) fd.append('id', id);

    try {
        const res = await fetch(url, { method: 'POST', body: fd });
        const json = await res.json();
        if (json.success) {
            location.reload();
        } else {
            showToast(json.message, true);
        }
    } catch {
        showToast('A network error occurred.', true);
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
        const res = await fetch('/Admin/DeleteCity', { method: 'POST', body: fd });
        const json = await res.json();
        if (json.success) {
            location.reload();
        } else {
            showToast(json.message, true);
        }
    } catch {
        showToast('A network error occurred.', true);
    }
    closeModal('deleteModal');
}
