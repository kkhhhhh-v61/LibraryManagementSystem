const modalId = 'roomEditModal';
const addRoomModalId = 'addRoomModal';
const deleteModalId = 'deleteConfirmationModal';

function openAddRoomModal() {
    const modal = document.getElementById(addRoomModalId);
    if (modal) {
        modal.style.display = 'block';

        const form = modal.querySelector('form');
        if (form) form.reset();
    }
}

function closeAddRoomModal() {
    const modal = document.getElementById(addRoomModalId);
    if (modal) {
        modal.style.display = 'none';
    }
}

function openEditModal(roomId, name, location, capacity, status) {
    const modal = document.getElementById(modalId);

    if (!modal) {
        console.error('Modal not found!');
        return;
    }

    document.getElementById('modalRoomIdHidden').value = roomId;
    document.getElementById('modalRoomIdDisplay').value = roomId;

    document.getElementById('modalRoomName').value = name;
    document.getElementById('modalRoomLocation').value = location;

    document.getElementById('modalCapacity').value = capacity;
    document.getElementById('modalStatus').value = status;

    modal.style.display = 'block';
}

function closeEditModal() {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'none';
    }
}

// Close on click outside
window.addEventListener('click', (e) => {
    const editModal = document.getElementById('roomEditModal');
    const addModal = document.getElementById('addRoomModal');

    if (editModal && e.target === editModal) {
        closeEditModal();
    }

    if (addModal && e.target === addModal) {
        closeAddRoomModal();
    }
});

// Close on Escape key
window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        closeEditModal();
        closeAddRoomModal();
    }
});

function openDeleteModal(actionUrl, itemName) {
    const modal = document.getElementById(deleteModalId);
    if (!modal) return;

    const deleteForm = document.getElementById('deleteForm');
    deleteForm.action = actionUrl;

    document.getElementById('deleteItemName').textContent = itemName;

    modal.style.display = 'block';
}

function closeDeleteModal() {
    const modal = document.getElementById(deleteModalId);
    if (modal) modal.style.display = 'none';
}

window.addEventListener('click', (e) => {
    const dModal = document.getElementById(deleteModalId);
    if (e.target === dModal) closeDeleteModal();
});

function addCategoryToDropdown(data) {
    var select = document.getElementById('RoomInput_RoomCategoryId');

    if (select) {
        var option = new Option(data.name, data.id);
        select.add(option, 1);

        select.style.borderColor = "var(--success)";
        setTimeout(() => select.style.borderColor = "", 1000);
    }
}
