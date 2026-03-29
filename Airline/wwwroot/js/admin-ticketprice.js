document.addEventListener("DOMContentLoaded", function () {
    const modal = document.getElementById("modal");
    const openModalBtn = document.getElementById("openModalBtn");
    const closeModalBtn = document.getElementById("closeModal");
    const cancelModalBtn = document.getElementById("cancelModalBtn");
    const priceInput = document.getElementById("Price");
    const preview = document.getElementById("preview");
    const priceIdInput = document.getElementById("PriceId");
    const scheduleInput = document.getElementById("ScheduleId");
    const classInput = document.getElementById("ClassId");

    function formatVnd(value) {
        const number = Number(value || 0);
        return number.toLocaleString("vi-VN") + " VNĐ";
    }

    function updatePreview() {
        if (!preview || !priceInput) return;
        preview.textContent = formatVnd(priceInput.value);
    }

    function openModal() {
        if (!modal) return;
        modal.classList.add("open");
        document.body.style.overflow = "hidden";
    }

    function closeModal() {
        if (!modal) return;
        modal.classList.remove("open");
        document.body.style.overflow = "";
    }

    function resetFormForCreate() {
        if (priceIdInput) priceIdInput.value = "";
        if (scheduleInput) scheduleInput.selectedIndex = 0;
        if (classInput) classInput.selectedIndex = 0;
        if (priceInput) priceInput.value = "";
        updatePreview();
    }

    if (openModalBtn) {
        openModalBtn.addEventListener("click", function () {
            resetFormForCreate();
            openModal();
        });
    }

    if (closeModalBtn) {
        closeModalBtn.addEventListener("click", closeModal);
    }

    if (cancelModalBtn) {
        cancelModalBtn.addEventListener("click", closeModal);
    }

    if (modal) {
        modal.addEventListener("click", function (e) {
            if (e.target === modal) {
                closeModal();
            }
        });
    }

    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") {
            closeModal();
        }
    });

    if (priceInput) {
        priceInput.addEventListener("input", updatePreview);
        updatePreview();
    }

    document.querySelectorAll(".editBtn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            if (priceIdInput) priceIdInput.value = btn.dataset.id || "";
            if (scheduleInput) scheduleInput.value = btn.dataset.schedule || "";
            if (classInput) classInput.value = btn.dataset.class || "";
            if (priceInput) priceInput.value = btn.dataset.price || "";
            updatePreview();
            openModal();
        });
    });
});