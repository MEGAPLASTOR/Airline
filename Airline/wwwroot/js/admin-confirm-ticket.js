document.addEventListener('DOMContentLoaded', function () {
    const confirmModal = document.getElementById('confirmModal');
    const confirmBookingId = document.getElementById('confirmBookingId');
    const confirmTargetText = document.getElementById('confirmTargetText');

    function openModal() {
        if (!confirmModal) return;
        confirmModal.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    function closeModal() {
        if (!confirmModal) return;
        confirmModal.classList.remove('active');
        document.body.style.overflow = '';
    }

    function filterBookings() {
        const keyword = (document.getElementById('bookingSearch')?.value || '').trim().toLowerCase();
        const rows = document.querySelectorAll('.booking-row');
        let visible = 0;

        rows.forEach(row => {
            const matches = !keyword || (row.dataset.search || '').toLowerCase().includes(keyword);
            row.style.display = matches ? '' : 'none';
            if (matches) {
                visible += 1;
            }
        });

        const counter = document.getElementById('bookingCount');
        if (counter) {
            counter.textContent = visible;
        }
    }

    document.querySelectorAll('.js-open-confirm-modal').forEach(button => {
        button.addEventListener('click', function () {
            if (confirmBookingId) {
                confirmBookingId.value = this.dataset.bookingId || '';
            }

            if (confirmTargetText) {
                confirmTargetText.textContent = (this.dataset.customerName || 'Customer') + ' - ' + (this.dataset.flight || 'Flight') + ' - ' + (this.dataset.total || '0 VND');
            }

            openModal();
        });
    });

    document.querySelectorAll('.js-close-modal').forEach(button => {
        button.addEventListener('click', closeModal);
    });

    confirmModal?.addEventListener('click', function (event) {
        if (event.target === confirmModal) {
            closeModal();
        }
    });

    document.getElementById('bookingSearch')?.addEventListener('input', filterBookings);

    document.querySelector('.js-refresh-page')?.addEventListener('click', function () {
        window.location.reload();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeModal();
        }
    });
});

