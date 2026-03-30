let createModal;
let editModal;
let deleteModal;

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

function closeAllTicketClassModals() {
    closeModalElement(createModal);
    closeModalElement(editModal);
    closeModalElement(deleteModal);
}

function filterTicketClasses() {
    const keyword = (document.getElementById('ticketClassSearch')?.value || '').trim().toLowerCase();
    const usage = (document.getElementById('ticketClassUsageFilter')?.value || '').trim().toUpperCase();
    const rows = document.querySelectorAll('.ticket-class-row');
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

    const counter = document.getElementById('ticketClassCount');
    if (counter) {
        counter.textContent = visible;
    }
}

document.addEventListener('DOMContentLoaded', function () {
    createModal = document.getElementById('createModal');
    editModal = document.getElementById('editModal');
    deleteModal = document.getElementById('deleteModal');

    document.querySelector('.js-open-create-modal')?.addEventListener('click', function () {
        document.getElementById('createClassName').value = '';
        openModal(createModal);
    });

    document.querySelectorAll('.js-open-edit-modal').forEach(button => {
        button.addEventListener('click', function () {
            document.getElementById('editClassId').value = this.dataset.classId || '';
            document.getElementById('editClassName').value = this.dataset.className || '';
            openModal(editModal);
        });
    });

    document.querySelectorAll('.js-open-delete-modal').forEach(button => {
        button.addEventListener('click', function () {
            document.getElementById('deleteClassId').value = this.dataset.classId || '';
            document.getElementById('deleteTargetText').textContent = this.dataset.className || 'Selected class';
            openModal(deleteModal);
        });
    });

    document.querySelectorAll('.js-close-modal').forEach(button => {
        button.addEventListener('click', closeAllTicketClassModals);
    });

    [createModal, editModal, deleteModal].forEach(modal => {
        if (!modal) return;
        modal.addEventListener('click', function (event) {
            if (event.target === modal) {
                closeModalElement(modal);
            }
        });
    });

    document.getElementById('ticketClassSearch')?.addEventListener('input', filterTicketClasses);
    document.getElementById('ticketClassUsageFilter')?.addEventListener('change', filterTicketClasses);

    document.querySelector('.js-refresh-page')?.addEventListener('click', function () {
        window.location.reload();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeAllTicketClassModals();
        }
    });
});

