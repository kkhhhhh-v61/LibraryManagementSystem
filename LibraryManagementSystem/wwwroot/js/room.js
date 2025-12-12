// The ID of your modal div in the HTML
const modalId = 'roomEditModal';

// Open the popup and fill the inputs with existing data
function openEditModal(roomId, capacity, status) {
    const modal = document.getElementById(modalId);

    // Just a safety check in case the ID is wrong
    if (!modal) {
        console.error('Modal not found!');
        return;
    }

    // Set values for the form fields
    // Ensure these IDs match your input tags exactly
    document.getElementById('modalRoomIdHidden').value = roomId;
    document.getElementById('modalRoomIdDisplay').value = roomId;
    document.getElementById('modalCapacity').value = capacity;
    document.getElementById('modalStatus').value = status;

    // Show the box
    modal.style.display = 'block';
}

// Hide the popup
function closeEditModal() {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'none';
    }
}

// Close the modal if user clicks on the dark background overlay
window.addEventListener('click', (e) => {
    const modal = document.getElementById(modalId);
    if (e.target === modal) {
        closeEditModal();
    }
});

// Also close it if user hits the 'Escape' key
window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        closeEditModal();
    }
});