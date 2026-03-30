let cityModal;
let cityDeleteModal;

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

function closeAllCityModals() {
    closeModalElement(cityModal);
    closeModalElement(cityDeleteModal);
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
    document.getElementById('mTitle').textContent = 'Add City';
    document.getElementById('cId').value = '0';
    document.getElementById('cName').value = '';
    document.getElementById('cCountry').value = '';
    openModal(cityModal);
}

async function openEditModal(id) {
    document.getElementById('mTitle').textContent = 'Edit City';
    document.getElementById('cId').value = id;

    try {
        const response = await fetch('/Admin/GetCity/' + id);
        if (!response.ok) {
            throw new Error('Failed to load city');
        }

        const data = await response.json();
        document.getElementById('cName').value = data.cityName || '';
        document.getElementById('cCountry').value = data.country || '';
        openModal(cityModal);
    } catch {
        showToast('Failed to load city data.', true);
    }
}

function openDeleteModal(id, cityName) {
    document.getElementById('delId').value = id;
    document.getElementById('deleteTargetText').textContent = cityName || 'Selected city';
    openModal(cityDeleteModal);
}

async function submitCity() {
    const id = document.getElementById('cId').value;
    const cityName = document.getElementById('cName').value.trim();
    const country = document.getElementById('cCountry').value.trim();

    if (!cityName || !country) {
        showToast('Please fill in both city and country.', true);
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const formData = new FormData();
    formData.append('cityName', cityName);
    formData.append('country', country);
    formData.append('__RequestVerificationToken', token);

    const url = id === '0' ? '/Admin/CreateCity' : '/Admin/EditCity';
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

        showToast(result.message || 'Unable to save city.', true);
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
        const response = await fetch('/Admin/DeleteCity', { method: 'POST', body: formData });
        const result = await response.json();

        if (result.success) {
            location.reload();
            return;
        }

        showToast(result.message || 'Unable to delete city.', true);
    } catch {
        showToast('A network error occurred.', true);
    }
}

function filterCities() {
    const keyword = (document.getElementById('citySearch')?.value || '').trim().toLowerCase();
    const usage = (document.getElementById('cityUsageFilter')?.value || '').trim().toUpperCase();
    const rows = document.querySelectorAll('.city-row');
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

    const counter = document.getElementById('cityCount');
    if (counter) {
        counter.textContent = visible;
    }
}

document.addEventListener('DOMContentLoaded', function () {
    cityModal = document.getElementById('cityModal');
    cityDeleteModal = document.getElementById('deleteModal');

    document.querySelectorAll('.js-close-modal').forEach(button => {
        button.addEventListener('click', closeAllCityModals);
    });

    [cityModal, cityDeleteModal].forEach(modal => {
        if (!modal) return;
        modal.addEventListener('click', function (event) {
            if (event.target === modal) {
                closeModalElement(modal);
            }
        });
    });

    document.getElementById('citySearch')?.addEventListener('input', filterCities);
    document.getElementById('cityUsageFilter')?.addEventListener('change', filterCities);

    document.querySelector('.js-refresh-page')?.addEventListener('click', function () {
        window.location.reload();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeAllCityModals();
        }
    });
});

