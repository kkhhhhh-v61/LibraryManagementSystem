const modalId = 'roomEditModal';

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
    const modal = document.getElementById(modalId);
    if (e.target === modal) {
        closeEditModal();
    }
});

// Close on Escape key
window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        closeEditModal();
    }
});