document.addEventListener("DOMContentLoaded", function () {
    const createModal = document.getElementById("createModal");
    const editModal = document.getElementById("editModal");
    const deleteModal = document.getElementById("deleteModal");

    const searchInput = document.getElementById("accountSearch");
    const roleFilter = document.getElementById("roleFilter");
    const rows = document.querySelectorAll(".account-row");
    const accountCount = document.getElementById("accountCount");

    function openModal(modal) {
        if (!modal) return;
        modal.classList.add("active");
        document.body.style.overflow = "hidden";
    }

    function closeModal(modal) {
        if (!modal) return;
        modal.classList.remove("active");
        document.body.style.overflow = "";
    }

    function closeAllModals() {
        closeModal(createModal);
        closeModal(editModal);
        closeModal(deleteModal);
    }

    document.querySelectorAll(".js-close-modal").forEach(button => {
        button.addEventListener("click", function () {
            closeAllModals();
        });
    });

    [createModal, editModal, deleteModal].forEach(modal => {
        if (!modal) return;

        modal.addEventListener("click", function (e) {
            if (e.target === modal) {
                closeModal(modal);
            }
        });
    });

    const openCreateButton = document.querySelector(".js-open-create-modal");
    if (openCreateButton) {
        openCreateButton.addEventListener("click", function () {
            openModal(createModal);
        });
    }

    document.querySelectorAll(".js-open-edit-modal").forEach(button => {
        button.addEventListener("click", function () {
            document.getElementById("editUserId").value = this.dataset.userId || "";
            document.getElementById("editFirstName").value = this.dataset.firstName || "";
            document.getElementById("editLastName").value = this.dataset.lastName || "";
            document.getElementById("editUsername").value = this.dataset.username || "";
            document.getElementById("editEmail").value = this.dataset.email || "";
            document.getElementById("editPhone").value = this.dataset.phone || "";
            document.getElementById("editAddress").value = this.dataset.address || "";
            document.getElementById("editGender").value = this.dataset.gender || "";
            document.getElementById("editAge").value = this.dataset.age || "";
            document.getElementById("editCccd").value = this.dataset.cccd || "";
            document.getElementById("editRole").value = (this.dataset.role || "USER").toUpperCase();
            document.getElementById("editSkyMiles").value = this.dataset.skymiles || 0;
            document.getElementById("editPassword").value = "";

            openModal(editModal);
        });
    });

    document.querySelectorAll(".js-open-delete-modal").forEach(button => {
        button.addEventListener("click", function () {
            document.getElementById("deleteUserId").value = this.dataset.userId || "";
            document.getElementById("deleteTargetText").textContent =
                `${this.dataset.fullName || ""} (${this.dataset.username || ""})`;

            openModal(deleteModal);
        });
    });

    function filterAccounts() {
        const keyword = (searchInput?.value || "").trim().toLowerCase();
        const selectedRole = (roleFilter?.value || "").trim().toUpperCase();
        let visibleCount = 0;

        rows.forEach(row => {
            const searchText = (row.dataset.search || "").toLowerCase();
            const role = (row.dataset.role || "").toUpperCase();

            const matchKeyword = !keyword || searchText.includes(keyword);
            const matchRole = !selectedRole || role === selectedRole;

            const visible = matchKeyword && matchRole;
            row.style.display = visible ? "" : "none";

            if (visible) {
                visibleCount++;
            }
        });

        if (accountCount) {
            accountCount.textContent = visibleCount;
        }
    }

    if (searchInput) {
        searchInput.addEventListener("input", filterAccounts);
    }

    if (roleFilter) {
        roleFilter.addEventListener("change", filterAccounts);
    }

    const refreshButton = document.querySelector(".js-refresh-page");
    if (refreshButton) {
        refreshButton.addEventListener("click", function () {
            window.location.reload();
        });
    }

    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") {
            closeAllModals();
        }
    });
});