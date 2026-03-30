let promotionModal;
let deletePromotionModal;

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

function closeAllPromotionModals() {
    closeModalElement(promotionModal);
    closeModalElement(deletePromotionModal);
}

function showPromotionToast(message, isError = false) {
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

function openCreatePromotionModal() {
    document.getElementById('promotionModalTitle').textContent = 'Add Promotion';
    document.getElementById('promotionId').value = '0';
    document.getElementById('promotionCode').value = '';
    document.getElementById('discountPercent').value = '';
    document.getElementById('promotionStartDate').value = '';
    document.getElementById('promotionEndDate').value = '';
    openModal(promotionModal);
}

async function openEditPromotionModal(id) {
    document.getElementById('promotionModalTitle').textContent = 'Edit Promotion';
    document.getElementById('promotionId').value = id;

    try {
        const response = await fetch('/Admin/GetPromotion/' + id);
        if (!response.ok) {
            throw new Error('Failed to load promotion.');
        }

        const data = await response.json();
        document.getElementById('promotionCode').value = data.promoCode || '';
        document.getElementById('discountPercent').value = data.discountPercent || '';
        document.getElementById('promotionStartDate').value = data.startDate || '';
        document.getElementById('promotionEndDate').value = data.endDate || '';
        openModal(promotionModal);
    } catch {
        showPromotionToast('Failed to load promotion data.', true);
    }
}

function openDeletePromotionModal(id, promoCode) {
    document.getElementById('deletePromotionId').value = id;
    document.getElementById('deletePromotionTargetText').textContent = promoCode || 'Selected promotion';
    openModal(deletePromotionModal);
}

async function submitPromotion() {
    const id = document.getElementById('promotionId').value;
    const promoCode = document.getElementById('promotionCode').value.trim().toUpperCase();
    const discountPercent = document.getElementById('discountPercent').value.trim();
    const startDate = document.getElementById('promotionStartDate').value;
    const endDate = document.getElementById('promotionEndDate').value;

    if (!promoCode || !discountPercent || !startDate || !endDate) {
        showPromotionToast('Please fill in all promotion fields.', true);
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const formData = new FormData();
    formData.append('promoCode', promoCode);
    formData.append('discountPercent', discountPercent);
    formData.append('startDate', startDate);
    formData.append('endDate', endDate);
    formData.append('__RequestVerificationToken', token);

    const url = id === '0' ? '/Admin/CreatePromotion' : '/Admin/EditPromotion';
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

        showPromotionToast(result.message || 'Unable to save promotion.', true);
    } catch {
        showPromotionToast('A network error occurred.', true);
    }
}

async function submitDeletePromotion() {
    const id = document.getElementById('deletePromotionId').value;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const formData = new FormData();
    formData.append('id', id);
    formData.append('__RequestVerificationToken', token);

    try {
        const response = await fetch('/Admin/DeletePromotion', { method: 'POST', body: formData });
        const result = await response.json();

        if (result.success) {
            location.reload();
            return;
        }

        showPromotionToast(result.message || 'Unable to delete promotion.', true);
    } catch {
        showPromotionToast('A network error occurred.', true);
    }
}

function filterPromotions() {
    const keyword = (document.getElementById('promotionSearch')?.value || '').trim().toLowerCase();
    const status = (document.getElementById('promotionStatusFilter')?.value || '').trim().toUpperCase();
    const rows = document.querySelectorAll('.promotion-row');
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

    const counter = document.getElementById('promotionCount');
    if (counter) {
        counter.textContent = visible;
    }
}

document.addEventListener('DOMContentLoaded', function () {
    promotionModal = document.getElementById('promotionModal');
    deletePromotionModal = document.getElementById('deletePromotionModal');

    document.querySelectorAll('.js-close-modal').forEach(button => {
        button.addEventListener('click', closeAllPromotionModals);
    });

    [promotionModal, deletePromotionModal].forEach(modal => {
        if (!modal) return;
        modal.addEventListener('click', function (event) {
            if (event.target === modal) {
                closeModalElement(modal);
            }
        });
    });

    document.getElementById('promotionSearch')?.addEventListener('input', filterPromotions);
    document.getElementById('promotionStatusFilter')?.addEventListener('change', filterPromotions);

    document.querySelector('.js-refresh-page')?.addEventListener('click', function () {
        window.location.reload();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeAllPromotionModals();
        }
    });
});
