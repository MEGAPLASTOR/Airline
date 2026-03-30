document.addEventListener('DOMContentLoaded', function () {
    const modal = document.getElementById('modal');
    const deleteModal = document.getElementById('deleteModal');
    const openModalBtn = document.getElementById('openModalBtn');
    const closeModalBtn = document.getElementById('closeModal');
    const cancelModalBtn = document.getElementById('cancelModalBtn');
    const priceInput = document.getElementById('Price');
    const preview = document.getElementById('preview');
    const priceIdInput = document.getElementById('PriceId');
    const scheduleInput = document.getElementById('ScheduleId');
    const classInput = document.getElementById('ClassId');
    const modalTitle = document.getElementById('priceModalTitle');
    const deletePriceId = document.getElementById('deletePriceId');
    const deleteTargetText = document.getElementById('deleteTargetText');

    function formatVnd(value) {
        const number = Number(value || 0);
        return number.toLocaleString('vi-VN') + ' VND';
    }

    function updatePreview() {
        if (!preview || !priceInput) return;
        preview.textContent = formatVnd(priceInput.value);
    }

    function openModal() {
        if (!modal) return;
        modal.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    function closeModal() {
        if (!modal) return;
        modal.classList.remove('active');
        document.body.style.overflow = '';
    }

    function openDeleteModal() {
        if (!deleteModal) return;
        deleteModal.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    function closeDeleteModal() {
        if (!deleteModal) return;
        deleteModal.classList.remove('active');
        document.body.style.overflow = '';
    }

    function resetFormForCreate() {
        if (modalTitle) modalTitle.textContent = 'Set Ticket Price';
        if (priceIdInput) priceIdInput.value = '';
        if (scheduleInput) scheduleInput.selectedIndex = 0;
        if (classInput) classInput.selectedIndex = 0;
        if (priceInput) priceInput.value = '';
        updatePreview();
    }

    function filterPrices() {
        const keyword = (document.getElementById('priceSearch')?.value || '').trim().toLowerCase();
        const classFilter = (document.getElementById('priceClassFilter')?.value || '').trim();
        const rows = document.querySelectorAll('.price-row');
        let visible = 0;

        rows.forEach(row => {
            const matchesKeyword = !keyword || (row.dataset.search || '').toLowerCase().includes(keyword);
            const matchesClass = !classFilter || (row.dataset.classId || '') === classFilter;
            const shouldShow = matchesKeyword && matchesClass;

            row.style.display = shouldShow ? '' : 'none';
            if (shouldShow) {
                visible += 1;
            }
        });

        const counter = document.getElementById('priceCount');
        if (counter) {
            counter.textContent = visible;
        }
    }

    openModalBtn?.addEventListener('click', function () {
        resetFormForCreate();
        openModal();
    });

    closeModalBtn?.addEventListener('click', closeModal);
    cancelModalBtn?.addEventListener('click', closeModal);

    modal?.addEventListener('click', function (event) {
        if (event.target === modal) {
            closeModal();
        }
    });

    deleteModal?.addEventListener('click', function (event) {
        if (event.target === deleteModal) {
            closeDeleteModal();
        }
    });

    document.querySelectorAll('.js-close-delete-modal').forEach(button => {
        button.addEventListener('click', closeDeleteModal);
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeModal();
            closeDeleteModal();
        }
    });

    if (priceInput) {
        priceInput.addEventListener('input', updatePreview);
        updatePreview();
    }

    document.querySelectorAll('.editBtn').forEach(button => {
        button.addEventListener('click', function () {
            if (modalTitle) modalTitle.textContent = 'Edit Ticket Price';
            if (priceIdInput) priceIdInput.value = button.dataset.id || '';
            if (scheduleInput) scheduleInput.value = button.dataset.schedule || '';
            if (classInput) classInput.value = button.dataset.class || '';
            if (priceInput) priceInput.value = button.dataset.price || '';
            updatePreview();
            openModal();
        });
    });

    document.querySelectorAll('.js-open-delete-modal').forEach(button => {
        button.addEventListener('click', function () {
            if (deletePriceId) deletePriceId.value = button.dataset.id || '';
            if (deleteTargetText) {
                deleteTargetText.textContent = (button.dataset.flight || 'Selected flight') + ' - ' + (button.dataset.className || 'Selected class');
            }
            openDeleteModal();
        });
    });

    document.getElementById('priceSearch')?.addEventListener('input', filterPrices);
    document.getElementById('priceClassFilter')?.addEventListener('change', filterPrices);

    document.querySelector('.js-refresh-page')?.addEventListener('click', function () {
        window.location.reload();
    });
});

